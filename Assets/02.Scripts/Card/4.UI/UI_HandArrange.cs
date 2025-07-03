using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // DOTween 네임스페이스 추가

[ExecuteInEditMode]
public class CardHandArranger : MonoBehaviour
{
    [Header("Arrangement Settings")]
    [Tooltip("카드가 펼쳐질 전체 너비입니다.")]
    [Range(0f, 20f)]
    public float arcWidth = 10f;

    [Tooltip("카드가 아래로 휘어지는 정도입니다. 0이면 직선이 됩니다.")]
    [Range(0f, 10f)]
    public float arcHeight = 2f;

    [Tooltip("카드 사이의 간격을 조절합니다.")]
    [Range(0f, 2f)]
    public float spacing = 1.2f;

    [Tooltip("카드를 살짝 겹치게 보이도록 z축 간격을 조절합니다.")]
    [Range(0.01f, 0.5f)]
    public float cardDepth = 0.1f;

    [Tooltip("카드의 중심을 조정할 피봇값입니다.")] public Vector3 CardPivot;

    // --- DOTween 관련 설정 추가 ---
    [Header("Tweening Settings")]
    [Tooltip("카드 정렬 시 이동 및 회전 애니메이션에 걸리는 시간입니다.")]
    public float arrangeDuration = 0.3f; // 보간 시간 (원하는 시간으로 조절)
    [Tooltip("카드 정렬 애니메이션의 이징(Easing) 함수입니다.")]
    public Ease arrangeEase = Ease.OutQuad; // 보간 이징 (원하는 이징 함수로 조절)
    // ----------------------------

    private List<Transform> lastArrangedCards = new List<Transform>();

    private void OnValidate() // 유니티 에디터상에서 아크를 조절할 수 있도록 하는 부분
    {
        if (Application.isPlaying) return;
        // 에디터에서는 보간 없이 즉시 정렬되도록 합니다.
        // 그렇지 않으면 에디터에서 값 조절 시 애니메이션이 재생됩니다.
        ArrangeCards(null, true);
    }

    private void Update()
    {
        // 플레이 모드에서만 자식 변경 감지 후 정렬
        if (Application.isPlaying)
        {
            var currentChildren = GetChildList();
            if (!currentChildren.SequenceEqual(lastArrangedCards))
            {
                ArrangeCards();
            }
        }
    }

    private List<Transform> GetChildList()
    {
        var list = new List<Transform>();
        foreach (Transform child in transform)
        {
            list.Add(child);
        }
        return list;
    }

    /// <summary>
    /// 카드들을 부채꼴 형태로 정렬합니다. 특정 카드는 정렬에서 제외할 수 있습니다.
    /// 이제 이 함수는 DOTween을 사용하여 카드들의 움직임에 보간을 적용합니다.
    /// </summary>
    /// <param name="cardToIgnore">정렬에서 제외할 카드 Transform (예: 현재 드래그 중인 카드)</param>
    /// <param name="instantArrange">true이면 보간 없이 즉시 정렬합니다 (주로 에디터 OnValidate에서 사용).</param>
    public void ArrangeCards(Transform cardToIgnore = null, bool instantArrange = false)
    {
        List<Transform> sourceCards = GetChildList();
        lastArrangedCards = new List<Transform>(sourceCards);

        if (sourceCards.Count == 0) return;

        // 드래그 중인 카드를 제외하고 정렬할 카드들만 필터링합니다.
        // 이렇게 하면 드래그 중인 카드가 빠진 공간을 나머지 카드들이 채우는 것처럼 보입니다.
        List<Transform> cardsToArrange = sourceCards.Where(card => card != cardToIgnore).ToList();

        // 정렬할 카드가 없다면 리턴 (예: 드래그 중인 카드 하나만 남은 경우)
        if (cardsToArrange.Count == 0 && cardToIgnore != null) return;
        if (sourceCards.Count == 1 && cardToIgnore != null && sourceCards[0] == cardToIgnore) return;

        int arrangingCardCount = cardsToArrange.Count;
        float arrangingTotalWidth = (arrangingCardCount > 1) ? (arrangingCardCount - 1) * spacing : 0f;
        float arrangingEffectiveWidth = Mathf.Min(arrangingTotalWidth, arcWidth);

        for (int i = 0; i < arrangingCardCount; i++)
        {
            Transform cardSlot = cardsToArrange[i];

            float t = (arrangingCardCount > 1) ? (float)i / (arrangingCardCount - 1) : 0.5f;
            float xPos = Mathf.Lerp(-arrangingEffectiveWidth / 2f, arrangingEffectiveWidth / 2f, t);

            float yPos = 0;
            if (arcHeight > 0 && arrangingEffectiveWidth > 0)
            {
                yPos = -(xPos * xPos) * (arcHeight / (arrangingEffectiveWidth * arrangingEffectiveWidth / 4f));
            }
            float zPos = i * cardDepth; // z-depth는 순서에 따라 계속 부여

            Vector3 basePosition = new Vector3(xPos, yPos, zPos);

            float tangentAngle = 0f;
            if (arrangingEffectiveWidth > 0)
            {
                float derivative = -(8f * arcHeight / (arrangingEffectiveWidth * arrangingEffectiveWidth)) * xPos;
                tangentAngle = Mathf.Atan(derivative) * Mathf.Rad2Deg;
            }
            Quaternion rotation = Quaternion.Euler(0f, 0f, tangentAngle);

            // 피봇 보정 적용
            Vector3 pivotOffset = rotation * CardPivot;
            Vector3 finalLocalPosition = basePosition - pivotOffset;
            Quaternion finalLocalRotation = rotation;

            // --- 변경된 부분: DOTween을 사용하여 보간 적용 ---
            if (instantArrange)
            {
                // 즉시 정렬 모드에서는 보간 없이 바로 위치/회전 설정
                cardSlot.localPosition = finalLocalPosition;
                cardSlot.localRotation = finalLocalRotation;
            }
            else
            {
                // 이전 트윈이 있다면 중복 방지를 위해 강제로 종료
                cardSlot.DOKill(true);

                // DOTween을 사용하여 목표 위치와 회전으로 부드럽게 이동/회전
                cardSlot.DOLocalMove(finalLocalPosition, arrangeDuration).SetEase(arrangeEase).SetLink(cardSlot.gameObject);
                cardSlot.DOLocalRotateQuaternion(finalLocalRotation, arrangeDuration).SetEase(arrangeEase).SetLink(cardSlot.gameObject);
            }
            // ------------------------------------------------
        }
    }

    // UpdateCardOrderDuringDrag 함수는 변경 없이 그대로 유지됩니다.
    public void UpdateCardOrderDuringDrag(Transform draggedSlot)
    {
        if (draggedSlot == null) return;
        int newIndex = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform otherSlot = transform.GetChild(i);
            if (otherSlot == draggedSlot) continue;
            if (draggedSlot.GetChild(0).position.x > otherSlot.position.x)
            {
                newIndex++;
            }
        }
        if (draggedSlot.GetSiblingIndex() != newIndex)
        {
            draggedSlot.SetSiblingIndex(newIndex);
        }

        // 드래그 중인 자기 자신(draggedSlot)을 제외하고 나머지 카드들을 정렬합니다.
        // 이때, ArrangeCards는 이제 보간을 적용합니다.
        ArrangeCards(draggedSlot);
    }
}
