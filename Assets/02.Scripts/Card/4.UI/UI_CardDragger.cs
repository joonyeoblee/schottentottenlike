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

    // 터치 시 카드를 손가락 위로 띄워서 손가락에 가려지지 않게 합니다. (월드 단위)
    private const float TouchLift = 1.1f;
    private const float DragScale = 1.15f;
    // 손패는 화면 아래에 반쯤 걸쳐 있으므로, 마우스를 올리면 카드가 떠올라 전체가 보이게 합니다. (월드 단위)
    private const float HoverLift = 1.0f;
    private const int RaisedSortingBoost = 50;

    private SpriteRenderer _renderer;
    private bool _raised;

    /// <summary>카드를 다른 카드 위로 올려 그리기 (호버/드래그 중)</summary>
    private void SetRaised(bool on)
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null || _raised == on) return;
        _raised = on;
        _renderer.sortingOrder += on ? RaisedSortingBoost : -RaisedSortingBoost;
    }

    /// <summary>호버 시 카드를 화면 위쪽(월드 +y) 방향으로 띄움</summary>
    private void SetHoverLift(bool on)
    {
        if (_slotTransform == null) return;
        if (on)
        {
            Vector3 worldUp = Vector3.up * HoverLift;
            Vector3 local = _slotTransform.InverseTransformVector(worldUp);
            transform.localPosition = new Vector3(local.x, local.y, -0.5f);
            transform.rotation = Quaternion.identity; // 똑바로 세워서 보기 쉽게
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 마우스/터치 공통 포인터 위치 → 월드 좌표. (eventData.position은 마우스와 터치 모두에서 동작)
    /// </summary>
    private Vector3 PointerWorld(PointerEventData eventData)
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        Vector2 screen = eventData != null ? eventData.position
            : (Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero);
        return _mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, _mainCamera.nearClipPlane));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 카드가 없는 슬롯이거나 다른 손가락이 이미 드래그 중이면 무시 (멀티터치 방지)
        // 상대 손패 카드에도 같은 컴포넌트가 붙어 있으므로 내 카드만 허용
        if (_handCardSlot == null || !_handCardSlot.IsMine || !_handCardSlot.HasCard || (_activeDragger != null && _activeDragger != this)) return;

        _isDragging = true;
        _activeDragger = this;

        Vector3 worldPos = PointerWorld(eventData);
        _dragOffset = transform.position - worldPos;

        // 터치: 카드를 손가락 위로 들어 올림
        bool touch = eventData != null && eventData.pointerId >= 0 && Touchscreen.current != null;
        if (touch) _dragOffset = new Vector3(0, TouchLift, _dragOffset.z);

        transform.localScale = _originalScale * DragScale;
        transform.rotation = Quaternion.identity; // 들어 올린 카드는 똑바로
        SetRaised(true);
        if (_outlinable != null) _outlinable.enabled = true;
        DropHighlight.Show(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        transform.position = PointerWorld(eventData) + _dragOffset;
    }

    private static UI_CardDragger _activeDragger;

    private void OnDisable()
    {
        if (_activeDragger == this)
        {
            _activeDragger = null;
            DropHighlight.Show(false);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;
        _activeDragger = null;
        DropHighlight.Show(false);
        transform.localScale = _originalScale;
        SetRaised(false);
        if (_outlinable != null) _outlinable.enabled = false;

        if (_handArranger != null)
        {
            _handArranger.enabled = true;
            _handArranger.ArrangeCards();
        }

        // --- 분기: 솔로/멀티 ---
        var battleFieldAI = BattleField_AI.Instance;
        bool isSolo = battleFieldAI != null;
        bool isMyTurn;

        if (isSolo)
        {
            // 솔로플레이: 내 턴(플레이어1)만 배치 허용
            isMyTurn = battleFieldAI.IsPlayerTurn;
        }
        else
        {
            isMyTurn = BattleField.Instance != null && BattleField.Instance.IsMyTurn;
        }

        if (!isMyTurn || _handCardSlot == null || !_handCardSlot.HasCard)
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

                // 놓을 수 없는 슬롯(상대 슬롯, 이미 찬 슬롯, 점령된 경계석)이면 다른 히트를 계속 확인
                bool canPlace = isSolo
                    ? battleFieldAI.CanPlayerPlaceOn(cardSlot)
                    : BattleField.Instance.CanPlaceOn(cardSlot);
                if (!canPlace) continue;

                cardSlot.Refresh(_handCardSlot.Card);
                _handCardSlot.Clear();

                // --- 분기: 후처리 ---
                if (isSolo)
                {
                    battleFieldAI.OnMyCardPlaced(cardSlot);
                }
                else
                {
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
        // 터치에는 '호버'가 없으므로 마우스일 때만 확대 연출
        if (_isDragging || (eventData != null && eventData.pointerId >= 0)) return;

        if (_handCardSlot == null || !_handCardSlot.IsMine || !_handCardSlot.HasCard) return;

        // <<< 변경: Outlinable 컴포넌트 활성화
        if (_outlinable != null)
        {
            _outlinable.enabled = true;
        }

        // 하스스톤처럼: 카드가 떠오르고 커져서 전체가 보임
        transform.localScale = _originalScale * 1.25f;
        SetHoverLift(true);
        SetRaised(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isDragging || (eventData != null && eventData.pointerId >= 0)) return;

        if (_outlinable != null)
        {
            _outlinable.enabled = false;
        }

        transform.localScale = _originalScale;
        SetHoverLift(false);
        SetRaised(false);
    }
}
