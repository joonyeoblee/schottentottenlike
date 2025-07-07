using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
// 추가 필요

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
    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;

#if UNITY_ANDROID || UNITY_IOS
        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, _mainCamera.nearClipPlane));
#else
    Vector2 mousePos = Mouse.current.position.ReadValue();
    Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _mainCamera.nearClipPlane));
#endif

        _dragOffset = transform.position - worldPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

#if UNITY_ANDROID || UNITY_IOS
        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, _mainCamera.nearClipPlane));
#else
    Vector2 mousePos = Mouse.current.position.ReadValue();
    Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _mainCamera.nearClipPlane));
#endif

        transform.position = worldPos + _dragOffset;
    }


    // 마우스 떼었을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
        // --- 자기 턴인지 확인 ---
        var isMaster = PhotonNetwork.IsMasterClient;
        var currentTurn = BattleField.Instance.CurrentTurn;
        bool isMyTurn = (isMaster && currentTurn == ETurn.Player1) || (!isMaster && currentTurn == ETurn.Player2);
        if (!isMyTurn)
        {
            // 자기 턴이 아니면 배치 불가 → 제자리 복귀
            transform.position = _originPosition;
            return;
        }

        var hits = Physics2D.OverlapPointAll(transform.position);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("PlayerSlot"))
            {
                var slotCollider = hit.GetComponent<Collider2D>();
                var cardSlot = hit.GetComponent<CardSlot>();

                if (cardSlot == null || slotCollider == null) continue;

                // 슬롯에 이미 카드가 있다면 배치 못함 // 혹은 내꺼가 아니면
                if (cardSlot.IsOccupied || !cardSlot.IsMine)
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
