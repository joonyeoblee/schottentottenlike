using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// 규칙 안내 튜토리얼 (5페이지). 처음 AI 대전을 시작할 때 자동으로 열리고, HUD의 "?" 버튼으로 언제든 다시 볼 수 있습니다.
/// 런타임에 UI를 생성하며, 족보 예시는 실제 카드 이미지(Addressables)를 사용합니다.
/// </summary>
public class UI_Tutorial : MonoBehaviour
{
    private const string DoneKey = "tutorial_done_v1";

    public static bool IsDone => PlayerPrefs.GetInt(DoneKey, 0) == 1;
    public static bool IsOpen => _instance != null;
    private static UI_Tutorial _instance;

    private TMP_FontAsset _font;
    private bool _pauseTime;
    private float _prevTimeScale = 1f;
    private Action _onClose;

    private RectTransform _panel;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _body;
    private RectTransform _art;
    private RectTransform _dots;
    private Button _prev;
    private Button _next;
    private TextMeshProUGUI _nextLabel;
    private int _page;

    private readonly List<AsyncOperationHandle<Sprite>> _handles = new List<AsyncOperationHandle<Sprite>>();

    private struct Page
    {
        public string Title;
        public string Body;
        public Action<RectTransform> Art;
    }

    private Page[] _pages;

    /// <param name="pauseTime">솔로 모드에서는 true (튜토리얼을 보는 동안 AI가 진행하지 않도록)</param>
    public static UI_Tutorial Show(TMP_FontAsset font, bool pauseTime, Action onClose)
    {
        if (_instance != null) return _instance;
        UI_MatchHud.EnsureEventSystem();

        var go = new GameObject("UI_Tutorial", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2280, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();

        var t = go.AddComponent<UI_Tutorial>();
        _instance = t;
        t._font = font != null ? font : UI_MatchHud.FindSceneFont(go);
        t._pauseTime = pauseTime;
        t._onClose = onClose;
        if (pauseTime) { t._prevTimeScale = Time.timeScale; Time.timeScale = 0f; }
        t.Build();
        t.ShowPage(0);
        return t;
    }

    private void Update()
    {
        // 뒤로가기(ESC) = 튜토리얼 닫기
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
    }

    private void OnDestroy()
    {
        foreach (var h in _handles) if (h.IsValid()) Addressables.Release(h);
        if (_instance == this) _instance = null;
    }

    private void Close()
    {
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        if (_pauseTime) Time.timeScale = _prevTimeScale <= 0 ? 1f : _prevTimeScale;
        var cb = _onClose;
        Destroy(gameObject);
        cb?.Invoke();
    }

    // ------------------------------------------------------------------
    private void Build()
    {
        var dim = UIKit.Rect("Dim", transform);
        UIKit.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);

        _panel = UIKit.Rect("Panel", transform);
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.sizeDelta = new Vector2(1640, 940);
        UIKit.SlicedImage(_panel.gameObject, "UI/Panel", Color.white);

        _title = UIKit.Text(_panel, _font, "", 64, UI_MatchHud.InkColor);
        UIKit.Place(_title.rectTransform, new Vector2(0, 380), new Vector2(1400, 90));
        _title.fontStyle = FontStyles.Bold;

        _art = UIKit.Rect("Art", _panel);
        UIKit.Place(_art, new Vector2(0, 110), new Vector2(1480, 380));

        _body = UIKit.Text(_panel, _font, "", 36, UI_MatchHud.InkColor);
        UIKit.Place(_body.rectTransform, new Vector2(0, -205), new Vector2(1420, 220));
        _body.lineSpacing = 18;

        _dots = UIKit.Rect("Dots", _panel);
        UIKit.Place(_dots, new Vector2(0, -330), new Vector2(400, 30));

        _prev = UIKit.Button(_panel, _font, "UI/Button", new Vector2(-560, -390), new Vector2(280, 110), out var prevLabel);
        prevLabel.text = "이전";
        _prev.onClick.AddListener(() => ShowPage(_page - 1));

        _next = UIKit.Button(_panel, _font, "UI/ButtonGreen", new Vector2(560, -390), new Vector2(320, 110), out _nextLabel);
        _next.onClick.AddListener(() => { if (_page >= _pages.Length - 1) Close(); else ShowPage(_page + 1); });

        // 건너뛰기 (우상단)
        var skip = UIKit.Rect("Skip", _panel);
        UIKit.Place(skip, new Vector2(700, 400), new Vector2(200, 70));
        var skipImg = skip.gameObject.AddComponent<Image>();
        skipImg.color = new Color(0, 0, 0, 0); // 터치 영역만
        var skipBtn = skip.gameObject.AddComponent<Button>();
        skipBtn.onClick.AddListener(Close);
        var skipText = UIKit.Text(skip, _font, "<u>건너뛰기</u>", 30, new Color(UI_MatchHud.InkColor.r, UI_MatchHud.InkColor.g, UI_MatchHud.InkColor.b, 0.7f));
        UIKit.Stretch(skipText.rectTransform);

        _pages = new[]
        {
            new Page
            {
                Title = "경계석 대전에 오신 걸 환영해요!",
                Body = "가운데 놓인 <b>경계석 9개</b>를 두고 겨루는 1:1 카드 게임이에요.\n" +
                       "<color=#4E9F61><b>이웃한 경계석 3개</b></color>를 차지하거나, 아무 곳이든 <color=#4E9F61><b>5개</b></color>를 먼저 차지하면 승리!",
                Art = ArtGoal
            },
            new Page
            {
                Title = "카드 내기",
                Body = "내 차례마다 손패에서 카드 <b>1장</b>을 끌어서 경계석 <b>내 쪽(아래) 칸</b>에 놓아요.\n" +
                       "경계석 하나에 <b>3장</b>까지 놓을 수 있고, 카드를 내면 자동으로 1장을 뽑아요.",
                Art = ArtPlay
            },
            new Page
            {
                Title = "족보 (3장 조합)",
                Body = "경계석마다 내 3장과 상대 3장을 비교해요. <b>왼쪽일수록 강한 조합</b>이에요.\n" +
                       "족보가 같으면 <b>숫자 합</b>이 큰 쪽이, 합도 같으면 <b>먼저 3장을 완성한 쪽</b>이 이겨요.",
                Art = ArtRanks
            },
            new Page
            {
                Title = "경계석 차지하기",
                Body = "양쪽 3장이 모두 놓이면 더 강한 쪽이 경계석을 차지해요.\n" +
                       "상대가 아직 3장을 다 놓지 않았어도, <b>남은 카드로 절대 나를 이길 수 없으면</b> 바로 내 것이 돼요!",
                Art = ArtClaim
            },
            new Page
            {
                Title = "준비 완료!",
                Body = "차지한 경계석은 <color=#C9A040><b>금빛</b></color>, 상대가 차지한 경계석은 <color=#C74C42><b>붉은빛</b></color>으로 변해요.\n" +
                       "이 안내는 처음 한 번만 나와요. 게임 중 오른쪽 위 <b>규칙</b> 버튼이나 로비의 <b>게임 규칙</b>에서 다시 볼 수 있어요.",
                Art = ArtReady
            },
        };

        for (int i = 0; i < _pages.Length; i++)
        {
            var d = UIKit.Rect("Dot", _dots);
            UIKit.Place(d, new Vector2((i - (_pages.Length - 1) / 2f) * 44, 0), new Vector2(22, 22));
            d.gameObject.AddComponent<Image>().sprite = UIKit.Sprite("UI/RoundButton");
        }
    }

