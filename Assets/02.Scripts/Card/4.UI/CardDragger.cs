using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragger : BaseSelectable, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Vector3 _originPosition;
    private Camera _mainCamera;
    private CardController _cardController;
    private HandCardSlot _handCardSlot;
    private void Start()
    {
        _originPosition = transform.position;
        _mainCamera = Camera.main;
        _cardController = GetComponent<CardController>();
        _handCardSlot = GetComponent<HandCardSlot>();
    }

    // 카드 드래그 시작
    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _dragOffset = transform.position - mousePos;
    }

    // 카드 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = mousePos + _dragOffset;
    }

    // 마우스 떼었을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;

        var hits = Physics2D.OverlapPointAll(transform.position);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("PlayerSlot"))
            {
                var slotCollider = hit.GetComponent<Collider2D>();
                var cardSlot = hit.GetComponent<CardSlot>();

                if (cardSlot == null || slotCollider == null) continue;

                // 슬롯에 이미 카드가 있다면 배치 못함
                if (cardSlot.IsOccupied)
                {
                    break;
                }

                cardSlot.Refresh(_handCardSlot.Card);
                _handCardSlot.Clear();
                // 원위치로 되돌리기
                transform.position = _originPosition;
                return;
            }
        }

        // 원위치로 되돌리기
        transform.position = _originPosition;
    }
}
