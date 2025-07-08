using System;
using UnityEngine;
using DG.Tweening;

public class EnemyHandDrawAnimation : MonoBehaviour
{
    public static event Action OnCardDrawEnd;


    [Header("====== 대상 Transform 설정 ======")]
    [Tooltip("상대방의 카드가 정렬되어 있는 부모 Transform 입니다.")]
    [SerializeField] private Transform enemyHandPoint;

    [Tooltip("손패 정렬을 제어하는 CardHandArranger 컴포넌트입니다.")]
    [SerializeField] private CardHandArranger enemyHandArranger;

    private Transform midPoint;

    [Header("====== 애니메이션 상세 설정 ======")]
    [SerializeField] private float scaleMultiplier = 1.5f;
    [SerializeField] private float durationToMid = 0.5f;
    [SerializeField] private float delayAtMid = 0.3f;
    [SerializeField] private float durationToEnd = 0.6f;
    [SerializeField] private float enemyDeckDuration = 0.3f;
    [SerializeField] private Ease easeToMid = Ease.OutCubic;
    [SerializeField] private Ease easeToEnd = Ease.InCubic;

    [Header("카드 애니메이션 대상 (자동 할당)")]
    public HandCardSlot TargetSlot;
    public UI_Cards CardUI;
    public Transform CardToAnimate;

    private Vector3 _cachedOriginalScale = Vector3.one;

    /// <summary>
    /// 애니메이션 대상을 찾고 초기화합니다.
    /// IsInit 플래그를 제거하여, 애니메이션 호출 시 항상 최신 상태를 반영하도록 수정했습니다.
    /// </summary>
    private void AnimationObjectInit()
    {
        // 싱글턴 인스턴스에서 중간 지점 Transform을 가져옵니다.
        if (midPoint == null)
        {
            midPoint = AnimationTransforms.Instance.EnemyShowTransform;
        }

        if (enemyHandArranger == null)
        {
            Debug.Log("!!!!!!!!");
            enemyHandArranger = enemyHandPoint.GetComponentInParent<CardHandArranger>();
        }

        if (enemyHandPoint == null || enemyHandPoint.childCount == 0)
        {
            Debug.LogWarning("EnemyHandPoint에 자식 오브젝트(카드 슬롯)가 없어 초기화를 중단합니다.");
            return;
        }

        // 항상 마지막 자식을 애니메이션 대상으로 설정합니다.
        TargetSlot = enemyHandPoint.GetChild(enemyHandPoint.childCount - 1).GetComponent<HandCardSlot>();
        if (TargetSlot != null)
        {
            CardUI = TargetSlot.MyCard;
            if (CardUI != null)
            {
                CardToAnimate = CardUI.transform;
                _cachedOriginalScale = CardToAnimate.localScale;
            }
        }
    }