    private void ShowPage(int index)
    {
        _page = Mathf.Clamp(index, 0, _pages.Length - 1);
        var p = _pages[_page];
        _title.text = p.Title;
        _body.text = p.Body;

        for (int i = _art.childCount - 1; i >= 0; i--) Destroy(_art.GetChild(i).gameObject);
        p.Art?.Invoke(_art);

        for (int i = 0; i < _dots.childCount; i++)
        {
            var img = _dots.GetChild(i).GetComponent<Image>();
            img.color = i == _page ? new Color(0.3f, 0.62f, 0.38f) : new Color(1, 1, 1, 0.6f);
            _dots.GetChild(i).localScale = Vector3.one * (i == _page ? 1.3f : 1f);
        }

        _prev.gameObject.SetActive(_page > 0);
        _nextLabel.text = _page >= _pages.Length - 1 ? "시작하기" : "다음";
    }

    // ------------------------------------------------------------------
    // 페이지 그림
    // ------------------------------------------------------------------

    private Image StoneIcon(RectTransform parent, Vector2 pos, float width, Color tint)
    {
        var rt = UIKit.Rect("Stone", parent);
        UIKit.Place(rt, pos, new Vector2(width, width * 256f / 360f));
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = UIKit.Sprite("UI/Stone");
        img.color = tint;
        img.preserveAspect = true;
        return img;
    }

