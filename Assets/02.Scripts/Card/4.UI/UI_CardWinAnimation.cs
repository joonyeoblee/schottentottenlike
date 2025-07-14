using UnityEngine;
using DG.Tweening;

public class UI_CardWinAnimation : MonoBehaviour
{
    [Header("===== 파괴 애니메이션 =====")]
    // ▼▼▼▼▼▼ 값 변경 ▼▼▼▼▼▼
    [SerializeField] private float _chaosMoveDuration = 0.6f; // 비행 시간을 0.8초에서 0.6초로 단축
    // ▲▲▲▲▲▲ 값 변경 ▲▲▲▲▲▲
    [SerializeField] private float _chaosMoveStrength = 15f;
    [SerializeField] private Ease _chaosEase = Ease.InCubic;
    [Tooltip("카드가 날아가기 시작한 후 폭발이 일어날 때까지의 시간")]
    [SerializeField] private float _explosionDelay = 0.1f; // 비행 시간이 줄었으므로 폭발 딜레이도 약간 줄임

    [Header("===== 이펙트 =====")]
    [SerializeField] private GameObject _explosionEffect;

    /// <summary>
    /// 카드가 혼란스럽게 날아가며 파괴되는 애니메이션 시퀀스를 반환합니다.
    /// </summary>
    public Sequence PlayDestructionAnimation()
    {
        Sequence sequence = DOTween.Sequence();

        // 1. 화면 바깥의 랜덤한 위치로 날아감
        Vector2 randomDirection = Random.insideUnitCircle.normalized * _chaosMoveStrength;
        Vector3 endPosition = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0);

        sequence.Join(transform.DOMove(endPosition, _chaosMoveDuration).SetEase(_chaosEase));
        sequence.Join(transform.DORotate(new Vector3(0, 0, Random.Range(-2000, 2000)), _chaosMoveDuration, RotateMode.FastBeyond360));

        // 2. 날아가는 동안 크기가 불규칙하게 변하는 시퀀스
        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.Append(transform.DOScale(1.5f, _chaosMoveDuration * 0.25f));
        scaleSequence.Append(transform.DOScale(0.5f, _chaosMoveDuration * 0.25f));
        scaleSequence.Append(transform.DOScale(1.2f, _chaosMoveDuration * 0.25f));
        scaleSequence.Append(transform.DOScale(1f, _chaosMoveDuration * 0.25f)); // 마지막에 원래 크기(1)로 복귀
        sequence.Join(scaleSequence);

        // 3. 비행 시작 후 잠시 뒤에 폭발 이펙트 실행
        sequence.InsertCallback(_explosionDelay, () =>
        {
            if (_explosionEffect != null)
            {
                Instantiate(_explosionEffect, transform.position, Quaternion.identity);
            }
        });

        return sequence;
    }
}
