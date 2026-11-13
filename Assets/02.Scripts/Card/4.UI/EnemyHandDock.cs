using UnityEngine;

/// <summary>
/// 상대 손패를 줄여서 화면 왼쪽 여백(상대 정보 아래)에 고정합니다.
/// 원래 위치에서는 손패가 상대 쪽 경계석 칸을 가려서, 모바일 화면에서 판이 잘 보이지 않았습니다.
/// 화면비에 따라 왼쪽 여백 크기가 달라지므로 크기와 위치를 매 프레임 카메라 기준으로 다시 계산합니다.
/// (카드 내기/뽑기 연출은 슬롯의 월드 좌표를 쓰므로 그대로 동작합니다)
/// </summary>
public class EnemyHandDock : MonoBehaviour
{
    [Tooltip("보드(경계석 9개)의 왼쪽 끝 x (월드). 손패는 이보다 왼쪽에만 놓입니다.")]
    public float BoardLeftEdge = -6.4f;

    [Tooltip("배율 1일 때 손패(부채꼴 6장)의 크기 (월드 단위)")]
    public Vector2 HandSizeAtScale1 = new Vector2(7.3f, 2.2f);

    public float MinScale = 0.25f;
    public float MaxScale = 0.5f;

    [Tooltip("화면 위쪽에서 상대 정보 패널 아래까지의 거리 (카메라 세로 크기 5 기준)")]
    public float TopInset = 1.6f;

    private Camera _cam;
    private Vector3 _baseLocalScale;

    private void Start()
    {
        _cam = Camera.main;
        _baseLocalScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;
        float k = h / 5f; // UI는 화면 높이에 맞춰 커지므로 위쪽 여백도 같은 비율로
        var c = _cam.transform.position;

        const float margin = 0.3f;
        float gap = (BoardLeftEdge - (c.x - w)) - margin * 2; // 화면 왼쪽 끝 ~ 보드 왼쪽 끝 사이 여백
        float scale = Mathf.Clamp(gap / HandSizeAtScale1.x, MinScale, MaxScale);
        transform.localScale = _baseLocalScale * scale;

        float halfW = HandSizeAtScale1.x * scale * 0.5f;
        float halfH = HandSizeAtScale1.y * scale * 0.5f;
        var p = transform.position;
        p.x = c.x - w + margin + halfW;
        p.y = c.y + h - TopInset * k - halfH;
        transform.position = p;
    }
}