    /// <summary>
    /// 상대방 핸드에서 카드를 '내는' 애니메이션을 재생합니다.
    /// </summary>
    public void PlaySetAnimation(Transform endTransform, Texture2D cardTexture, Action callback = null)
    {
        // 애니메이션 시작 직전에 항상 대상을 새로 찾습니다.
        AnimationObjectInit();

        if (CardToAnimate == null )
        {
            Debug.Log("애니메이션 대상 카드가 없어 PlaySetAnimation을 실행할 수 없습니다.");
            TargetSlot = enemyHandPoint.GetChild(enemyHandPoint.childCount - 1).GetComponent<HandCardSlot>();
            if (TargetSlot != null)
            {
                CardUI = TargetSlot.MyCard;
                if (CardUI != null)
                {
                    CardToAnimate = CardUI.transform;
                    _cachedOriginalScale = CardToAnimate.localScale;
                }
            }
            callback?.Invoke();
            return;
        }

        if (enemyHandArranger == null)
        {
            Debug.Log("HandArranger가 없어 PlaySetAnimation을 실행할 수 없습니다.");
            enemyHandArranger = enemyHandPoint.GetComponentInParent<CardHandArranger>();

            return;
        }

        Quaternion targetRotation = endTransform.rotation * Quaternion.Euler(0f, 180f, 0f);

        CardUI.SwitchRenderer(true);
        if (cardTexture != null)
        {
            CardUI.backTexture = cardTexture;
            CardUI.ApplyTextures();
        }

        Sequence drawSequence = DOTween.Sequence();

        drawSequence.OnStart(() =>
        {
            // 애니메이션을 위해 월드 좌표계로 카드를 이동시킵니다.
            CardToAnimate.SetParent(null, true);
            // 핸드 재정렬을 위해 슬롯을 비웁니다.
            TargetSlot.IsEmpty = true;
            enemyHandArranger.UpdateArrange();
        });

        // 애니메이션 시퀀스: 중간 지점으로 이동 -> 최종 목적지로 이동
        drawSequence.Append(CardToAnimate.DOMove(midPoint.position, durationToMid).SetEase(easeToMid));
        drawSequence.Join(CardToAnimate.DOScale(_cachedOriginalScale * scaleMultiplier, durationToMid).SetEase(easeToMid));
        drawSequence.Join(CardToAnimate.DORotate(midPoint.rotation.eulerAngles, durationToMid).SetEase(easeToMid));

        if (delayAtMid > 0) drawSequence.AppendInterval(delayAtMid);

        drawSequence.Append(CardToAnimate.DOMove(endTransform.position, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(CardToAnimate.DOScale(_cachedOriginalScale, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(CardToAnimate.DORotateQuaternion(targetRotation, durationToEnd).SetEase(easeToEnd));

        // [수정됨] OnComplete 콜백 로직: 카드를 핸드로 되돌리는 대신, 제출 처리합니다.
        drawSequence.OnComplete(() =>
        {
            Debug.Log("카드 내기(Set) 애니메이션 완료. 카드를 비활성화하고 슬롯을 정리합니다.");



            // 핸드 슬롯의 카드 데이터를 완전히 제거합니다.
            if(TargetSlot != null)
            {
                TargetSlot.Clear(); // MyCard = null; IsEmpty = true; 와 같은 내부 로직이 필요합니다.
            }

            callback?.Invoke();
        });

        drawSequence.Play();
    }

    /// <summary>
    /// 지정된 시작점에서 상대방 핸드로 카드를 '뽑는' 애니메이션을 재생합니다.
    /// </summary>
    public void EnemySetAnimation(Action callback = null, Ease easeType = Ease.Linear)
    {
        // 애니메이션 시작 직전에 항상 대상을 새로 찾습니다.
        AnimationObjectInit();

        if (!ValidateDependencies() || enemyHandArranger == null)
        {
            Debug.LogError("필수 요소 문제로 EnemySetAnimation을 중단합니다.");
            callback?.Invoke();
            return;
        }

        Transform startPoint = AnimationTransforms.Instance.EnemyResetTransfrom;
        if (startPoint == null)
        {
            Debug.LogError("EnemyResetTransfrom이 할당되지 않았습니다.");
            callback?.Invoke();
            return;
        }

        // 새 카드가 들어올 공간을 확보하기 위해 핸드를 먼저 재정렬합니다.
        TargetSlot.IsEmpty = false;
        enemyHandArranger.UpdateArrange();

        // 카드의 최종 목적지 Transform 값을 미리 저장합니다.
        Vector3 finalHandPos = TargetSlot.transform.position;
        Quaternion finalHandRot = TargetSlot.transform.rotation;

        // 애니메이션을 위해 월드 좌표계로 이동시키고 시작점에 배치합니다.
        CardToAnimate.SetParent(null, true);
        CardToAnimate.position = startPoint.position;
        CardToAnimate.rotation = startPoint.rotation;
        CardToAnimate.localScale = _cachedOriginalScale;

        CardUI.SwitchRenderer(true);

        Sequence drawSequence = DOTween.Sequence();

        // 저장해둔 최종 목적지로 이동합니다.
        drawSequence.Append(CardToAnimate.DOMove(finalHandPos, enemyDeckDuration).SetEase(easeType));
        drawSequence.Join(CardToAnimate.DORotateQuaternion(finalHandRot, enemyDeckDuration).SetEase(easeType));

        drawSequence.OnComplete(() =>
        {
            // 애니메이션 완료 후, 카드를 핸드 슬롯의 자식으로 설정하고 위치를 초기화합니다.
            CardToAnimate.SetParent(TargetSlot.transform, true);
            CardToAnimate.localPosition = Vector3.zero;
            CardToAnimate.localRotation = Quaternion.identity;
            CardToAnimate.localScale = _cachedOriginalScale;

            callback?.Invoke();
            Debug.Log("적 드로우 애니메이션 완료");
        });

        drawSequence.Play();
    }

    private bool ValidateDependencies()
    {
        string errorPrefix = "[EnemyAnimation Validation Error] ";
        if (AnimationTransforms.Instance == null) { Debug.LogError(errorPrefix + "AnimationTransforms.Instance가 null입니다."); return false; }
        if (midPoint == null) { Debug.LogError(errorPrefix + "midPoint가 null입니다."); return false; }
        if (enemyHandArranger == null) { Debug.LogError(errorPrefix + "enemyHandArranger가 인스펙터에 할당되지 않았습니다."); return false; }
        if (!midPoint.gameObject.activeInHierarchy) { Debug.LogError(errorPrefix + "중간 지점(midPoint) 오브젝트가 비활성화되어 있습니다."); return false; }
        if (enemyHandPoint == null) { Debug.LogError(errorPrefix + "enemyHandPoint가 인스펙터에 할당되지 않았습니다."); return false; }
        if (!enemyHandPoint.gameObject.activeInHierarchy) { Debug.LogError(errorPrefix + "적 핸드 포인트(enemyHandPoint) 오브젝트가 비활성화되어 있습니다."); return false; }
        if (enemyHandPoint.childCount == 0) { Debug.LogWarning(errorPrefix + "enemyHandPoint에 자식 오브젝트(카드 슬롯)가 없습니다."); return false; }
        if (TargetSlot == null || CardUI == null || CardToAnimate == null) { Debug.LogError(errorPrefix + "애니메이션 대상(TargetSlot, CardUI, CardToAnimate)이 null입니다."); return false; }
        if (!TargetSlot.gameObject.activeInHierarchy) { Debug.LogError(errorPrefix + $"목표 슬롯 '{TargetSlot.name}' 오브젝트가 비활성화되어 있습니다."); return false; }
        return true;
    }

    /// <summary>
    /// 이 오브젝트와 관련된 모든 DOTween 애니메이션을 중지합니다.
    /// </summary>
    public void Clear()
    {
        // 이 스크립트를 대상으로 실행된 모든 트윈을 안전하게 제거합니다.
        DOTween.Kill(this);
    }
}
