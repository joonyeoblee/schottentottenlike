using UnityEngine;
using DG.Tweening;

public class UI_StoneWinAnimation : MonoBehaviour
{
    [Header("===== 상승 애니메이션 =====")]
    [SerializeField] private float _riseDuration = 0.3f; // 상승 시간 단축
    [SerializeField] private float _riseScale = 1.2f;
    [SerializeField] private Ease _riseEase = Ease.OutQuad;

    [Header("===== 비행 애니메이션 =====")]
    [SerializeField] private float _flyDuration = 0.8f; // 비행 시간 단축
    // ▼▼▼▼▼▼ Ease 변경 ▼▼▼▼▼▼
    [SerializeField] private Ease _flyEase = Ease.InBack; // 뒤로 빠졌다 나가는 효과
    // ▲▲▲▲▲▲ Ease 변경 ▲▲▲▲▲▲
    [Tooltip("곡선이 옆으로 휘는 정도")]
    [SerializeField] private float _horizontalArcStrength = 4f;
    [Tooltip("곡선이 위로 솟는 높이")]
    [SerializeField] private float _verticalArcStrength = 6f;
    // ▼▼▼▼▼▼ 추가된 변수 ▼▼▼▼▼▼
    [Tooltip("비행 중 최대 크기")]
    [SerializeField] private float _peakScale = 2.0f; // 입체감을 위한 최대 크기
    // ▲▲▲▲▲▲ 추가된 변수 ▲▲▲▲▲▲


    [Header("===== 이펙트 =====")]
    [SerializeField] private GameObject _celebrationEffect;


    public Sequence PlayRiseAndFlyAnimation(Vector3 targetPosition, bool isPlayerSide)
    {
        Sequence sequence = DOTween.Sequence();

        // 1. 제자리에서 살짝 커지는 준비 동작
        sequence.Append(transform.DOScale(_riseScale, _riseDuration).SetEase(_riseEase));
        sequence.AppendCallback(() =>
        {
            if (_celebrationEffect != null) Instantiate(_celebrationEffect, transform.position, Quaternion.identity);
        });
        sequence.AppendInterval(0.1f);

        // 2. 베지어 곡선을 그리며 타겟으로 이동 (뒤로 빠지는 효과 적용)
        Vector3 controlPoint = (transform.position + targetPosition) / 2;
        Vector3 verticalOffset = Vector3.up * _verticalArcStrength;
        float horizontalDirection = isPlayerSide ? 1f : -1f;
        Vector3 horizontalOffset = Vector3.right * _horizontalArcStrength * horizontalDirection;
        controlPoint += verticalOffset + horizontalOffset;

        Vector3[] path = new Vector3[2];
        path[0] = controlPoint;
        path[1] = targetPosition;

        // ▼▼▼▼▼▼ 애니메이션 로직 변경 ▼▼▼▼▼▼
        // 경로를 따라 이동하는 트윈. SetEase(Ease.InBack)으로 예비 동작 추가
        Tween flyTween = transform.DOPath(path, _flyDuration, PathType.CatmullRom)
                                  .SetEase(_flyEase);

        // 크기 변화 트윈: 중간까지 커졌다가 다시 원래대로 돌아옴
        Sequence scaleSequence = DOTween.Sequence();
        // 비행 시간의 절반 동안 _peakScale까지 커짐
        scaleSequence.Append(transform.DOScale(_peakScale, _flyDuration / 2).SetEase(Ease.OutSine));
        // 나머지 절반 동안 원래 크기로 작아짐
        scaleSequence.Append(transform.DOScale(0f, _flyDuration / 2).SetEase(Ease.InSine));

        // 이동과 크기 변화를 동시에 실행
        sequence.Append(flyTween);
        sequence.Join(scaleSequence);
        // ▲▲▲▲▲▲ 애니메이션 로직 변경 ▲▲▲▲▲▲

        return sequence;
    }
}
