using System;
using UnityEngine;
using DG.Tweening;

// CardHandArranger 컴포넌트가 반드시 함께 있도록 강제합니다.
[RequireComponent(typeof(CardHandArranger))]
public class EnemyHandAnimation : MonoBehaviour
{
    [Header("====== 애니메이션 제어 ======")]
    [Tooltip("애니메이션을 제어할 CardHandArranger 컴포넌트입니다.")]
    [SerializeField] private CardHandArranger handArranger;

    [Header("====== 애니메이션 상세 설정 ======")]
    [Tooltip("애니메이션이 재생되는 시간입니다.")]
    [SerializeField] private float faninanimationDuration = 2f;
[SerializeField] private float fandOutDuration = 0.1f;
    [Tooltip("애니메이션에 적용될 Ease 타입입니다.")]
    [SerializeField] private Ease easeType = Ease.OutBack;

    [Header("Pivot Y값 애니메이션")]
    [Tooltip("애니메이션 시작 시점의 Pivot.y 값입니다. (카드를 아래로 내리는 효과)")]
    [SerializeField] private float startPivotY = -5f;

    [Tooltip("애니메이션 종료 시점의 Pivot.y 값입니다. (최종 위치)")]
    [SerializeField] private float endPivotY = 0f;

    [Header("부채꼴 너비(Arc Width) 애니메이션")]
    [Tooltip("애니메이션 시작 시점의 부채꼴 너비입니다. (카드가 뭉쳐있는 상태)")]
    [SerializeField] private float startArcWidth = 0f;

    [Tooltip("애니메이션 종료 시점의 부채꼴 너비입니다. (활짝 펼쳐진 상태)")]
    [SerializeField] private float endArcWidth = 10f;

    private void Awake()
    {
        // 스크립트가 비활성화 상태여도 참조를 찾기 위해 Reset()을 사용합니다.
        // Awake()에서도 한 번 더 확인합니다.
        if (handArranger == null)
        {
            handArranger = GetComponent<CardHandArranger>();
        }
    }

    // 컴포넌트 추가 시 또는 인스펙터 리셋 시 자동으로 참조를 찾아줍니다.
    private void Reset()
    {
        handArranger = GetComponent<CardHandArranger>();
        // 기본 endArcWidth 값을 CardHandArranger의 현재 값으로 설정
        if (handArranger != null)
        {
            endArcWidth = handArranger.arcWidth;
            endPivotY = handArranger.CardPivot.y;
        }
    }

    /// <summary>
    /// 카드를 부채꼴로 펼치는 애니메이션을 재생합니다.
    /// </summary>
    [ContextMenu("Play Fan Out Animation")] // 인스펙터에서 테스트용 메뉴 추가
    public void PlayFanOutAnimation(Action onFinish = null)
    {
        if (handArranger == null)
        {
            Debug.LogError("CardHandArranger 컴포넌트가 할당되지 않았습니다!");
            return;
        }

        // DOTween.To를 사용하여 0에서 1로 진행되는 가상의 'progress' 값을 애니메이션합니다.
        // 이 progress 값의 변화에 따라 실제 프로퍼티들을 업데이트합니다.
        var tween= DOTween.To(
            getter: () => 1f, // 항상 0에서 시작
            setter: progress =>
            {
                // progress(0 -> 1)에 따라 시작값과 종료값 사이를 보간합니다.
                handArranger.arcWidth = Mathf.Lerp(startArcWidth, endArcWidth, progress);

                // CardPivot은 Vector3이므로 y값만 따로 변경해줍니다.
                Vector3 currentPivot = handArranger.CardPivot;
                currentPivot.y = Mathf.Lerp(startPivotY, endPivotY, progress);
                handArranger.CardPivot = currentPivot;

                // 변경된 프로퍼티 값으로 카드 정렬을 부드럽게 업데이트합니다.
                handArranger.UpdateArrange();
            },
            endValue: 0f, // 항상 1로 끝남
            duration: fandOutDuration
        ).SetEase(easeType);

        if (onFinish != null)
        {
            tween.OnComplete(() => onFinish());
        }
    }

    /// <summary>
    /// 펼쳐진 카드를 다시 뭉치는 애니메이션을 재생합니다.
    /// </summary>
    [ContextMenu("Play Fan In Animation")] // 인스펙터에서 테스트용 메뉴 추가
    public void PlayFanInAnimation(Action onComplete = null) // 파라미터는 Action으로 유지하는 것이 좋습니다.
    {
        if (handArranger == null)
        {
            Debug.LogError("CardHandArranger 컴포넌트가 할당되지 않았습니다!");
            onComplete?.Invoke(); // 콜백이 있다면 실행해주고 종료
            return;
        }

        // Fan Out과 반대로, 현재 상태(1)에서 목표 상태(0)로 progress 값을 애니메이션합니다.
        var tween = DOTween.To(
            getter: () => 1f, // 현재 상태를 1로 가정하고 시작
            setter: progress =>
            {
                // progress(1 -> 0)에 따라 값을 보간합니다.
                handArranger.arcWidth = Mathf.Lerp(startArcWidth, endArcWidth, progress);

                Vector3 currentPivot = handArranger.CardPivot;
                currentPivot.y = Mathf.Lerp(startPivotY, endPivotY, progress);
                handArranger.CardPivot = currentPivot;

                handArranger.UpdateArrange();
            },
            endValue: 1f, // 0으로 돌아감
            duration: faninanimationDuration
        ).SetEase(easeType);

        // onComplete 콜백이 null이 아닐 경우에만 등록합니다.
        if (onComplete != null)
        {
            // Action을 TweenCallback으로 한번 감싸서 전달합니다.
            // () => onComplete() 람다식이 새로운 TweenCallback을 생성하는 역할을 합니다.
            tween.OnComplete(() => onComplete());
        }
    }
}
