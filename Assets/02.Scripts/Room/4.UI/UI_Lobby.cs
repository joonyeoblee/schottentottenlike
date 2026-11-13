using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_Lobby : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI NickNameTextUI;
    public TMP_InputField RoomNameInputField;
    public TMP_InputField PasswordInputField;

    public GameObject RoomPanel;

    private void Start()
    {
        NickNameTextUI.text = AccountManager.Instance.CurrentAccount.Nickname;
        CreateRulesButton();
    }

    public void OpenRoomPanel()
    {
        RoomPanel.SetActive(true);
    }

    public void CloseRoomPanel()
    {
        RoomPanel.SetActive(false);
    }

    public void CreateRoom()
    {
        Debug.Log("=== 방 생성 시작 ===");

        // 연결 상태 확인
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Photon에 연결되지 않았습니다.");
            return;
        }

        Debug.Log($"연결 상태: {PhotonNetwork.NetworkClientState}");

        // 로비에 참가하지 않았다면 참가
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("로비에 참가합니다.");
            PhotonNetwork.JoinLobby();
            return;
        }

        Debug.Log("로비에 이미 참가되어 있습니다.");

        string roomName = RoomNameInputField.text + "_" + (int)Time.time;
        Debug.Log($"방 이름: {roomName}");

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true, // 반드시 명시적으로 true로 설정
            IsOpen = true,
            CleanupCacheOnLeave = true // 반드시 명시적으로 true로 설정
        };
        bool result = PhotonNetwork.CreateRoom(roomName,
            options, TypedLobby.Default);

        Debug.Log($"방 생성 요청 결과: {result}");

        if (result)
        {
            CloseRoomPanel();
        }
        else
        {
            Debug.LogError("방 생성 요청이 실패했습니다.");
        }
    }

    public void CreatAIRoom()
    {
        // 난이도(쉬움/보통/어려움)를 고른 뒤 AI 대전 시작
        UI_DifficultyPopup.Show(_ => SceneManager.LoadScene(3));
    }

    /// <summary>로비 우상단 '게임 규칙' 버튼 (런타임 생성) — 튜토리얼을 언제든 다시 보기</summary>
    private void CreateRulesButton()
    {
        UI_MatchHud.EnsureEventSystem();
        var go = new GameObject("UI_LobbyRules", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2280, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var safe = UIKit.Rect("SafeArea", go.transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        var font = UI_MatchHud.FindSceneFont(go);
        var btn = UIKit.Button(safe, font, "UI/ButtonGreen", Vector2.zero, new Vector2(260, 100), out var label);
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-36, -30);
        label.text = "게임 규칙";
        btn.onClick.AddListener(() => UI_Tutorial.Show(font, false, null));
    }
}
