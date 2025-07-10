using UnityEngine;
using DG.Tweening;

public class UI_StoneWinAnimation : MonoBehaviour
{
    [Header("===== 상승 애니메이션 =====")]
    [SerializeField] private float _riseDuration = 0.5f;
    [SerializeField] private float _riseScale = 1.5f;
    [SerializeField] private Ease _riseEase = Ease.OutBack;

    [Header("===== 비행 애니메이션 =====")]
    [SerializeField] private float _flyDuration = 1.0f;
    [SerializeField] private Ease _flyEase = Ease.InOutQuad;

    [Header("===== 이펙트 =====")]
    [SerializeField] private GameObject _celebrationEffect;

    /// <summary>
    /// 돌이 솟아오르고 타겟으로 날아가는 전체 애니메이션 시퀀스를 반환합니다.
    /// </summary>
    public Sequence PlayRiseAndFlyAnimation(Vector3 targetPosition)
    {
        Sequence sequence = DOTween.Sequence();

        // 1. 커지면서 솟아오르는 연출
        sequence.Append(transform.DOScale(_riseScale, _riseDuration).SetEase(_riseEase));

        // 2. 축하 이펙트 재생 (Callback 사용)
        sequence.AppendCallback(() =>
        {
            if (_celebrationEffect != null) Instantiate(_celebrationEffect);
        });
        sequence.AppendInterval(0.2f); // 이펙트가 보일 시간

        // 3. 베지어 곡선을 그리며 타겟으로 이동
        Vector3[] path = new Vector3[2];
        path[0] = (transform.position + targetPosition) / 2 + Vector3.up * 5f; // 중간 제어점 (포물선 높이)
        path[1] = targetPosition;
        sequence.Append(transform.DOPath(path, _flyDuration, PathType.CatmullRom).SetEase(_flyEase));

        // 만들어진 시퀀스를 Controller가 사용할 수 있도록 반환
        return sequence;
    }
}
