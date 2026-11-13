using UnityEngine;

/// <summary>
/// RectTransform을 기기의 안전 영역(노치/펀치홀/둥근 모서리 제외 영역)에 맞춥니다.
/// 화면 회전(가로 왼쪽↔오른쪽)으로 노치 위치가 바뀌면 자동으로 다시 맞춥니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private Rect _applied;
    private Vector2Int _screen;

    private void OnEnable() => Apply();

    private void Update()
    {
        if (Screen.safeArea != _applied || _screen.x != Screen.width || _screen.y != Screen.height)
            Apply();
    }

    private void Apply()
    {
        var rt = (RectTransform)transform;
        Rect safe = Screen.safeArea;
        _applied = safe;
        _screen = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = safe.position, max = safe.position + safe.size;
        min.x /= Screen.width; min.y /= Screen.height;
        max.x /= Screen.width; max.y /= Screen.height;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// 기존 씬 UI 요소(전체화면 배경 제외)를 안전 영역 안으로 밀어 넣습니다.
/// 레이아웃 계층을 바꾸지 않고 anchoredPosition만 보정하므로 기존 씬 구성은 그대로 유지됩니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaNudge : MonoBehaviour
{
    private Vector2 _basePos;
    private bool _captured;
    private Rect _applied;

    private void OnEnable()
    {
        if (!_captured) { _basePos = ((RectTransform)transform).anchoredPosition; _captured = true; }
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != _applied) Apply();
    }

    private void Apply()
    {
        var rt = (RectTransform)transform;
        _applied = Screen.safeArea;
        var canvas = GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.rootCanvas.scaleFactor : 1f;
        if (scale <= 0) scale = 1f;

        Rect s = Screen.safeArea;
        float left = s.xMin / scale, right = (Screen.width - s.xMax) / scale;
        float bottom = s.yMin / scale, top = (Screen.height - s.yMax) / scale;

        Vector2 a = (rt.anchorMin + rt.anchorMax) * 0.5f;
        Vector2 offset = Vector2.zero;
        if (a.x < 0.34f) offset.x = left; else if (a.x > 0.66f) offset.x = -right;
        if (a.y < 0.34f) offset.y = bottom; else if (a.y > 0.66f) offset.y = -top;
        rt.anchoredPosition = _basePos + offset;
    }
}
