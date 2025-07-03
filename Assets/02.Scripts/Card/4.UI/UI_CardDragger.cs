using EPOOutline;
using Photon.Pun;
using UnityEngine;
using UnityEngine.EventSystems;

// IPointerEnterHandler, IPointerExitHandler 인터페이스는 이미 추가되어 있습니다.
public class UI_CardDragger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
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
    private Vector3 _originalScale;

    // <<< 추가: Outlinable 컴포넌트를 저장할 변수
    private Outlinable _outlinable;

    void Start()
    {
       Init();
    }

    public void Init()
    {
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
        // <<< 추가: 드래그 시작 시 아웃라인 끄기
        if (_outlinable != null)
        {
            _outlinable.enabled = false;
        }

        transform.localScale = _originalScale;
        _isDragging = true;
        if (_handArranger != null) _handArranger.enabled = false;

        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
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

        if (_handArranger != null)
        {
            _handArranger.UpdateCardOrderDuringDrag(_slotTransform);
        }
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

        // ★ 바로 원위치
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