    private Image Card(RectTransform parent, string address, Vector2 pos, float height, float angle = 0)
    {
        var rt = UIKit.Rect(address, parent);
        UIKit.Place(rt, pos, new Vector2(height * 880f / 1287f, height));
        rt.localRotation = Quaternion.Euler(0, 0, angle);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0); // 로드 전에는 숨김
        img.preserveAspect = true;
        var h = Addressables.LoadAssetAsync<Sprite>(address);
        _handles.Add(h);
        h.Completed += op =>
        {
            if (img == null || op.Status != AsyncOperationStatus.Succeeded) return;
            img.sprite = op.Result;
            img.color = Color.white;
        };
        return img;
    }

    private TextMeshProUGUI Label(RectTransform parent, string text, Vector2 pos, float size, Color color, float width = 300)
    {
        var t = UIKit.Text(parent, _font, text, size, color);
        UIKit.Place(t.rectTransform, pos, new Vector2(width, size * 1.6f));
        return t;
    }

    private void ArtGoal(RectTransform art)
    {
        // 위: 이웃한 3개, 아래: 아무 곳 5개
        bool[] row1 = { false, false, false, true, true, true, false, false, false };
        bool[] row2 = { true, false, true, false, true, false, false, true, true };
        for (int i = 0; i < 9; i++)
        {
            float x = -560 + i * 140;
            StoneIcon(art, new Vector2(x + 120, 95), 120, row1[i] ? RoundSlot.MyStone : new Color(1, 1, 1, 0.55f));
            StoneIcon(art, new Vector2(x + 120, -95), 120, row2[i] ? RoundSlot.MyStone : new Color(1, 1, 1, 0.55f));
        }
        Label(art, "이웃한 3개", new Vector2(-640, 95), 34, UI_MatchHud.MyColor, 220).fontStyle = FontStyles.Bold;
        Label(art, "아무 곳 5개", new Vector2(-640, -95), 34, UI_MatchHud.MyColor, 220).fontStyle = FontStyles.Bold;
    }

    private void ArtPlay(RectTransform art)
    {
        // 손패 카드 → 경계석 아래 칸
        StoneIcon(art, new Vector2(300, 120), 200, Color.white);
        var slot = UIKit.Rect("Slot", art);
        UIKit.Place(slot, new Vector2(300, -70), new Vector2(150, 215));
        var si = slot.gameObject.AddComponent<Image>();
        si.sprite = UIKit.Sprite("UI/Slot");
        si.color = new Color(1f, 0.92f, 0.55f, 1f);

        Card(art, "Purple_4", new Vector2(-420, -40), 260, 8);
        Card(art, "Red_7", new Vector2(-300, -20), 280, -4);

        var arrow = Label(art, "→", new Vector2(20, -40), 150, UI_MatchHud.MyColor, 260);
        arrow.fontStyle = FontStyles.Bold;
        Label(art, "끌어서 놓기", new Vector2(20, 70), 32, UI_MatchHud.InkColor, 260);
    }

    private void ArtRanks(RectTransform art)
    {
        var groups = new[]
        {
            (name: "같은 색\n연속 숫자", cards: new[] { "Red_3", "Red_4", "Red_5" }),
            (name: "같은 숫자\n3장", cards: new[] { "Blue_7", "Green_7", "Yellow_7" }),
            (name: "같은 색\n3장", cards: new[] { "Purple_1", "Purple_5", "Purple_9" }),
            (name: "연속 숫자", cards: new[] { "Brown_4", "Blue_5", "Red_6" }),
            (name: "그 외\n(숫자 합)", cards: new[] { "Yellow_9", "Green_8", "Brown_2" }),
        };
        for (int g = 0; g < groups.Length; g++)
        {
            float cx = -592 + g * 296;
            for (int c = 0; c < 3; c++)
                Card(art, groups[g].cards[c], new Vector2(cx - 62 + c * 62, 40), 190, (c - 1) * -7f);
            var num = Label(art, (g + 1).ToString(), new Vector2(cx - 118, 150), 40, Color.white, 60);
            num.outlineWidth = 0.25f; num.outlineColor = UI_MatchHud.InkColor; num.fontStyle = FontStyles.Bold;
            Label(art, groups[g].name, new Vector2(cx, -120), 28, UI_MatchHud.InkColor, 280).fontStyle = FontStyles.Bold;
        }
        Label(art, "강함", new Vector2(-560, -185), 26, UI_MatchHud.MyColor, 200);
        Label(art, "약함", new Vector2(600, -185), 26, UI_MatchHud.EnemyColor, 200);
    }

    private void ArtClaim(RectTransform art)
    {
        // 상대(위) 같은 숫자 3장 vs 나(아래) 같은 색 연속 → 내가 차지
        for (int c = 0; c < 3; c++)
        {
            Card(art, new[] { "Green_9", "Yellow_9", "Red_9" }[c], new Vector2(-380 + c * 80, 0), 220, (c - 1) * -6f);
            Card(art, new[] { "Blue_6", "Blue_7", "Blue_8" }[c], new Vector2(260 + c * 80, 0), 220, (c - 1) * -6f);
        }
        Label(art, "상대: 같은 숫자 3장", new Vector2(-300, -160), 30, UI_MatchHud.EnemyColor, 420);
        Label(art, "나: 같은 색 연속 숫자", new Vector2(340, -160), 30, UI_MatchHud.MyColor, 420);
        Label(art, "<", new Vector2(-20, 0), 120, UI_MatchHud.InkColor, 120).fontStyle = FontStyles.Bold;
        StoneIcon(art, new Vector2(-20, 140), 150, RoundSlot.MyStone);
    }

    private void ArtReady(RectTransform art)
    {
        StoneIcon(art, new Vector2(-260, 20), 260, RoundSlot.MyStone);
        StoneIcon(art, new Vector2(0, 20), 260, Color.white);
        StoneIcon(art, new Vector2(260, 20), 260, RoundSlot.EnemyStone);
        Label(art, "내 것", new Vector2(-260, -110), 32, UI_MatchHud.MyColor, 200).fontStyle = FontStyles.Bold;
        Label(art, "미점령", new Vector2(0, -110), 32, UI_MatchHud.InkColor, 200).fontStyle = FontStyles.Bold;
        Label(art, "상대 것", new Vector2(260, -110), 32, UI_MatchHud.EnemyColor, 200).fontStyle = FontStyles.Bold;
    }
}
