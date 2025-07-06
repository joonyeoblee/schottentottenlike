using System;
using System.Collections;
using System.Net;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class EnemyHandDrawAnimation : MonoBehaviour
{
    [Header("====== 대상 Transform 설정 ======")]
    [Tooltip("상대방의 카드가 정렬되어 있는 부모 Transform 입니다.")]
    [SerializeField] private Transform enemyHandPoint;

    [Tooltip("카드를 크게 보여줄 중간 지점 Transform 입니다.")]
    private Transform midPoint;

    [Tooltip("애니메이션이 끝난 후 카드가 최종적으로 위치할 Transform 입니다.")]
    public Transform EndTransform;

    [Header("====== 애니메이션 상세 설정 ======")]
    [Tooltip("중간 지점에서 카드가 커지는 배율입니다.")]
    [SerializeField] private float scaleMultiplier = 1.5f;

    [Tooltip("중간 지점까지 이동하는 데 걸리는 시간입니다.")]
    [SerializeField] private float durationToMid = 0.5f;

    // ==================================================
    // [수정] 중간 지점에서 머무는 시간을 조절하는 변수 추가
    // ==================================================
    [Tooltip("중간 지점에서 머무는 시간입니다.")]
    [SerializeField] private float delayAtMid = 0.3f;

    [Tooltip("최종 목적지까지 이동하는 데 걸리는 시간입니다.")]
    [SerializeField] private float durationToEnd = 0.6f;

    [Tooltip("중간 지점으로 갈 때의 Ease 타입입니다.")]
    [SerializeField] private Ease easeToMid = Ease.OutCubic;

    [Tooltip("최종 목적지로 갈 때의 Ease 타입입니다.")]
    [SerializeField] private Ease easeToEnd = Ease.InCubic;

    private void Start()
    {
        midPoint = AnimationTransforms.Instance.EnemyShowTransform;
        EndTransform = AnimationTransforms.Instance.DeckTransfrom;
    }

    /// <summary>
    /// 상대방 핸드에서 카드를 뽑는 애니메이션을 재생합니다.
    /// </summary>
    public void PlaySetAnimation(Transform endTransform, Texture2D cardTextrue,Action callback = null)
    {
        EndTransform = endTransform;
        EndTransform.rotation = endTransform.rotation * Quaternion.Euler(0f, 180f, 0f);


        // 1. 필수 컴포넌트 및 조건 확인
        if (enemyHandPoint == null)
        {
            Debug.LogError("적의 손패에 대한 내용이 없습니다.");
            return;
        }

        if( midPoint == null)
        {
            midPoint = AnimationTransforms.Instance.EnemyShowTransform;

        }

        if (enemyHandPoint.childCount == 0)
        {
            Debug.LogWarning("핸드에 카드가 없습니다.");
            return;
        }

        // 2. 애니메이션 대상 카드 선택 (가장 마지막 자식)
        HandCardSlot TargetSlot = enemyHandPoint.GetChild(enemyHandPoint.childCount - 1).GetComponent<HandCardSlot>();
        UI_Cards CardUI = TargetSlot.MyCard;
        CardUI.SwitchRenderer(true);
        Transform cardToAnimate = CardUI.transform;

        CardUI.backTexture = cardTextrue;
        CardUI.ApplyTextures();


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


        if (delayAtMid > 0)
        {
            drawSequence.AppendInterval(delayAtMid);
        }

        // Part 2: 최종 목적지로 이동하며 원래 크기로 축소 및 최종 회전값 적용
        drawSequence.Append(cardToAnimate.DOMove(EndTransform.position, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(cardToAnimate.DOScale(originalScale, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(cardToAnimate.DORotateQuaternion(EndTransform.rotation, durationToEnd).SetEase(easeToEnd));

        // OnComplete: 애니메이션 완료 후 처리
        drawSequence.OnComplete(() =>
        {
            Debug.Log("상대 핸드 드로우 애니메이션 완료.");
            CardUI.SwitchRenderer(false);
            cardToAnimate.SetParent(TargetSlot.transform);
            TargetSlot.transform.SetParent(AnimationTransforms.Instance.EnemyHandTransform, true);
            cardToAnimate.localPosition = Vector3.zero;
            cardToAnimate.localRotation = Quaternion.identity;
            cardToAnimate.localScale = Vector3.one;

            if (callback != null)
            {
                callback();
            }

            //StartCoroutine(EnemySetAnimation());
        });

        // 5. 시퀀스 재생
        drawSequence.Play();

    }


     /// <summary>
    /// 지정된 시작점에서 상대방 핸드로 카드를 '뽑는' 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="startPoint">애니메이션이 시작될 위치의 Transform</param>
    /// <param name="callback">애니메이션 완료 후 실행될 콜백 함수</param>
    public IEnumerator EnemySetAnimation(Action callback = null)
    {
        Debug.Log("적 드로우 애니메이션 실행");
        yield return new WaitForSeconds(0.5f);
        Transform startPoint = AnimationTransforms.Instance.EnemyResetTransfrom;

        if (enemyHandPoint == null || midPoint == null || startPoint == null || enemyHandPoint.childCount == 0)
        {
            Debug.LogWarning("SetDrawAnimation을 실행할 수 없습니다. 필수 요소가 부족합니다.");
            callback?.Invoke();
            yield break;
        }

        // 1. 애니메이션의 최종 목적지인 '핸드의 마지막 슬롯'을 찾습니다.
        HandCardSlot destinationSlot = enemyHandPoint.GetChild(enemyHandPoint.childCount - 1).GetComponent<HandCardSlot>();
        UI_Cards cardUI = destinationSlot.MyCard;
        Transform cardToAnimate = cardUI.transform;

        // 2. 애니메이션을 위해 카드를 준비합니다.
        //    - 최종적으로 돌아와야 할 크기를 미리 저장합니다.
        Vector3 finalScale = cardToAnimate.localScale;
        //    - 카드를 부모로부터 분리하고, 지정된 시작 위치로 즉시 이동시킵니다.
        cardToAnimate.SetParent(null, true);
        cardToAnimate.position = startPoint.position;
        cardToAnimate.rotation = startPoint.rotation;

        // 카드가 보이도록 렌더러를 켭니다 (필요에 따라).
        cardUI.SwitchRenderer(true);

        // 3. DOTween 시퀀스를 생성합니다.
        Sequence drawSequence = DOTween.Sequence();

        // 4. 애니메이션 단계를 설정합니다.
        // Part 1: 시작 지점 -> 중간 지점으로 이동하며 확대 및 정면 보기
        drawSequence.Append(cardToAnimate.DOMove(midPoint.position, durationToMid).SetEase(easeToMid));
        drawSequence.Join(cardToAnimate.DOScale(finalScale * scaleMultiplier, durationToMid).SetEase(easeToMid));
        drawSequence.Join(cardToAnimate.DORotate(midPoint.rotation.eulerAngles, durationToMid).SetEase(easeToMid));

        if (delayAtMid > 0)
        {
            drawSequence.AppendInterval(delayAtMid);
        }

        // Part 2: 중간 지점 -> 최종 목적지(핸드 슬롯)로 이동하며 원래 크기로 축소 및 최종 회전값 적용
        drawSequence.Append(cardToAnimate.DOMove(destinationSlot.transform.position, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(cardToAnimate.DOScale(finalScale, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(cardToAnimate.DORotateQuaternion(destinationSlot.transform.rotation, durationToEnd).SetEase(easeToEnd));

        // OnComplete: 애니메이션 완료 후 처리
        drawSequence.OnComplete(() =>
        {
            Debug.Log("상대 핸드 드로우 애니메이션 완료.");
            // 카드를 최종 목적지인 슬롯의 자식으로 다시 설정합니다.
            cardToAnimate.SetParent(destinationSlot.transform, true);
            cardToAnimate.localPosition = Vector3.zero;
            cardToAnimate.localRotation = Quaternion.identity;

            // 외부에서 전달된 콜백이 있다면 실행합니다.
            callback?.Invoke();
            Debug.Log("적 드로우 애니메이션 완료");

        });

        // 5. 시퀀스를 재생합니다.
        drawSequence.Play();
    }




    public void Clear()
    {

    }
}
