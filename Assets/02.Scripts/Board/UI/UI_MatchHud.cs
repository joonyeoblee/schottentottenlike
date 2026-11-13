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
        // 씬에 이미 있는 TMP 폰트 중 '한글 글리프가 있는' 폰트를 사용
        // (멀티 씬에서는 기본 영문 폰트(LiberationSans)가 먼저 잡혀 한글이 □로 깨졌음)
        TMP_FontAsset fallback = null;
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.font == null || t.gameObject == exclude) continue;
            if (t.font.HasCharacter('경', true, true)) return t.font;
            if (fallback == null) fallback = t.font;
        }
        return fallback != null ? fallback : TMP_Settings.defaultFontAsset;
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

        // ---- 규칙 버튼 (우상단, 기존 나가기 버튼 왼쪽) — 튜토리얼은 첫 플레이에만 자동으로 뜨고, 이후엔 여기서 다시 봄 ----
        var help = UIKit.Rect("RulesButton", safe);
        help.anchorMin = help.anchorMax = new Vector2(1f, 1f);
        help.pivot = new Vector2(1f, 1f);
        help.anchoredPosition = new Vector2(-230, -26);
        help.sizeDelta = new Vector2(150, 84);
        var helpImg = UIKit.SlicedImage(help.gameObject, "UI/Panel", Color.white);
        var helpBtn = help.gameObject.AddComponent<Button>();
        helpBtn.targetGraphic = helpImg;
        helpBtn.onClick.AddListener(() => OnHelp?.Invoke());
        help.gameObject.AddComponent<ButtonPress>();
        var q = UIKit.Text(help, _font, "규칙", 34, InkColor);
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
        UIKit.Place(_resultSub.rectTransform, new Vector2(0, 20), new Vector2(780, 100));

        _resultStatus = UIKit.Text(_resultPanel, _font, "", 28, new Color(InkColor.r, InkColor.g, InkColor.b, 0.7f));
        UIKit.Place(_resultStatus.rectTransform, new Vector2(0, -62), new Vector2(780, 44));

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

    private string _turnBase = "";
    private int _lastTimerShown = -2;

    /// <summary>턴 남은 시간 표시 (음수면 숨김). 10초 이하면 붉게 강조</summary>
    public void SetTimer(float seconds)
    {
        int s = seconds < 0 ? -1 : Mathf.CeilToInt(seconds);
        if (s == _lastTimerShown || !_turnBadge.gameObject.activeSelf) return;
        _lastTimerShown = s;
        _turnText.text = s < 0 ? _turnBase
            : s <= 10 ? $"{_turnBase} <color=#C0392B>{s}</color>"
            : $"{_turnBase} <size=80%>{s}</size>";
    }

    /// <param name="text">null이면 배지를 숨깁니다.</param>
    public void SetTurn(string text, bool isMine)
    {
        bool show = !string.IsNullOrEmpty(text);
        _turnBadge.gameObject.SetActive(show);
        if (!show) return;
        _turnBase = text;
        _lastTimerShown = -2;
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
        if (_reviewButton != null) _reviewButton.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // 상태 배너 (상대 재연결 대기 등, 계속 표시)
    // ------------------------------------------------------------------
    private RectTransform _statusRoot;
    private TextMeshProUGUI _statusText;

    public void ShowStatus(string text)
    {
        if (_statusRoot == null)
        {
            var safe = transform.Find("SafeArea");
            _statusRoot = UIKit.Rect("Status", safe != null ? safe : transform);
            UIKit.Place(_statusRoot, new Vector2(0, 130), new Vector2(1100, 80));
            UIKit.SlicedImage(_statusRoot.gameObject, "UI/Panel", new Color(1f, 0.9f, 0.8f)).raycastTarget = false;
            _statusText = UIKit.Text(_statusRoot, _font, "", 32, InkColor);
            UIKit.Stretch(_statusText.rectTransform, 20, 20, 0, 0);
            _statusText.fontStyle = FontStyles.Bold;
        }
        _statusRoot.gameObject.SetActive(true);
        if (_statusText.text != text) _statusText.text = text;
    }

    public void HideStatus()
    {
        if (_statusRoot != null) _statusRoot.gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------
    // 복기 (게임이 끝난 뒤 경계석별 양쪽 조합 보기)
    // ------------------------------------------------------------------
    private Button _reviewButton;
    private Action _onReview;
    private GameObject _reviewRoot;

    /// <summary>결과 패널 오른쪽 위에 '복기' 버튼 표시</summary>
    public void SetReviewAction(Action onReview)
    {
        _onReview = onReview;
        if (_reviewButton == null)
        {
            var rt = UIKit.Rect("ReviewButton", _resultPanel);
            UIKit.Place(rt, new Vector2(320, 225), new Vector2(170, 76));
            var img = UIKit.SlicedImage(rt.gameObject, "UI/Panel", new Color(0.9f, 0.95f, 1f));
            _reviewButton = rt.gameObject.AddComponent<Button>();
            _reviewButton.targetGraphic = img;
            _reviewButton.onClick.AddListener(() => _onReview?.Invoke());
            rt.gameObject.AddComponent<ButtonPress>();
            var t = UIKit.Text(rt, _font, "복기", 32, InkColor);
            UIKit.Stretch(t.rectTransform);
            t.fontStyle = FontStyles.Bold;
        }
        _reviewButton.gameObject.SetActive(onReview != null);
    }

    public void HideReview()
    {
        if (_reviewRoot != null) Destroy(_reviewRoot);
    }

    private static string RankName(System.Collections.Generic.List<Card> cards)
    {
        if (cards.Count < 3) return $"<size=80%>미완성 ({cards.Count}장)</size>";
        int sum = 0; foreach (var c in cards) sum += c.CardNumber;
        switch (GameManager.EvaluateHand(cards))
        {
            case HandRank.StraightFlush: return "같은 색 연속";
            case HandRank.ThreeOfAKind: return "같은 숫자";
            case HandRank.Flush: return "같은 색";
            case HandRank.Straight: return "연속 숫자";
            default: return $"합 {sum}";
        }
    }

    /// <summary>
    /// 경계석 9개 각각에 대해 양쪽 카드와 조합, 누가 차지했는지 보여줍니다. (owners: 1 = 나, 2 = 상대)
    /// </summary>
    public void ShowReview(RoundSlot[] rounds, int[] owners, string myName, string enemyName)
    {
        HideReview();
        _reviewRoot = UIKit.Rect("Review", transform).gameObject;
        var root = (RectTransform)_reviewRoot.transform;
        UIKit.Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.75f);

        var panel = UIKit.Rect("Panel", root);
        UIKit.Place(panel, Vector2.zero, new Vector2(2120, 960));
        UIKit.SlicedImage(panel.gameObject, "UI/Panel", Color.white);
        // 화면이 좁으면(16:9, 태블릿) 패널 전체를 줄여서 맞춤
        var canvasRect = ((RectTransform)transform).rect;
        float fit = Mathf.Min(1f, (canvasRect.width - 40f) / 2120f, (canvasRect.height - 40f) / 960f);
        if (fit > 0) panel.localScale = Vector3.one * fit;

        var title = UIKit.Text(panel, _font, "대전 복기", 52, InkColor);
        UIKit.Place(title.rectTransform, new Vector2(0, 410), new Vector2(800, 70));
        title.fontStyle = FontStyles.Bold;

        var en = UIKit.Text(panel, _font, enemyName, 30, EnemyColor);
        UIKit.Place(en.rectTransform, new Vector2(-960, 225), new Vector2(160, 60));
        en.fontStyle = FontStyles.Bold;
        var me = UIKit.Text(panel, _font, myName, 30, MyColor);
        UIKit.Place(me.rectTransform, new Vector2(-960, -205), new Vector2(160, 60));
        me.fontStyle = FontStyles.Bold;

        float colW = 205f;
        for (int r = 0; r < rounds.Length; r++)
        {
            float x = -820 + r * colW;
            var mine = new System.Collections.Generic.List<Card>();
            var theirs = new System.Collections.Generic.List<Card>();

            for (int k = 0; k < rounds[r].EnemyCardSlots.Length; k++)
            {
                var s = rounds[r].EnemyCardSlots[k];
                if (!s.IsOccupied) continue;
                theirs.Add(s.Card);
                ReviewCard(panel, s.CardSprite, new Vector2(x - 52 + k * 52, 270), 180f);
            }
            for (int k = 0; k < rounds[r].PlayerCardSlots.Length; k++)
            {
                var s = rounds[r].PlayerCardSlots[k];
                if (!s.IsOccupied) continue;
                mine.Add(s.Card);
                ReviewCard(panel, s.CardSprite, new Vector2(x - 52 + k * 52, -250), 0f);
            }

            var et = UIKit.Text(panel, _font, RankName(theirs), 26, InkColor);
            UIKit.Place(et.rectTransform, new Vector2(x, 145), new Vector2(200, 40));
            var mt = UIKit.Text(panel, _font, RankName(mine), 26, InkColor);
            UIKit.Place(mt.rectTransform, new Vector2(x, -125), new Vector2(200, 40));

            // 경계석 (차지한 쪽 색)
            var stone = UIKit.Rect("Stone", panel);
            UIKit.Place(stone, new Vector2(x, 10), new Vector2(150, 107));
            var si = stone.gameObject.AddComponent<Image>();
            si.sprite = UIKit.Sprite("UI/Stone");
            si.preserveAspect = true;
            int o = owners != null && r < owners.Length ? owners[r] : 0;
            si.color = o == 1 ? RoundSlot.MyStone : o == 2 ? RoundSlot.EnemyStone : new Color(1, 1, 1, 0.6f);
            var label = UIKit.Text(stone, _font, o == 1 ? "<b>내 것</b>" : o == 2 ? "<b>상대</b>" : "", 24, Color.white);
            UIKit.Stretch(label.rectTransform);
            label.outlineWidth = 0.25f; label.outlineColor = InkColor;
        }

        var close = UIKit.Button(panel, _font, "UI/Button", new Vector2(0, -410), new Vector2(260, 96), out var closeLabel);
        closeLabel.text = "닫기";
        close.onClick.AddListener(HideReview);
    }

    private void ReviewCard(RectTransform parent, Sprite sprite, Vector2 pos, float rotation)
    {
        var rt = UIKit.Rect("Card", parent);
        UIKit.Place(rt, pos, new Vector2(74, 108));
        rt.localRotation = Quaternion.Euler(0, 0, rotation);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = sprite != null ? Color.white : new Color(1, 1, 1, 0.2f);
    }
}
