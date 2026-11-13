using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 대전 중 네트워크가 끊겼을 때 자동으로 같은 방에 다시 들어가는 관리자. (게임 시작 시 자동 생성)
///
/// 흐름
/// 1) 대전 중 연결 끊김(OnDisconnected) → "재연결 중" 오버레이, 로비로 튕기지 않음
/// 2) 3초마다 ReconnectAndRejoin 시도 (실패하면 서버 재접속 후 RejoinRoom)
/// 3) 방에 다시 들어오면 BattleField가 상대에게 판 상태 전체를 요청해서 복원
/// 4) 방의 PlayerTtl(=RejoinWindowSeconds) 안에 못 돌아오면 포기하고 로비로
///
/// 방은 PlayerTtl이 있어야 끊긴 플레이어의 자리가 보존됩니다. (RoomOptionsFactory 참고)
/// </summary>
public class ReconnectManager : MonoBehaviourPunCallbacks
{
    public const int RejoinWindowSeconds = 60;

    /// <summary>
    /// 대전 방 공통 옵션 (방 만들기 / 빠른 대전 모두 같은 설정).
    /// PlayerTtl: 연결이 끊긴 플레이어의 자리를 보존하는 시간 → 이 안에 돌아오면 이어서 대전
    /// </summary>
    public static RoomOptions CreateRoomOptions()
    {
        return new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true,
            CleanupCacheOnLeave = true,
            PlayerTtl = RejoinWindowSeconds * 1000,
            EmptyRoomTtl = 15000, // 둘 다 잠깐 끊겨도 방이 바로 사라지지 않게
            PublishUserId = true,
        };
    }
    private const float GiveUpSeconds = RejoinWindowSeconds - 5;

    public static ReconnectManager Instance { get; private set; }

    /// <summary>재연결 시도 중 (로비 이동/새 게임 시작 등을 막는 용도)</summary>
    public static bool IsReconnecting { get; private set; }

    /// <summary>
    /// 대전 중인데 스스로 나가지 않았음 → 방에서 빠지면 '연결 끊김'으로 보고 재연결을 기다려야 함.
    /// (Photon은 연결이 끊길 때 OnDisconnected보다 OnLeftRoom을 먼저 호출하므로 IsReconnecting만으로는 부족)
    /// </summary>
    public static bool ExpectingReconnect => IsReconnecting || (Instance != null && Instance._inMatch && !Instance._intentionalLeave);

    /// <summary>방금 재입장했음 — BattleField가 판 상태를 요청한 뒤 false로 돌림</summary>
    public static bool JustRejoined { get; set; }

    private string _lastRoom;
    private bool _intentionalLeave;
    private bool _inMatch;
    private GameObject _overlay;
    private TextMeshProUGUI _overlayText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("ReconnectManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ReconnectManager>();
    }

    /// <summary>플레이어가 직접 방을 나갈 때 호출 (재연결하지 않음, 자리도 즉시 비움)</summary>
    public static void LeaveIntentionally()
    {
        if (Instance != null) { Instance._intentionalLeave = true; Instance._inMatch = false; Instance._lastRoom = null; }
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(false); // becomeInactive=false: 상대가 기다리지 않게
    }

    /// <summary>대전 화면에 들어왔음 (BattleField 초기화 시)</summary>
    public static void MarkInMatch(bool inMatch)
    {
        if (Instance != null) Instance._inMatch = inMatch;
    }

    // ------------------------------------------------------------------
    public override void OnJoinedRoom()
    {
        _lastRoom = PhotonNetwork.CurrentRoom.Name;
        _intentionalLeave = false;
        if (IsReconnecting)
        {
            Debug.Log("[Reconnect] 방 재입장 성공: " + _lastRoom);
            IsReconnecting = false;
            JustRejoined = true;
            HideOverlay();
        }
    }

    public override void OnLeftRoom()
    {
        if (_intentionalLeave) { _lastRoom = null; _inMatch = false; }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (IsReconnecting || _intentionalLeave || !_inMatch || string.IsNullOrEmpty(_lastRoom)) return;
        if (cause == DisconnectCause.DisconnectByClientLogic) return; // 앱이 직접 끊은 경우
        Debug.LogWarning($"[Reconnect] 대전 중 연결 끊김 ({cause}) → 재연결 시도");
        StartCoroutine(ReconnectLoop());
    }

    /// <summary>AccountManager.OnConnectedToMaster에서 호출: 재연결 중이면 로비 대신 원래 방으로</summary>
    public static bool HandleConnectedToMaster()
    {
        if (!IsReconnecting || Instance == null || string.IsNullOrEmpty(Instance._lastRoom)) return false;
        Debug.Log("[Reconnect] 마스터 서버 재접속 → RejoinRoom " + Instance._lastRoom);
        PhotonNetwork.RejoinRoom(Instance._lastRoom);
        return true;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (!IsReconnecting) return;
        Debug.LogWarning($"[Reconnect] 재입장 실패 {returnCode} {message}");
        // 방이 이미 없어졌거나(상대도 나감), 닫혔거나, 내 자리가 만료됨 → 포기. 그 외(일시적 오류)는 계속 재시도
        if (returnCode == ErrorCode.GameDoesNotExist || returnCode == ErrorCode.GameClosed ||
            returnCode == ErrorCode.JoinFailedWithRejoinerNotFound)
            GiveUp("대전이 이미 끝났어요.");
    }

    private IEnumerator ReconnectLoop()
    {
        IsReconnecting = true;
        ShowOverlay();
        float start = Time.realtimeSinceStartup;
        float nextTry = 0;

        while (IsReconnecting)
        {
            float elapsed = Time.realtimeSinceStartup - start;
            float left = GiveUpSeconds - elapsed;
            if (left <= 0) { GiveUp("다시 연결하지 못했어요."); yield break; }
            if (_overlayText != null) _overlayText.text = $"연결이 끊겼어요\n<size=60%>다시 연결하는 중... {Mathf.CeilToInt(left)}초</size>";

            if (elapsed >= nextTry)
            {
                nextTry = elapsed + 3f;
                var state = PhotonNetwork.NetworkClientState;
                if (state == ClientState.Disconnected || state == ClientState.PeerCreated)
                {
                    if (!PhotonNetwork.ReconnectAndRejoin())
                    {
                        Debug.Log("[Reconnect] ReconnectAndRejoin 불가 → 서버 재접속");
                        PhotonNetwork.ConnectUsingSettings();
                    }
                }
                else if (state == ClientState.ConnectedToMasterServer || state == ClientState.JoinedLobby)
                {
                    PhotonNetwork.RejoinRoom(_lastRoom);
                }
            }
            yield return null;
        }
    }

    private void GiveUp(string reason)
    {
        Debug.LogWarning("[Reconnect] 포기: " + reason);
        IsReconnecting = false;
        _inMatch = false;
        _lastRoom = null;
        StopAllCoroutines();
        if (_overlayText != null) _overlayText.text = reason + "\n<size=60%>로비로 돌아갑니다</size>";
        StartCoroutine(GoLobbyAfter(2f));
    }

    private IEnumerator GoLobbyAfter(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        HideOverlay();
        if (PhotonNetwork.IsConnectedAndReady) SceneManager.LoadScene(1);
        else
        {
            // 서버 연결까지 끊겼으면 메인 화면에서 다시 접속
            if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
            SceneManager.LoadScene(0);
        }
    }

    // ------------------------------------------------------------------
    private void ShowOverlay()
    {
        if (_overlay != null) { _overlay.SetActive(true); return; }
        UI_MatchHud.EnsureEventSystem();
        _overlay = new GameObject("ReconnectOverlay", typeof(RectTransform));
        DontDestroyOnLoad(_overlay);
        var canvas = _overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = _overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2280, 1080);
        scaler.matchWidthOrHeight = 1f;
        _overlay.AddComponent<GraphicRaycaster>();

        var dim = UIKit.Rect("Dim", _overlay.transform);
        UIKit.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);

        var panel = UIKit.Rect("Panel", _overlay.transform);
        UIKit.Place(panel, Vector2.zero, new Vector2(900, 380));
        UIKit.SlicedImage(panel.gameObject, "UI/Panel", Color.white);
        var font = UI_MatchHud.FindSceneFont(_overlay);
        _overlayText = UIKit.Text(panel, font, "연결이 끊겼어요", 56, UI_MatchHud.InkColor);
        UIKit.Place(_overlayText.rectTransform, new Vector2(0, 50), new Vector2(820, 200));
        _overlayText.fontStyle = FontStyles.Bold;

        var btn = UIKit.Button(panel, font, "UI/Button", new Vector2(0, -110), new Vector2(300, 100), out var label);
        label.text = "포기하고 나가기";
        label.fontSize = 30;
        btn.onClick.AddListener(() => GiveUp("대전을 나갔어요."));
    }

    private void HideOverlay()
    {
        if (_overlay != null) _overlay.SetActive(false);
    }
}
