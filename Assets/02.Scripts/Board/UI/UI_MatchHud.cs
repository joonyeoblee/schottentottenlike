using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// 게임 보드 위에 표시되는 HUD(턴 표시, 점령 현황, 힌트, 도움말 버튼)와 결과/확인 패널.
/// 씬/프리팹 수정 없이 런타임에 생성되므로 솔로(AI)와 멀티 양쪽에서 공용으로 사용합니다.
/// 아트는 Resources/UI 의 스프라이트(Panel, Button, ButtonGreen, RoundButton)를 사용합니다.
/// </summary>
public class UI_MatchHud : MonoBehaviour
{
    public static readonly Color MyColor = new Color(0.30f, 0.62f, 0.38f);
    public static readonly Color EnemyColor = new Color(0.78f, 0.30f, 0.26f);
    public static readonly Color InkColor = new Color(0.23f, 0.15f, 0.08f);

    private TMP_FontAsset _font;

    private Image _turnBadge;
    private TextMeshProUGUI _turnText;
    private TextMeshProUGUI _scoreText;

    private RectTransform _hintRoot;
    private TextMeshProUGUI _hintText;
    private Coroutine _hintRoutine;

    private GameObject _resultRoot;
    private RectTransform _resultPanel;
    private TextMeshProUGUI _resultTitle;
    private TextMeshProUGUI _resultSub;
    private TextMeshProUGUI _resultStatus;
    private Button _primaryButton;
    private TextMeshProUGUI _primaryLabel;
    private Button _secondaryButton;
    private TextMeshProUGUI _secondaryLabel;
    private Action _onPrimary;
    private Action _onSecondary;

    /// <summary>"?" 버튼을 눌렀을 때</summary>
    public event Action OnHelp;
    /// <summary>안드로이드 뒤로가기(ESC)를 눌렀을 때. 결과 패널이 떠 있으면 호출되지 않습니다.</summary>
    public event Action OnBack;

    public bool IsResultVisible => _resultRoot != null && _resultRoot.activeSelf;
    public TMP_FontAsset Font => _font;

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
        scaler.referenceResolution = new Vector2(2280, 1080);
        scaler.matchWidthOrHeight = 1f; // 가로 모드 기준: 세로 높이에 맞춰 스케일
        go.AddComponent<GraphicRaycaster>();

