using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_CardDragger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private int _originalSiblingIndex;
    private Transform _slotTransform;
    private Camera _mainCamera;
    private HandCardSlot _handCardSlot;
    private CardHandArranger _handArranger;

    void Start()
    {
        _mainCamera = Camera.main;
        _handCardSlot = GetComponentInParent<HandCardSlot>(); // GetComponent 대신 GetComponentInParent 사용
        _handArranger = GetComponentInParent<CardHandArranger>();
        _slotTransform = transform.parent;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        if (_handArranger != null) _handArranger.enabled = false;

        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation; // localRotation으로 변경
        _originalSiblingIndex = _slotTransform.GetSiblingIndex();

        transform.rotation = Quaternion.identity;
        _slotTransform.SetAsLastSibling();

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _dragOffset = transform.position - mousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x + _dragOffset.x, mousePos.y + _dragOffset.y, -1f);

        // CHANGED: Swap 로직을 직접 수행하는 대신, Arranger에게 위임합니다.
        if (_handArranger != null)
        {
            _handArranger.UpdateCardOrderDuringDrag(_slotTransform);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        //슬롯 로직

//********************


        // 슬롯에 카드를 놓지 못했다면 원래 위치로 복원

        transform.localRotation = _originalRotation;
        transform.localPosition = _originalPosition;

        // Arranger를 마지막에 활성화하여 최종적으로 한 번만 정렬하도록 합니다.
        if (_handArranger != null)
        {
            _handArranger.enabled = true;
            _handArranger.ArrangeCards();
        }
    }
}
