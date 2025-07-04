using System;
using UnityEngine;
using DG.Tweening;

public class EnemyHandDrawAnimation : MonoBehaviour
{
    [Header("====== 대상 Transform 설정 ======")]
    [Tooltip("상대방의 카드가 정렬되어 있는 부모 Transform 입니다.")]
    [SerializeField] private Transform enemyHandPoint;

    [Tooltip("카드를 크게 보여줄 중간 지점 Transform 입니다.")]
    [SerializeField] private Transform midPoint;

    [Tooltip("애니메이션이 끝난 후 카드가 최종적으로 위치할 Transform 입니다.")]
    [SerializeField] private Transform endTransform;

    [Header("====== 애니메이션 상세 설정 ======")]
    [Tooltip("중간 지점에서 카드가 커지는 배율입니다.")]
    [SerializeField] private float scaleMultiplier = 1.5f;

    [Tooltip("중간 지점까지 이동하는 데 걸리는 시간입니다.")]
    [SerializeField] private float durationToMid = 0.5f;

    [Tooltip("최종 목적지까지 이동하는 데 걸리는 시간입니다.")]
    [SerializeField] private float durationToEnd = 0.6f;

    [Tooltip("중간 지점으로 갈 때의 Ease 타입입니다.")]
    [SerializeField] private Ease easeToMid = Ease.OutCubic;

    [Tooltip("최종 목적지로 갈 때의 Ease 타입입니다.")]
    [SerializeField] private Ease easeToEnd = Ease.InCubic;



    /// <summary>
    /// 상대방 핸드에서 카드를 뽑는 애니메이션을 재생합니다.
    /// </summary>
    public void PlayDrawAnimation()
    {
        // 1. 필수 컴포넌트 및 조건 확인
        if (enemyHandPoint == null || midPoint == null || endTransform == null)
        {
            Debug.LogError("필수 Transform이 인스펙터에 할당되지 않았습니다. 애니메이션을 실행할 수 없습니다.");
            return;
        }

        if (enemyHandPoint.childCount == 0)
        {
            Debug.LogWarning("상대방의 핸드에 카드가 없습니다.");
            return;
        }

        // 2. 애니메이션 대상 카드 선택 (가장 마지막 자식)
        Transform cardToAnimate = enemyHandPoint.GetChild(enemyHandPoint.childCount - 1);
        Vector3 originalScale = cardToAnimate.localScale;

        // 3. DOTween 시퀀스 생성
        Sequence drawSequence = DOTween.Sequence();

        // 4. 애니메이션 단계 설정

        // OnStart: 애니메이션 시작 직전에 카드를 부모로부터 분리
        drawSequence.OnStart(() =>
        {
            // true 파라미터는 월드 포지션을 유지하면서 부모를 해제하는 옵션입니다.
            cardToAnimate.SetParent(null, true);
        });

        // Part 1: 중간 지점으로 이동하며 확대 및 정면 보기
        drawSequence.Append(cardToAnimate.DOMove(midPoint.position, durationToMid).SetEase(easeToMid));
        drawSequence.Join(cardToAnimate.DOScale(originalScale * scaleMultiplier, durationToMid).SetEase(easeToMid));
        drawSequence.Join(cardToAnimate.DORotate(midPoint.rotation.eulerAngles, durationToMid).SetEase(easeToMid));

        // Part 2: 최종 목적지로 이동하며 원래 크기로 축소 및 최종 회전값 적용
        drawSequence.Append(cardToAnimate.DOMove(endTransform.position, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(cardToAnimate.DOScale(originalScale, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(cardToAnimate.DORotateQuaternion(endTransform.rotation, durationToEnd).SetEase(easeToEnd));

        // OnComplete: 애니메이션 완료 후 처리
        drawSequence.OnComplete(() =>
        {
            Debug.Log("상대 핸드 드로우 애니메이션 완료.");
            // 선택: 최종적으로 endTransform의 자식으로 만들 수 있습니다.
            cardToAnimate.SetParent(endTransform, true);
        });

        // 5. 시퀀스 재생
        drawSequence.Play();
    }
}
