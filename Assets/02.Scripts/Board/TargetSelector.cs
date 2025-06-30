using EPOOutline;
using UnityEngine;
using UnityEngine.EventSystems;
[RequireComponent(typeof(Transform))]
public class TargetSelector : BaseSelectable, IPointerClickHandler
{

    private void Reset()
    {
        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();

        if (GetComponent<Outlinable>() == null)
            gameObject.AddComponent<Outlinable>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        CardSlot cardSlot = GetComponent<CardSlot>();
        if (cardSlot != null)
        {
        }
        // if (cardSlot.Card == null)
        //     Debug.LogWarning("이 슬롯에 카드가 없음!");
    }
}