        var hud = go.AddComponent<UI_MatchHud>();
        hud.Build();
        return hud;
    }

    public static void EnsureEventSystem()
    {
        if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(es);
    }

    public static TMP_FontAsset FindSceneFont(GameObject exclude = null)
    {
        // 씬에 이미 있는 TMP 텍스트의 폰트를 그대로 사용 (한글 글리프 포함 폰트)
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.font != null && t.gameObject != exclude) return t.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private void Build()
    {
        _font = FindSceneFont(gameObject);

        // 노치/펀치홀을 피하는 안전 영역
        var safe = UIKit.Rect("SafeArea", transform);
        UIKit.Stretch(safe);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        // ---- 턴 배지 (오른쪽, 덱 위) ----
        // 화면 위 가운데는 상대 손패 자리이므로, 턴 표시는 하스스톤의 턴 버튼처럼 오른쪽에 둡니다.
        var badge = UIKit.Rect("TurnBadge", safe);
        badge.anchorMin = badge.anchorMax = new Vector2(1f, 0.5f);
        badge.pivot = new Vector2(1f, 0.5f);
        badge.anchoredPosition = new Vector2(-24, 190);
        badge.sizeDelta = new Vector2(250, 84);
        _turnBadge = UIKit.SlicedImage(badge.gameObject, "UI/Button", Color.white);
        _turnBadge.raycastTarget = false;
        _turnText = UIKit.Text(badge, _font, "", 38, InkColor);
        UIKit.Stretch(_turnText.rectTransform, 16, 16, 0, 10);
        _turnText.fontStyle = FontStyles.Bold;
        _turnText.enableAutoSizing = true; // "상대를 기다리는 중" 같은 긴 문구도 배지 안에
        _turnText.fontSizeMin = 20;
        _turnText.fontSizeMax = 38;

        // ---- 점령 현황 (오른쪽, 덱 아래) ----
        var score = UIKit.Rect("Score", safe);
        score.anchorMin = score.anchorMax = new Vector2(1f, 0.5f);
        score.pivot = new Vector2(1f, 0.5f);
        score.anchoredPosition = new Vector2(-24, -180);
        score.sizeDelta = new Vector2(250, 92);
        var scoreBg = UIKit.SlicedImage(score.gameObject, "UI/Panel", new Color(1, 1, 1, 0.92f));
        scoreBg.raycastTarget = false;
        _scoreText = UIKit.Text(score, _font, "", 26, InkColor);
        UIKit.Stretch(_scoreText.rectTransform);

        // ---- 도움말 버튼 (우상단, 기존 일시정지 버튼 왼쪽) ----
        var help = UIKit.Rect("HelpButton", safe);
        help.anchorMin = help.anchorMax = new Vector2(1f, 1f);
        help.pivot = new Vector2(1f, 1f);
        help.anchoredPosition = new Vector2(-250, -28);
        help.sizeDelta = new Vector2(96, 96);
        UIKit.SlicedImage(help.gameObject, "UI/RoundButton", Color.white, false);
        var helpBtn = help.gameObject.AddComponent<Button>();
        helpBtn.onClick.AddListener(() => OnHelp?.Invoke());
        var q = UIKit.Text(help, _font, "?", 54, InkColor);
        UIKit.Stretch(q.rectTransform);
        q.fontStyle = FontStyles.Bold;

        // ---- 힌트 배너 (화면 가운데, 잠깐 떴다 사라짐) ----
        _hintRoot = UIKit.Rect("Hint", safe);
        _hintRoot.anchorMin = _hintRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _hintRoot.pivot = new Vector2(0.5f, 0.5f);
        _hintRoot.anchoredPosition = new Vector2(0, 0);
        _hintRoot.sizeDelta = new Vector2(1000, 76);
        UIKit.SlicedImage(_hintRoot.gameObject, "UI/Panel", Color.white).raycastTarget = false;
        _hintText = UIKit.Text(_hintRoot, _font, "", 30, InkColor);
        UIKit.Stretch(_hintText.rectTransform, 30, 30, 0, 0);
        _hintRoot.gameObject.SetActive(false);

        // ---- 결과 / 확인 패널 ----
        var dim = UIKit.Rect("Result", transform);
        UIKit.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.62f); // 뒤쪽 카드 조작 차단
        _resultRoot = dim.gameObject;

        _resultPanel = UIKit.Rect("Panel", dim);
        _resultPanel.anchorMin = _resultPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _resultPanel.sizeDelta = new Vector2(860, 560);
        UIKit.SlicedImage(_resultPanel.gameObject, "UI/Panel", Color.white);

        _resultTitle = UIKit.Text(_resultPanel, _font, "", 110, Color.white);
        UIKit.Place(_resultTitle.rectTransform, new Vector2(0, 140), new Vector2(800, 150));
        _resultTitle.fontStyle = FontStyles.Bold;
        _resultTitle.outlineWidth = 0.18f;
        _resultTitle.outlineColor = InkColor;

        _resultSub = UIKit.Text(_resultPanel, _font, "", 34, InkColor);
        UIKit.Place(_resultSub.rectTransform, new Vector2(0, 30), new Vector2(780, 60));

        _resultStatus = UIKit.Text(_resultPanel, _font, "", 28, new Color(InkColor.r, InkColor.g, InkColor.b, 0.7f));
        UIKit.Place(_resultStatus.rectTransform, new Vector2(0, -40), new Vector2(780, 44));

        _primaryButton = UIKit.Button(_resultPanel, _font, "UI/ButtonGreen", new Vector2(-180, -170), new Vector2(320, 120), out _primaryLabel);
        _primaryButton.onClick.AddListener(() => _onPrimary?.Invoke());
        _secondaryButton = UIKit.Button(_resultPanel, _font, "UI/Button", new Vector2(180, -170), new Vector2(320, 120), out _secondaryLabel);
        _secondaryButton.onClick.AddListener(() => _onSecondary?.Invoke());

        _resultRoot.SetActive(false);
        SetTurn(null, false);
    }

    private void Update()
    {
        // 안드로이드 뒤로가기 버튼은 Input System에서 ESC 키로 들어옵니다.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !UI_Tutorial.IsOpen)
        {
            if (IsResultVisible)
            {
                // 확인 대화상자가 떠 있으면 '계속하기'와 같은 동작 (결과 화면에서는 무시)
                if (_isConfirmDialog) { HideResult(); }
            }
            else
            {
                OnBack?.Invoke();
            }
        }
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
        _turnBadge.color = isMine ? new Color(0.75f, 1f, 0.78f) : new Color(1f, 0.72f, 0.68f);
        _turnBadge.sprite = UIKit.Sprite(isMine ? "UI/ButtonGreen" : "UI/Button");
        _turnBadge.color = isMine ? Color.white : new Color(1f, 0.8f, 0.78f);
        _turnText.color = isMine ? Color.white : InkColor;
        _turnText.outlineWidth = isMine ? 0.2f : 0f;
        _turnText.outlineColor = InkColor;
    }

    public void SetScore(int mine, int enemy, string myName = "나", string enemyName = "상대")
    {
        _scoreText.text = $"<size=20>경계석</size>\n<color=#{ColorUtility.ToHtmlStringRGB(MyColor)}><b>{myName} {mine}</b></color> : <color=#{ColorUtility.ToHtmlStringRGB(EnemyColor)}><b>{enemy} {enemyName}</b></color>";
    }

    /// <summary>화면 가운데 힌트 배너. 판을 오래 가리지 않도록 항상 잠깐만 보였다가 사라집니다.</summary>
    public void ShowHint(string text, float duration = 3.5f)
    {
        if (duration <= 0) duration = 3.5f;
        if (_hintRoutine != null) StopCoroutine(_hintRoutine);
        _hintText.text = text;
        _hintRoot.gameObject.SetActive(true);
        _hintRoot.localScale = Vector3.one;
        if (duration > 0) _hintRoutine = StartCoroutine(HideHintAfter(duration));
    }

    public void HideHint()
    {
        if (_hintRoutine != null) StopCoroutine(_hintRoutine);
        _hintRoutine = null;
        _hintRoot.gameObject.SetActive(false);
    }

    private IEnumerator HideHintAfter(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        _hintRoot.gameObject.SetActive(false);
        _hintRoutine = null;
    }

    private bool _isConfirmDialog;

    public void ShowResult(string title, bool isWin, string sub,
        string primaryLabel, Action onPrimary,
        string secondaryLabel, Action onSecondary)
    {
        _isConfirmDialog = false;
        ShowPanel(title, isWin ? MyColor : (title == "무승부" || title == "게임 종료" ? new Color(0.95f, 0.8f, 0.45f) : EnemyColor),
            110, sub, primaryLabel, onPrimary, secondaryLabel, onSecondary);
    }

    /// <summary>확인 대화상자 (뒤로가기 → 나가기 확인 등)</summary>
    public void ShowConfirm(string title, string sub, string yesLabel, Action onYes, string noLabel)
    {
        _isConfirmDialog = true;
        ShowPanel(title, new Color(0.95f, 0.8f, 0.45f), 72, sub, noLabel, HideResult, yesLabel, onYes);
    }

    private void ShowPanel(string title, Color titleColor, float titleSize, string sub,
        string primaryLabel, Action onPrimary, string secondaryLabel, Action onSecondary)
    {
        _resultTitle.text = title;
        _resultTitle.color = titleColor;
        _resultTitle.fontSize = titleSize;
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
        p.anchoredPosition = new Vector2(both ? -180 : 0, -170);
        s.anchoredPosition = new Vector2(both ? 180 : 0, -170);

        _resultRoot.SetActive(true);
        _resultRoot.transform.SetAsLastSibling();
        StartCoroutine(PopIn(_resultPanel));
    }

    private static IEnumerator PopIn(RectTransform rt)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.unscaledDeltaTime / 0.25f;
            float k = 1 + 0.12f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            rt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(t)) * k;
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    public void SetResultStatus(string status, bool primaryInteractable = true)
    {
        _resultStatus.text = status;
        _primaryButton.interactable = primaryInteractable;
    }

    public void HideResult()
    {
        _isConfirmDialog = false;
        _resultRoot.SetActive(false);
    }
}
