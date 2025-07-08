using System;
using DG.Tweening;
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
using Sequence = DG.Tweening.Sequence; // 코루틴 사용을 위해 추가

public class UI_CardDrawShow : MonoBehaviour
{


    [Header("애니메이션 대상")]
    private Transform objectToMove; // Card_UI

    [Header("이동 경로")]
    public Transform startPoint;
    public Transform midPoint;
    public Transform HandPoint; // 카드 슬롯들이 모이는 곳

    [Header("애니메이션 설정")]
    public float durationToMid = 0.5f;
    [Tooltip("중간 지점에서 머무는 시간입니다.")]
    public float delayAtMid = 0.2f;
    public float durationToEnd = 0.5f;
    public Ease easeType = Ease.OutQuad;

    private UI_CardDragger _uiCardDragger;
    [SerializeField]private Transform _originalParent;



    void Start()
    {
Init();
    }

    private void Init()
    {
        _uiCardDragger = GetComponent<UI_CardDragger>();
        HandPoint = AnimationTransforms.Instance.PlayerHandTransform;
        midPoint = AnimationTransforms.Instance.ShowTransfrom;
        startPoint = AnimationTransforms.Instance.DeckTransfrom;

        objectToMove = this.transform;
        if (objectToMove == null || startPoint == null || midPoint == null || HandPoint == null)
        {
            Debug.LogError("필수 Transform이 할당되지 않았습니다.");
            return;
        }
    }

    /// <summary>
    /// '자리 만들기'가 포함된 드로우 애니메이션을 순차적으로 처리하는 코루틴
    /// </summary>
    public IEnumerator DrawProcessCoroutine()
    {
        Init();
        // --- 1단계: 준비 ---
        Debug.Log("0. 시퀀스 준비");
        _uiCardDragger.enabled = false;
        if (_originalParent == null)
        {
            _originalParent = objectToMove.parent; //originalParent는 카드 슬롯이다

        }
        DOTween.Kill(objectToMove);
        objectToMove.position = startPoint.position;
        objectToMove.rotation = startPoint.rotation;


        //_originalParent.SetParent(HandPoint);

        // --- 4단계: 카드의 비행 애니메이션 실행 ---
        Debug.Log("3. 카드를 빈 자리로 비행시킵니다.");
        Sequence flightSequence = CreateFlightSequence(_originalParent.position, _originalParent.rotation);
        flightSequence.Play();

        yield return new Null();

    }


    /// <summary>
    /// 카드가 목적지까지 날아가는 비행 DOTween 시퀀스를 생성합니다.
    /// </summary>
    /// <param name="finalPosition">최종 목적지 위치 (자리가 마련된 슬롯의 위치)</param>
    /// <param name="finalRotation">최종 목적지 회전</param>
    /// <returns>생성된 비행 시퀀스</returns>
    private Sequence CreateFlightSequence(Vector3 finalPosition, Quaternion finalRotation)
    {
        Sequence mySequence = DOTween.Sequence();
        mySequence.SetTarget(objectToMove);
        objectToMove.transform.SetParent(null, true);//움직일 카드 분리

        // Part 1: 시작 -> 중간
        mySequence.Append(objectToMove.DOMove(midPoint.position, durationToMid).SetEase(easeType))
                  .Join(objectToMove.DORotate(Vector3.zero, durationToMid).SetEase(easeType));

        if (delayAtMid > 0)
        {
            mySequence.AppendInterval(delayAtMid);
        }

        // Part 2: 중간 -> 최종 목적지 (준비된 자리)
        mySequence.Append(objectToMove.DOMove(finalPosition, durationToEnd).SetEase(easeType))
                  .Join(objectToMove.DORotateQuaternion(finalRotation, durationToEnd).SetEase(easeType));

        // Part 3: 완료 후 카드와 슬롯을 다시 합체
        mySequence.OnComplete(() =>
        {
            Debug.Log("4. 비행 완료. 카드와 슬롯을 다시 결합합니다.");
            objectToMove.SetParent(_originalParent);
            objectToMove.localPosition = Vector3.zero;
            objectToMove.localRotation = Quaternion.identity;
            _uiCardDragger.enabled = true;
        });

        return mySequence;
    }
}
