using UnityEngine;

/// <summary>
/// 손패를 화면 위/아래 가장자리에 붙여서 일부만 보이게 배치합니다. (하스스톤식 손패 배치)
/// - 상대 손패: 화면 위 가운데, 뒤집힌 부채꼴로 카드 아랫부분이 내려와 보임
/// - 내 손패: 화면 아래 가운데, 카드 윗부분(숫자/그림)이 올라와 보이고 누르면 떠오름
/// 경계석 칸을 가리지 않도록 '보이는 높이'를 제한하고, 화면비가 바뀌어도 카메라 기준으로 다시 계산합니다.
/// (카드 내기/뽑기 연출은 슬롯의 월드 좌표를 쓰므로 그대로 동작합니다)
/// </summary>
public class HandDock : MonoBehaviour
{
    public enum Edge { Top, Bottom }

    public Edge Anchor = Edge.Bottom;

    [Tooltip("원래 크기 대비 배율")]
    public float Scale = 1f;

    [Tooltip("손패 높이 중 화면 안에 보이는 비율 (0~1)")]
    [Range(0.1f, 1f)] public float VisibleFraction = 0.5f;

    [Tooltip("Z 회전 (상대 손패는 180도로 뒤집힌 부채꼴)")]
    public float RotationZ = 0f;

    [Tooltip("원래 크기(배율 1)에서 손패 부채꼴 전체의 크기 (월드 단위)")]
    public Vector2 HandSize = new Vector2(7.3f, 2.2f);

    [Tooltip("원래 크기에서 transform 위치 기준 손패 중심의 y 오프셋")]
    public float CenterOffsetY = 0f;

    private Camera _cam;
    private Vector3 _baseLocalScale;

    public static HandDock Attach(GameObject go, Edge edge, float scale, float visible, float rotZ, Vector2 handSize, float centerOffsetY)
    {
        var d = go.GetComponent<HandDock>();
        if (d == null) d = go.AddComponent<HandDock>();
        d.Anchor = edge; d.Scale = scale; d.VisibleFraction = visible; d.RotationZ = rotZ;
        d.HandSize = handSize; d.CenterOffsetY = centerOffsetY;
        return d;
    }

    private void Start()
    {
        _cam = Camera.main;
        _baseLocalScale = transform.localScale;
        transform.localScale = _baseLocalScale * Scale;
        transform.localRotation = Quaternion.Euler(0, 0, RotationZ);
    }

    private void LateUpdate()
    {
        if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }
        float h = _cam.orthographicSize;
        var c = _cam.transform.position;

        float H = HandSize.y * Scale;
        float centerY = Anchor == Edge.Top
            ? (c.y + h) - VisibleFraction * H + H * 0.5f   // 위쪽 끝에서 아래로 VisibleFraction만큼 보임
            : (c.y - h) + VisibleFraction * H - H * 0.5f;  // 아래쪽 끝에서 위로 VisibleFraction만큼 보임

        float offset = CenterOffsetY * Scale; // transform 위치 → 손패 중심 (실측값)
        var p = transform.position;
        p.x = c.x;
        p.y = centerY - offset;
        transform.position = p;
    }
}
