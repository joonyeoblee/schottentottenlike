using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

[ExecuteInEditMode]
public class CardHandArranger : MonoBehaviour
{
    [Header("Arrangement Settings")]
    [Range(0f, 20f)] public float arcWidth = 10f;
    [Range(0f, 10f)] public float arcHeight = 2f;
    [Range(0f, 2f)] public float spacing = 1.2f;
    [Range(0.01f, 0.5f)] public float cardDepth = 0.1f;
    public Vector3 CardPivot;

    private List<Transform> lastArrangedCards = new List<Transform>();

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        ArrangeCards(); // 에디터에서 즉시 정렬
    }

    private void Update()
    {
        var currentChildren = GetChildList();
        if (!currentChildren.SequenceEqual(lastArrangedCards))
        {
            UpdateArrange(); // ✅ 변화가 있으면 부드럽게 정렬
        }
    }

    private List<Transform> GetChildList()
    {
        List<Transform> list = new List<Transform>();
        foreach (Transform child in transform)
        {
            HandCardSlot slot = child.GetComponent<HandCardSlot>();
            if (slot != null && !slot.IsEmpty)
            {
                list.Add(child);
            }
        }
        return list;
    }

    // ===================================================================
    // [수정된 부분]
    // ===================================================================
    // 위치와 회전 계산
    private void CalculateCardTransform(int index, int cardCount, out Vector3 position, out Quaternion rotation)
    {
        // 1. 카드를 중앙(0) 기준으로 좌우로 배치하기 위한 오프셋을 계산합니다.
        float centerOffset = (cardCount - 1) / 2.0f;

        // 2. 각 카드의 x 위치를 계산합니다. spacing은 항상 일정하게 유지됩니다.
        float xPos = (index - centerOffset) * spacing;

        // 3. 호(arc)의 모양을 결정하는 너비로는 arcWidth를 직접 사용합니다.
        float parabolaWidth = arcWidth > 0 ? arcWidth : 0.001f;

        // 4. 계산된 x 위치를 기반으로 호의 y 위치를 계산합니다.
        float yPos = (arcHeight > 0)
            ? -(xPos * xPos) * (arcHeight / (parabolaWidth * parabolaWidth / 4f))
            : 0f;

        // 5. Z 위치는 그대로 둡니다 (겹침 효과).
        float zPos = index * cardDepth;
        position = new Vector3(xPos, yPos, zPos);

        // 6. 카드의 기울기도 x 위치와 arcWidth를 기반으로 계산합니다.
        float tangentAngle = (arcHeight > 0)
            ? Mathf.Atan(-(8f * arcHeight / (parabolaWidth * parabolaWidth)) * xPos) * Mathf.Rad2Deg
            : 0f;

        rotation = Quaternion.Euler(0f, 0f, tangentAngle);
    }

    // 적용
    private void ApplyCardTransform(Transform cardSlot, Vector3 position, Quaternion rotation, bool useTween)
    {
        Vector3 pivotOffset = rotation * CardPivot;

        if (useTween)
        {
            cardSlot.DOLocalMove(position - pivotOffset, 0.2f).SetEase(Ease.OutQuad);
            cardSlot.DOLocalRotateQuaternion(rotation, 0.2f).SetEase(Ease.OutQuad);
        }
        else
        {
            cardSlot.localPosition = position - pivotOffset;
            cardSlot.localRotation = rotation;
        }
    }

    public void ArrangeCards(Transform cardToIgnore = null)
    {
        List<Transform> sourceCards = GetChildList();
        lastArrangedCards = new List<Transform>(sourceCards);

        int cardCount = sourceCards.Count;
        if (cardCount == 0) return;

        for (int i = 0; i < cardCount; i++)
        {
            Transform cardSlot = sourceCards[i];
            if (cardSlot == null || cardSlot == cardToIgnore) continue;

            CalculateCardTransform(i, cardCount, out Vector3 pos, out Quaternion rot);
            ApplyCardTransform(cardSlot, pos, rot, false);
        }
    }

    public void UpdateArrange(Transform cardToIgnore = null)
    {
        List<Transform> sourceCards = GetChildList();
        lastArrangedCards = new List<Transform>(sourceCards);

        int cardCount = sourceCards.Count;
        if (cardCount == 0) return;

        for (int i = 0; i < cardCount; i++)
        {
            Transform cardSlot = sourceCards[i];
            if (cardSlot == null || cardSlot == cardToIgnore) continue;

            CalculateCardTransform(i, cardCount, out Vector3 pos, out Quaternion rot);
            ApplyCardTransform(cardSlot, pos, rot, true);
        }
    }

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

        UpdateArrange(draggedSlot);
    }
}
