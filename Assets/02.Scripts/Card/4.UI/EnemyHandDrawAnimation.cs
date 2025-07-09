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

    private void Start()
    {
        midPoint = AnimationTransforms.Instance.EnemyShowTransform;
        if (enemyHandArranger == null)
        {
            enemyHandArranger = enemyHandPoint.GetComponent<CardHandArranger>();
        }
        AnimationObjectInit();
    }

    private void AnimationObjectInit()
    {
        if (enemyHandPoint == null || enemyHandPoint.childCount == 0) return;

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

        midPoint = AnimationTransforms.Instance.EnemyShowTransform;

        if (enemyHandArranger == null)
        {
            enemyHandArranger = enemyHandPoint.GetComponent<CardHandArranger>();
        }
    }

    /// <summary>
    /// 상대방 핸드에서 카드를 '내는' 애니메이션을 재생합니다.
    /// </summary>
    public void PlaySetAnimation(Transform endTransform, Texture2D cardTexture, Action callback = null)
    {
        AnimationObjectInit();
        if (CardToAnimate == null || enemyHandArranger == null)
        {
            Debug.LogError("애니메이션 대상 카드 또는 HandArranger가 없어 PlaySetAnimation을 실행할 수 없습니다.");
            return;
        }

        Quaternion targetRotation = endTransform.rotation * Quaternion.Euler(0f, 180f, 0f);

        CardUI.SwitchRenderer(true);
        if (cardTexture != null) { CardUI.backTexture = cardTexture; CardUI.ApplyTextures(); }

        Sequence drawSequence = DOTween.Sequence();

        drawSequence.OnStart(() =>
        {
            CardToAnimate.SetParent(null, true);
            // 슬롯을 '비어있음'으로 처리하고, 즉시 핸드 재정렬 애니메이션을 호출합니다.
            TargetSlot.IsEmpty = true;
            enemyHandArranger.UpdateArrange();
        });

        drawSequence.Append(CardToAnimate.DOMove(midPoint.position, durationToMid).SetEase(easeToMid));
        drawSequence.Join(CardToAnimate.DOScale(_cachedOriginalScale * scaleMultiplier, durationToMid).SetEase(easeToMid));
        drawSequence.Join(CardToAnimate.DORotate(midPoint.rotation.eulerAngles, durationToMid).SetEase(easeToMid));
        if (delayAtMid > 0) drawSequence.AppendInterval(delayAtMid);
        drawSequence.Append(CardToAnimate.DOMove(endTransform.position, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(CardToAnimate.DOScale(_cachedOriginalScale, durationToEnd).SetEase(easeToEnd));
        drawSequence.Join(CardToAnimate.DORotateQuaternion(targetRotation, durationToEnd).SetEase(easeToEnd));

        drawSequence.OnComplete(() =>
        {
            Debug.Log("카드 내기(Set) 애니메이션 완료.");
            CardUI.SwitchRenderer(false);

            CardToAnimate.SetParent(TargetSlot.transform);

            TargetSlot.transform.SetParent(enemyHandPoint, true);
            CardToAnimate.localPosition = Vector3.zero;
            CardToAnimate.localRotation = Quaternion.identity;
            CardToAnimate.localScale = _cachedOriginalScale;
            callback?.Invoke();
        });

        drawSequence.Play();
    }

    /// <summary>
    /// 지정된 시작점에서 상대방 핸드로 카드를 '뽑는' 애니메이션을 재생합니다.
    /// </summary>
    public void EnemySetAnimation(Action callback = null, Ease easeType = Ease.Linear)
    {
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

        // 애니메이션 시작 전, 핸드를 재정렬하여 새 카드가 들어올 공간을 만듭니다.
        TargetSlot.IsEmpty = false; // 카드가 채워질 것이므로 IsEmpty를 false로 설정
        enemyHandArranger.UpdateArrange();

        float totalDuration = durationToMid + durationToEnd;

        CardToAnimate.localScale = _cachedOriginalScale;
        CardToAnimate.SetParent(null, true);
        CardToAnimate.position = startPoint.position;
        CardToAnimate.rotation = startPoint.rotation;

        CardUI.SwitchRenderer(true);

        Sequence drawSequence = DOTween.Sequence();

        drawSequence.Append(CardToAnimate.DOMove(TargetSlot.transform.position, enemyDeckDuration)
            .SetEase(easeType));
        drawSequence.Join(CardToAnimate.DORotateQuaternion(TargetSlot.transform.rotation, enemyDeckDuration)
            .SetEase(easeType));

        drawSequence.OnComplete(() =>
        {
            Debug.Log("상대 핸드 드로우 애니메이션 OnComplete 실행.");
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

    public void Clear()
    {
        DOTween.Kill(this);
    }
}
