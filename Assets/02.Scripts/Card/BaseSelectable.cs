using EPOOutline;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class BaseSelectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    protected Outlinable _outlinable;
    
    protected virtual void Awake()
    {
        _outlinable = GetComponent<Outlinable>();
        _outlinable.enabled = false;
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _outlinable.enabled = true;
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _outlinable.enabled = false;
    }
    
    public void ActivateOutlinable()
    {
        _outlinable.enabled = true;
    }

    public void DeactivateOutlinable()
    {
        _outlinable.enabled = false;
    }

    public void ChangeOutlineColor(Color color)
    {
        if (_outlinable != null)
        {
            _outlinable.OutlineParameters.Color = color;
        }
    }
}
