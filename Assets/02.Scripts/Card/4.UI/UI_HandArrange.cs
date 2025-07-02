using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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

    private List<Transform> lastArrangedCards = new List<Transform>();

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        ArrangeCards();
    }

    private void Update()
    {
        var currentChildren = GetChildList();
        if (!currentChildren.SequenceEqual(lastArrangedCards))
        {
            ArrangeCards();
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

  // 1. ArrangeCards 함수를 수정하여 특정 카드를 무시하는 기능을 추가합니다.
public void ArrangeCards(Transform cardToIgnore = null) // 예외 처리할 카드를 매개변수로 받음
{
    List<Transform> sourceCards = GetChildList();
    lastArrangedCards = new List<Transform>(sourceCards);

    if (sourceCards.Count == 0) return;

    // 왼쪽부터 정렬 (이전과 동일)
    int cardCount = sourceCards.Count;
    float totalWidth = (cardCount - 1) * spacing;
    float effectiveWidth = Mathf.Min(totalWidth, arcWidth);

    for (int i = 0; i < cardCount; i++)
    {
        Transform cardSlot = sourceCards[i];
        if (cardSlot == null) continue;

        // 만약 이 카드가 '무시해야 할 카드'라면 정렬 로직을 건너뜁니다.
        if (cardSlot == cardToIgnore) continue;

        float t = (cardCount > 1) ? (float)i / (cardCount - 1) : 0.5f;
        float xPos = Mathf.Lerp(-(effectiveWidth +CardPivot.x) / 2f, (effectiveWidth +CardPivot.x) / 2f, t);

        float yPos = 0;
        if ((arcHeight + CardPivot.y) > 0 && effectiveWidth > 0)
        {
            yPos = -(xPos * xPos) * ((arcHeight + CardPivot.y) / ((effectiveWidth +CardPivot.x) * (effectiveWidth +CardPivot.x) / 4f));
        }
        float zPos = -i * cardDepth;
        cardSlot.localPosition = new Vector3(xPos, yPos, zPos);

        float tangentAngle = 0;
        if ((effectiveWidth +CardPivot.x) > 0)
        {
            float derivative = -(8 * (arcHeight + CardPivot.y) / (effectiveWidth +CardPivot.x) * (effectiveWidth +CardPivot.x)) * xPos;
            tangentAngle = Mathf.Atan(derivative) * Mathf.Rad2Deg;
        }
        cardSlot.localRotation = Quaternion.Euler(0, 0, tangentAngle);
    }
}

// 2. UpdateCardOrderDuringDrag 함수가 수정된 ArrangeCards를 호출하도록 변경합니다.
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
    ArrangeCards(draggedSlot);
}


}
