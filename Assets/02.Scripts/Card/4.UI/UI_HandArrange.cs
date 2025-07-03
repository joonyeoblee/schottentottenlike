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
            list.Add(child);
        }
        return list;
    }

    // 위치와 회전 계산
    private void CalculateCardTransform(int index, int cardCount, out Vector3 position, out Quaternion rotation)
    {
        float totalWidth = (cardCount - 1) * spacing;
        float effectiveWidth = Mathf.Min(totalWidth, arcWidth);

        float t = (cardCount > 1) ? (float)index / (cardCount - 1) : 0.5f;
        float xPos = Mathf.Lerp(-effectiveWidth / 2f, effectiveWidth / 2f, t);

        float yPos = (arcHeight > 0 && effectiveWidth > 0)
            ? -(xPos * xPos) * (arcHeight / (effectiveWidth * effectiveWidth / 4f))
            : 0f;

        float zPos = index * cardDepth;
        position = new Vector3(xPos, yPos, zPos);

        float tangentAngle = (effectiveWidth > 0)
            ? Mathf.Atan(-(8f * arcHeight / (effectiveWidth * effectiveWidth)) * xPos) * Mathf.Rad2Deg
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

    /// <summary>
    /// 외부에서 명시적으로 호출하여 즉시 정렬 (예: 드래그 종료 후)
    /// </summary>
    public void ArrangeCards(Transform cardToIgnore = null)
    {
        List<Transform> sourceCards = GetChildList();
        lastArrangedCards = new List<Transform>(sourceCards); // 리스트 상태 갱신

        int cardCount = sourceCards.Count;
        if (cardCount == 0) return;

        for (int i = 0; i < cardCount; i++)
        {
            Transform cardSlot = sourceCards[i];
            if (cardSlot == null || cardSlot == cardToIgnore) continue;

            CalculateCardTransform(i, cardCount, out Vector3 pos, out Quaternion rot);
            ApplyCardTransform(cardSlot, pos, rot, false); // 즉시 정렬
        }
    }

    /// <summary>
    /// 변화가 감지되었을 때 자동으로 호출되어 부드럽게 정렬
    /// </summary>
    public void UpdateArrange(Transform cardToIgnore = null)
    {
        List<Transform> sourceCards = GetChildList();
        lastArrangedCards = new List<Transform>(sourceCards); // 리스트 상태 갱신

        int cardCount = sourceCards.Count;
        if (cardCount == 0) return;

        for (int i = 0; i < cardCount; i++)
        {
            Transform cardSlot = sourceCards[i];
            if (cardSlot == null || cardSlot == cardToIgnore) continue;

            CalculateCardTransform(i, cardCount, out Vector3 pos, out Quaternion rot);
            ApplyCardTransform(cardSlot, pos, rot, true); // DOTween으로 부드럽게 정렬
        }
    }

    /// <summary>
    /// 드래그 중 위치 재계산 (즉시 이동 X)
    /// </summary>
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

        UpdateArrange(draggedSlot); // 드래그 중에는 부드럽게 정렬
    }
}
