using EPOOutline;
using UnityEngine;
using UnityEngine.EventSystems;
[RequireComponent(typeof(Transform))]
public class TargetSelector : BaseSelectable, IPointerClickHandler
{
    private Outlinable _outlinable;

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
            Debug.Log($"카드 클릭됨using EPOOutline;\nusing UnityEngine;\nusing UnityEngine.EventSystems;\n[RequireComponent(typeof(Transform))]\npublic class TargetSelector : BaseSelectable, IPointerClickHandler\n{{\n    private Outlinable _outlinable;\n\n    private void Reset()\n    {{\n        if (GetComponent<Collider2D>() == null)\n            gameObject.AddComponent<BoxCollider2D>();\n\n        if (GetComponent<Outlinable>() == null)\n            gameObject.AddComponent<Outlinable>();\n    }}\n\n    private void Start()\n    {{\n        _outlinable = GetComponent<Outlinable>();\n        _outlinable.enabled = false;\n    }}\n\n    public void OnPointerClick(PointerEventData eventData)\n    {{\n        CardSlot cardSlot = GetComponent<CardSlot>();\n        if (cardSlot != null)\n        {{\n            Debug.Log($\"카드 클릭됨.{{cardSlot.Card?.CardNumber}}\");\n        }}\n        // if (cardSlot.Card == null)\n        //     Debug.LogWarning(\"이 슬롯에 카드가 없음!\");\n    }}\n\n    public void OnPointerEnter(PointerEventData eventData)\n    {{\n        _outlinable.enabled = true;\n    }}\n\n    public void OnPointerExit(PointerEventData eventData)\n    {{\n        _outlinable.enabled = false;\n    }}\n\n    public void ActivateOutlinable()\n    {{\n        _outlinable.enabled = true;\n    }}\n\n    public void DeactivateOutlinable()\n    {{\n        _outlinable.enabled = false;\n    }}\n\n    public void ChangeOutlineColor(Color color)\n    {{\n        _outlinable.OutlineParameters.Color = color;\n    }}\n}}.{cardSlot.Card?.CardNumber}");
        }
        // if (cardSlot.Card == null)
        //     Debug.LogWarning("이 슬롯에 카드가 없음!");
    }
}