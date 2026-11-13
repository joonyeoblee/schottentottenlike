using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 런타임 UI 생성 헬퍼. HUD/튜토리얼에서 같은 스타일(Resources/UI 스프라이트)을 쓰기 위해 공용으로 사용합니다.
/// </summary>
public static class UIKit
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Sprite(string path)
    {
        if (Cache.TryGetValue(path, out var s) && s != null) return s;
        s = Resources.Load<Sprite>(path);
        Cache[path] = s;
        return s;
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static Image SlicedImage(GameObject go, string spritePath, Color color, bool sliced = true)
    {
        var img = go.AddComponent<Image>();
        img.sprite = Sprite(spritePath);
        img.type = sliced && img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = img.sprite != null ? color : new Color(0.1f, 0.08f, 0.06f, 0.9f); // 스프라이트가 없을 때 대비
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string text, float size, Color color)
    {
        var rt = Rect("Text", parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    public static Button Button(Transform parent, TMP_FontAsset font, string spritePath, Vector2 pos, Vector2 size, out TextMeshProUGUI label)
    {
        var rt = Rect("Button", parent);
        Place(rt, pos, size);
        var img = SlicedImage(rt.gameObject, spritePath, Color.white);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        btn.colors = colors;
        label = Text(rt, font, "", 38, Color.white);
        Stretch(label.rectTransform, 10, 10, 0, 14);
        label.fontStyle = FontStyles.Bold;
        label.outlineWidth = 0.22f;
        label.outlineColor = UI_MatchHud.InkColor;
        rt.gameObject.AddComponent<ButtonPress>();
        return btn;
    }
}

/// <summary>버튼을 누를 때 살짝 눌리는 연출 (모바일 터치 피드백)</summary>
public class ButtonPress : MonoBehaviour, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
{
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) => transform.localScale = Vector3.one * 0.94f;
    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) => transform.localScale = Vector3.one;
    private void OnDisable() => transform.localScale = Vector3.one;
}
