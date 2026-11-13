using UnityEngine;

/// <summary>
/// 게임 보드가 어떤 화면비(폰 16:9~21:9, 태블릿 4:3)에서도 잘리지 않도록 직교 카메라 크기를 조절합니다.
/// 기준 화면(19:9)에서는 원래 크기를 그대로 쓰고, 더 좁은 화면에서만 카메라를 넓힙니다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFitter : MonoBehaviour
{
    [Tooltip("항상 보여야 하는 절반 너비 (월드 단위). 경계석 9개(±6.3) + 오른쪽 덱(약 8.8)")]
    public float RequiredHalfWidth = 9.0f;

    private Camera _cam;
    private float _baseSize;
    private float _lastAspect;

    [Tooltip("최소 카메라 세로 크기. 위/아래 손패가 보드를 가리지 않고 잘 보이도록 기본(5)보다 조금 넓게 봅니다.")]
    public float MinSize = 6f;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _baseSize = Mathf.Max(_cam.orthographicSize, MinSize);
        Fit();
    }

    private void Update()
    {
        if (!Mathf.Approximately(_lastAspect, _cam.aspect)) Fit();
    }

    private void Fit()
    {
        if (_cam == null || !_cam.orthographic) return;
        _lastAspect = _cam.aspect;
        _cam.orthographicSize = Mathf.Max(_baseSize, RequiredHalfWidth / Mathf.Max(0.1f, _cam.aspect));
    }
}
