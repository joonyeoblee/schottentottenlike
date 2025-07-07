using UnityEngine;
using DG.Tweening; // DOTween을 사용하여 부드러운 이동을 계속 활용합니다.

public class UI_CardMove : MonoBehaviour
{
    public float RotateSensitivity = 1f; // 회전 민감도 (조절하여 원하는 반응 속도 설정)
    public float MaxRotationY = 90f;     // Y축 최대 회전 각도 (90도로 제한)
    public float RotationSmoothTime = 0.1f; // DOTween을 사용한 회전 부드러움 시간

    private float _targetRotationY = 0f;  // 목표 Y축 회전 값

    /// <summary>
    /// X축 델타 값에 따라 카드를 Y축으로 부드럽게 회전시킵니다.
    /// </summary>
    /// <param name="xDelta">X축으로의 이동 델타 값 (예: Input.GetAxis("Mouse X"))</param>
    public void RotateCardByXDelta(float xDelta)
    {
        // delta 값에 민감도를 곱하여 목표 회전 값을 계산합니다.
        // 현재 목표 회전 값에 더해 나갑니다.
        _targetRotationY += xDelta * RotateSensitivity;

        // 목표 회전 값을 -MaxRotationY 에서 MaxRotationY 사이로 제한합니다.
        _targetRotationY = Mathf.Clamp(_targetRotationY, -MaxRotationY, MaxRotationY);

        // DOTween을 사용하여 현재 회전 값을 목표 회전 값으로 부드럽게 보간합니다.
        // Quaternion.Euler는 오일러 각도(x, y, z)를 쿼터니언으로 변환합니다.
        // 이 경우 Y축만 회전시키므로 X와 Z는 0으로 유지합니다.
        transform.DORotate(new Vector3(0, _targetRotationY, 0), RotationSmoothTime);
    }

    // 초기화나 다른 용도로 필요하다면, 현재 회전 값을 0으로 리셋하는 메서드도 추가할 수 있습니다.
    public void ResetRotation()
    {
        _targetRotationY = 0f;
        transform.DORotate(Vector3.zero, RotationSmoothTime); // 0으로 부드럽게 회전
    }
}
