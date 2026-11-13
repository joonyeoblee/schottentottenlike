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

        // 공통 방 옵션 (끊긴 플레이어 자리 60초 보존 → 재연결 가능)
        RoomOptions options = ReconnectManager.CreateRoomOptions();
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

        // ---- 빠른 대전 (자동 매칭) ----
        var quick = UIKit.Button(safe, font, "UI/Button", Vector2.zero, new Vector2(560, 150), out var quickLabel);
        var qrt = (RectTransform)quick.transform;
        qrt.anchorMin = qrt.anchorMax = qrt.pivot = new Vector2(1f, 0.5f);
        qrt.anchoredPosition = new Vector2(-90, -10);
        quickLabel.text = "빠른 대전";
        quickLabel.fontSize = 52;
        _quickLabel = quickLabel;
        quick.onClick.AddListener(QuickMatch);

        // ---- 전적 패널 ----
        var stats = UIKit.Rect("Stats", safe);
        stats.anchorMin = stats.anchorMax = stats.pivot = new Vector2(1f, 1f);
        stats.anchoredPosition = new Vector2(-90, -225);
        stats.sizeDelta = new Vector2(560, 205);
        UIKit.SlicedImage(stats.gameObject, "UI/Panel", Color.white).raycastTarget = false;
        var title = UIKit.Text(stats, font, "<b>내 전적</b>", 30, UI_MatchHud.InkColor);
        UIKit.Place(title.rectTransform, new Vector2(0, 70), new Vector2(500, 40));
        var body = UIKit.Text(stats, font, StatsText(), 23, UI_MatchHud.InkColor);
        UIKit.Place(body.rectTransform, new Vector2(0, -18), new Vector2(510, 140));
        body.alignment = TextAlignmentOptions.Left;
        body.lineSpacing = 6;
    }

    private TextMeshProUGUI _quickLabel;

    private static string StatsText()
    {
        string Line(string name, PlayerStats.Record r) =>
            r.Games == 0 ? $"{name}  <color=#888>기록 없음</color>"
                         : $"{name}  {r.Wins}승 {r.Losses}패  <size=85%>(승률 {r.WinRate}%, 최고 {r.BestStreak}연승)</size>";
        return Line("온라인", PlayerStats.GetOnline()) + "\n" +
               Line("AI 쉬움", PlayerStats.GetAI(AIDifficulty.Easy)) + "\n" +
               Line("AI 보통", PlayerStats.GetAI(AIDifficulty.Normal)) + "\n" +
               Line("AI 어려움", PlayerStats.GetAI(AIDifficulty.Hard));
    }

    /// <summary>
    /// 빠른 대전: 빈자리가 있는 방에 자동으로 들어가고, 없으면 새 방을 만들어 상대를 기다립니다.
    /// (방 이름/목록을 고를 필요 없음 — 모바일에서 버튼 하나로 대전)
    /// </summary>
    public void QuickMatch()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("빠른 대전: 서버에 연결되지 않음");
            return;
        }
        if (_quickLabel != null) _quickLabel.text = "상대 찾는 중...";
        PhotonNetwork.JoinRandomOrCreateRoom(
            expectedMaxPlayers: 2,
            matchingType: MatchmakingMode.FillRoom,
            typedLobby: TypedLobby.Default,
            roomName: "quick_" + System.DateTime.Now.Ticks % 100000,
            roomOptions: ReconnectManager.CreateRoomOptions());
        // 방에 들어가면 PhotonServerManager.OnJoinedRoom이 대전 씬으로 이동 (상대가 오면 자동 시작)
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"빠른 대전 실패: {returnCode} {message}");
        if (_quickLabel != null) _quickLabel.text = "빠른 대전";
    }
}
