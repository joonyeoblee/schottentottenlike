using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI 대전 난이도 선택 창 (쉬움 / 보통 / 어려움). 로비의 "AI 방" 버튼에서 열립니다.
/// 마지막으로 고른 난이도가 강조되어 있고, 고르면 저장 후 AI 대전을 시작합니다.
/// </summary>
public class UI_DifficultyPopup : MonoBehaviour
{
    private Action<AIDifficulty> _onSelect;

    public static UI_DifficultyPopup Show(Action<AIDifficulty> onSelect)
    {
        UI_MatchHud.EnsureEventSystem();
        var go = new GameObject("UI_DifficultyPopup", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2280, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();

        var popup = go.AddComponent<UI_DifficultyPopup>();
        popup._onSelect = onSelect;
        popup.Build(UI_MatchHud.FindSceneFont(go));
        return popup;
    }

    private void Build(TMP_FontAsset font)
    {
        // 바깥 영역을 누르면 닫힘
        var dim = UIKit.Rect("Dim", transform);
        UIKit.Stretch(dim);
        dim.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);

        var panel = UIKit.Rect("Panel", transform);
        UIKit.Place(panel, Vector2.zero, new Vector2(1500, 760));
        UIKit.SlicedImage(panel.gameObject, "UI/Panel", Color.white);

        var title = UIKit.Text(panel, font, "AI 난이도 선택", 64, UI_MatchHud.InkColor);
        UIKit.Place(title.rectTransform, new Vector2(0, 290), new Vector2(1200, 90));
        title.fontStyle = FontStyles.Bold;

        var current = AISettings.Difficulty;
        var levels = new[] { AIDifficulty.Easy, AIDifficulty.Normal, AIDifficulty.Hard };
        var colors = new[] { new Color(0.55f, 0.82f, 0.6f), new Color(0.98f, 0.82f, 0.45f), new Color(0.95f, 0.55f, 0.48f) };
        var stars = new[] { "Lv.1", "Lv.2", "Lv.3" };

        for (int i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            var card = UIKit.Rect("Level_" + level, panel);
            UIKit.Place(card, new Vector2(-460 + i * 460, -10), new Vector2(420, 420));
            var img = UIKit.SlicedImage(card.gameObject, "UI/Panel", colors[i]);
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => Select(level));
            card.gameObject.AddComponent<ButtonPress>();

            var s = UIKit.Text(card, font, stars[i], 56, UI_MatchHud.InkColor);
            UIKit.Place(s.rectTransform, new Vector2(0, 130), new Vector2(380, 70));
            var name = UIKit.Text(card, font, AISettings.DisplayName(level), 72, UI_MatchHud.InkColor);
            UIKit.Place(name.rectTransform, new Vector2(0, 40), new Vector2(380, 100));
            name.fontStyle = FontStyles.Bold;
            var desc = UIKit.Text(card, font, AISettings.Description(level), 28, UI_MatchHud.InkColor);
            UIKit.Place(desc.rectTransform, new Vector2(0, -90), new Vector2(360, 150));

            if (level == current)
            {
                var badge = UIKit.Text(card, font, "<b>최근 선택</b>", 26, UI_MatchHud.MyColor);
                UIKit.Place(badge.rectTransform, new Vector2(0, -180), new Vector2(360, 40));
                card.localScale = Vector3.one * 1.04f;
            }
        }

        var close = UIKit.Button(panel, font, "UI/Button", new Vector2(0, -320), new Vector2(260, 100), out var closeLabel);
        closeLabel.text = "닫기";
        close.onClick.AddListener(Close);
    }

    private void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Close(); // 안드로이드 뒤로가기
    }

    private void Select(AIDifficulty level)
    {
        AISettings.Difficulty = level;
        var cb = _onSelect;
        Destroy(gameObject);
        cb?.Invoke(level);
    }

    private void Close() => Destroy(gameObject);
}
