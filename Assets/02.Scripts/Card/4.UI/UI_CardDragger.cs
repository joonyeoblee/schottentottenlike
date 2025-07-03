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
    private UI_CardMove _uiCardMove; // UI_CardMove 컴포넌트 참조

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
        _uiCardMove = GetComponent<UI_CardMove>(); // 현재 GameObject에서 UI_CardMove 컴포넌트 가져오기
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        if (_handArranger != null) _handArranger.enabled = false; // 드래그 시작 시 Arranger 비활성화

        _originalPosition = transform.localPosition;
        _originalRotation = transform.localRotation;
        _originalSiblingIndex = _slotTransform.GetSiblingIndex();

        // 드래그 시작 시 카드의 회전을 초기화합니다.
        if (_uiCardMove != null)
        {
            _uiCardMove.ResetRotation(); // CardMove 스크립트의 ResetRotation 메서드 호출
        }

        transform.rotation = Quaternion.identity; // 카드 자체 회전 즉시 리셋 (선택 사항)
        _slotTransform.SetAsLastSibling(); // 드래그 중인 카드를 가장 위로

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _dragOffset = transform.position - mousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x + _dragOffset.x, mousePos.y + _dragOffset.y, -1f);

        // UI_CardMove에 X축 델타 값을 전달하여 드래그 중인 카드를 회전시킵니다.
        if (_uiCardMove != null)
        {
            float mouseXDelta = Input.GetAxis("Mouse X"); // 마우스 X축 변화량 가져오기
            _uiCardMove.RotateCardByXDelta(mouseXDelta);
        }

        // CardHandArranger에게 드래그 중인 카드 정보를 전달하여 나머지 카드를 보간 정렬합니다.
        if (_handArranger != null)
        {
            _handArranger.UpdateCardOrderDuringDrag(_slotTransform);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        // 슬롯 로직 (기존 코드 유지)
        //********************

        // 드래그가 끝나면 현재 드래그했던 카드를 원래 위치와 회전으로 즉시 복원합니다.
        // 여기서 보간 없이 바로 위치를 설정합니다.
        transform.localPosition = _originalPosition;
        transform.localRotation = _originalRotation;

        // Arranger를 다시 활성화하고 전체 카드를 최종적으로 한 번 정렬합니다.
        // 이때, 드래그했던 카드도 정렬에 포함되어 보간됩니다.
        if (_handArranger != null)
        {
            _handArranger.enabled = true;
            _handArranger.ArrangeCards(); // 드래그했던 카드 포함하여 모든 카드를 보간 정렬
        }
    }
}
