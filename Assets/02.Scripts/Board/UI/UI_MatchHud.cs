using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 게임 보드 위에 표시되는 HUD(턴 표시, 점령 현황)와 결과 패널.
/// 씬/프리팹 수정 없이 런타임에 생성되므로 솔로(AI)와 멀티 양쪽에서 공용으로 사용합니다.
/// </summary>
public class UI_MatchHud : MonoBehaviour
{
    private static readonly Color MyColor = new Color(0.36f, 0.78f, 0.47f);
    private static readonly Color EnemyColor = new Color(0.93f, 0.42f, 0.38f);
    private static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
    private static readonly Color ButtonColor = new Color(0.86f, 0.70f, 0.42f);
    private static readonly Color ButtonTextColor = new Color(0.12f, 0.09f, 0.05f);

    private TMP_FontAsset _font;

    private TextMeshProUGUI _turnText;
    private Image _turnBadge;
    private TextMeshProUGUI _scoreText;

    private GameObject _resultRoot;
    private TextMeshProUGUI _resultTitle;
    private TextMeshProUGUI _resultSub;
    private TextMeshProUGUI _resultStatus;
    private Button _primaryButton;
    private TextMeshProUGUI _primaryLabel;
    private Button _secondaryButton;
    private TextMeshProUGUI _secondaryLabel;
    private Action _onPrimary;
    private Action _onSecondary;

    public bool IsResultVisible => _resultRoot != null && _resultRoot.activeSelf;

    public static UI_MatchHud Create(Transform owner)
    {
        EnsureEventSystem();

        var go = new GameObject("UI_MatchHud", typeof(RectTransform));
        if (owner != null) go.transform.SetParent(owner, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var hud = go.AddComponent<UI_MatchHud>();
        hud.Build();
        return hud;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(es);
    }

    private TMP_FontAsset FindFont()
    {
        // 씬에 이미 있는 TMP 텍스트의 폰트를 그대로 사용 (한글 글리프 포함 폰트)
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.font != null && t.gameObject != gameObject) return t.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private void Build()
    {
        _font = FindFont();
        var root = (RectTransform)transform;

        // ---- 턴 배지 (상단 중앙) ----
        var badge = CreateRect("TurnBadge", root);
        badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
        badge.pivot = new Vector2(0.5f, 1f);
        badge.anchoredPosition = new Vector2(0, -18);
        badge.sizeDelta = new Vector2(300, 64);
        _turnBadge = badge.gameObject.AddComponent<Image>();
        _turnBadge.color = new Color(0, 0, 0, 0.55f);
        _turnBadge.raycastTarget = false;
        _turnText = CreateText("TurnText", badge, "", 34, Color.white);
        Stretch(_turnText.rectTransform);

        // ---- 점령 현황 (배지 아래) ----
        var score = CreateRect("Score", root);
        score.anchorMin = score.anchorMax = new Vector2(0.5f, 1f);
        score.pivot = new Vector2(0.5f, 1f);
        score.anchoredPosition = new Vector2(0, -86);
        score.sizeDelta = new Vector2(520, 40);
        _scoreText = CreateText("ScoreText", score, "", 24, new Color(1, 1, 1, 0.85f));
        Stretch(_scoreText.rectTransform);

        // ---- 결과 패널 ----
        var dim = CreateRect("Result", root);
        Stretch(dim);
        var dimImage = dim.gameObject.AddComponent<Image>();
        dimImage.color = new Color(0, 0, 0, 0.6f); // 뒤쪽 카드 조작 차단
        _resultRoot = dim.gameObject;

        var panel = CreateRect("Panel", dim);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(720, 460);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        _resultTitle = CreateText("Title", panel, "", 96, Color.white);
        Place(_resultTitle.rectTransform, new Vector2(0, 120), new Vector2(680, 130));
        _resultTitle.fontStyle = FontStyles.Bold;

        _resultSub = CreateText("Sub", panel, "", 30, new Color(1, 1, 1, 0.8f));
        Place(_resultSub.rectTransform, new Vector2(0, 30), new Vector2(680, 50));

        _resultStatus = CreateText("Status", panel, "", 24, new Color(1, 1, 1, 0.6f));
        Place(_resultStatus.rectTransform, new Vector2(0, -30), new Vector2(680, 40));

        _primaryButton = CreateButton("Primary", panel, new Vector2(-150, -140), out _primaryLabel);
        _primaryButton.onClick.AddListener(() => _onPrimary?.Invoke());
        _secondaryButton = CreateButton("Secondary", panel, new Vector2(150, -140), out _secondaryLabel);
        _secondaryButton.onClick.AddListener(() => _onSecondary?.Invoke());

        _resultRoot.SetActive(false);
        SetTurn(null, false);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <param name="text">null이면 배지를 숨깁니다.</param>
    public void SetTurn(string text, bool isMine)
    {
        bool show = !string.IsNullOrEmpty(text);
        _turnBadge.gameObject.SetActive(show);
        if (!show) return;
        _turnText.text = text;
        _turnText.color = isMine ? MyColor : EnemyColor;
    }

    public void SetScore(int mine, int enemy, string myName = "나", string enemyName = "상대")
    {
        _scoreText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(MyColor)}>{myName} {mine}</color>   경계석   <color=#{ColorUtility.ToHtmlStringRGB(EnemyColor)}>{enemy} {enemyName}</color>";
    }

    public void ShowResult(string title, bool isWin, string sub,
        string primaryLabel, Action onPrimary,
        string secondaryLabel, Action onSecondary)
    {
        _resultTitle.text = title;
        _resultTitle.color = isWin ? MyColor : (title == "무승부" ? Color.white : EnemyColor);
        _resultSub.text = sub;
        _resultStatus.text = "";

        _onPrimary = onPrimary;
        _onSecondary = onSecondary;
        _primaryLabel.text = primaryLabel;
        _secondaryLabel.text = secondaryLabel;
        _primaryButton.gameObject.SetActive(onPrimary != null);
        _secondaryButton.gameObject.SetActive(onSecondary != null);
        _primaryButton.interactable = true;

        // 버튼이 하나면 가운데 정렬
        var p = (RectTransform)_primaryButton.transform;
        var s = (RectTransform)_secondaryButton.transform;
        bool both = onPrimary != null && onSecondary != null;
        p.anchoredPosition = new Vector2(both ? -150 : 0, -140);
        s.anchoredPosition = new Vector2(both ? 150 : 0, -140);

        _resultRoot.SetActive(true);
        _resultRoot.transform.SetAsLastSibling();
    }

    public void SetResultStatus(string status, bool primaryInteractable = true)
    {
        _resultStatus.text = status;
        _primaryButton.interactable = primaryInteractable;
    }

    public void HideResult()
    {
        _resultRoot.SetActive(false);
    }

    // ------------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------------

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color)
    {
        var rt = CreateRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.richText = true;
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, Vector2 pos, out TextMeshProUGUI label)
    {
        var rt = CreateRect(name, parent);
        Place(rt, pos, new Vector2(260, 80));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = ButtonColor;
        var btn = rt.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        btn.colors = colors;
        label = CreateText("Label", rt, "", 32, ButtonTextColor);
        Stretch(label.rectTransform);
        return btn;
    }
}
