using EPOOutline;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
// IPointerEnterHandler, IPointerExitHandler 인터페이스는 이미 추가되어 있습니다.
public class UI_CardDragger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private bool _isDragging = false;
    private Vector3 _dragOffset;
    private Vector3 _originPosition;
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private int _originalSiblingIndex;
    private Transform _slotTransform;
    private Camera _mainCamera;
    private HandCardSlot _handCardSlot;
    private CardHandArranger _handArranger;
    [SerializeField]private Vector3 _originalScale;

    // <<< 추가: Outlinable 컴포넌트를 저장할 변수
    private Outlinable _outlinable;

    private void Awake()
    {

    }

    void Start()
    {
       Init();
    }

    public void Init()
    {
        _originPosition = transform.position;
        _mainCamera = Camera.main;
        _handCardSlot = GetComponentInParent<HandCardSlot>();
        _handArranger = GetComponentInParent<CardHandArranger>();
        _slotTransform = transform.parent;
        _originalScale = transform.localScale;

        // <<< 추가: 컴포넌트를 처음 한 번만 찾아옵니다.
        _outlinable = GetComponent<Outlinable>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

#if UNITY_EDITOR || UNITY_STANDALONE
        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : eventData.position; // fallback
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _mainCamera.nearClipPlane));
#elif UNITY_ANDROID || UNITY_IOS
        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, _mainCamera.nearClipPlane));
#endif

        _dragOffset = transform.position - worldPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;

#if UNITY_EDITOR || UNITY_STANDALONE
        Vector2 mousePos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : eventData.position;
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _mainCamera.nearClipPlane));
#elif UNITY_ANDROID || UNITY_IOS
        Vector2 touchPos = Touchscreen.current.primaryTouch.position.ReadValue();
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, _mainCamera.nearClipPlane));
#endif

        transform.position = worldPos + _dragOffset;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (_handArranger != null)
        {
            _handArranger.enabled = true;
            _handArranger.ArrangeCards();
        }

        // --- 분기: 솔로/멀티 ---
        bool isSolo = FindObjectOfType<BattleField_AI>() != null;
        bool isMyTurn = false;

        if (isSolo)
        {
            var battleFieldAI = FindObjectOfType<BattleField_AI>();
            // 솔로플레이: 내 턴(플레이어1)만 배치 허용
            isMyTurn = battleFieldAI.CurrentTurn == ETurn.Player1;
        }
        else
        {
            bool isMaster = PhotonNetwork.IsMasterClient;
            ETurn currentTurn = BattleField.Instance.CurrentTurn;
            isMyTurn = (isMaster && currentTurn == ETurn.Player1) || (!isMaster && currentTurn == ETurn.Player2);
        }

        if (!isMyTurn)
        {
            // 자기 턴이 아니면 배치 불가 → 제자리 복귀
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
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

                // 슬롯에 이미 카드가 있다면 배치 못함
                if (cardSlot.IsOccupied)
                {
                    break;
                }

                cardSlot.Refresh(_handCardSlot.Card);
                _handCardSlot.Clear();

                // --- 분기: 후처리 ---
                if (isSolo)
                {
                    var battleFieldAI = FindObjectOfType<BattleField_AI>();
                    if (battleFieldAI != null)
                        battleFieldAI.OnMyCardPlaced(cardSlot);
                }
                else
                {
                    // 멀티플레이 후처리 (기존대로)
                    BattleField.Instance.OnMyCardPlaced(cardSlot);
                }

                // 원위치로 되돌리기
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isDragging) return;

        // <<< 변경: Outlinable 컴포넌트 활성화
        if (_outlinable != null)
        {
            _outlinable.enabled = true;
        }

        transform.localScale = _originalScale * 1.1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isDragging) return;

        if (_outlinable != null)
        {
            _outlinable.enabled = false;
        }

        transform.localScale = _originalScale;
    }
}
