using UnityEngine;
using DG.Tweening;

public class UI_CardWinAnimation : MonoBehaviour
{
    [Header("===== 파괴 애니메이션 =====")]
    [SerializeField] private float _chaosMoveDuration = 1.5f;
    [SerializeField] private float _chaosMoveStrength = 10f;
    [SerializeField] private Ease _chaosEase = Ease.InQuad;

    [Header("===== 이펙트 =====")]
    [SerializeField] private GameObject _smokeEffect;
    [SerializeField] private GameObject _explosionEffect;

    /// <summary>
    /// 카드가 혼란스럽게 날아가며 파괴되는 애니메이션 시퀀스를 반환합니다.
    /// </summary>
    public Sequence PlayDestructionAnimation()
    {
        Sequence sequence = DOTween.Sequence();

        // 1. 연기 이펙트 재생
        sequence.AppendCallback(() =>
        {
            if (_smokeEffect != null) Instantiate(_smokeEffect);
        });
        sequence.AppendInterval(0.3f); // 연기가 피어오를 시간

        // 2. 폭발 이펙트와 함께 카드가 혼란스럽게 날아감
        sequence.AppendCallback(() =>
        {
            if (_explosionEffect != null)  Instantiate(_explosionEffect);
        });

        // 펀치 효과로 터지는 느낌 연출
        sequence.Join(transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 5, 0.5f));

        // 화면 바깥의 랜덤한 위치로 날아감
        Vector2 randomDirection = Random.insideUnitCircle.normalized * _chaosMoveStrength;
        Vector3 endPosition = transform.position + new Vector3(randomDirection.x, randomDirection.y, 0);

        sequence.Append(transform.DOMove(endPosition, _chaosMoveDuration).SetEase(_chaosEase));
        sequence.Join(transform.DORotate(new Vector3(0, 0, Random.Range(-720, 720)), _chaosMoveDuration, RotateMode.FastBeyond360));

        // 날아가면서 사라지도록 페이드 아웃
        sequence.Join(GetComponent<CanvasGroup>()?.DOFade(0, _chaosMoveDuration * 0.5f).SetDelay(_chaosMoveDuration * 0.5f));


        return sequence;
    }
}
