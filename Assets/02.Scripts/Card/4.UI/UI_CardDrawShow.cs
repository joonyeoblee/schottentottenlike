using DG.Tweening;
using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 추가

public class UI_CardDrawShow : MonoBehaviour
{
    [Header("애니메이션 대상")]
    public Transform objectToMove; // Card_UI

    [Header("이동 경로")]
    public Transform startPoint;
    public Transform midPoint;
    public Transform HandPoint; // 카드 슬롯들이 모이는 곳

    [Header("애니메이션 설정")]
    public float durationToMid = 0.5f;
    public float durationToEnd = 0.5f;
    public Ease easeType = Ease.OutQuad;

    [SerializeField]private Transform _originalParent;

    void Start()
    {
        if (objectToMove == null || startPoint == null || midPoint == null || HandPoint == null)
        {
            Debug.LogError("필수 Transform이 할당되지 않았습니다.");
            return;
        }

        // 전체 드로우 프로세스를 코루틴으로 시작
        StartCoroutine(DrawProcessCoroutine());
    }

    /// <summary>
    /// '자리 만들기'가 포함된 드로우 애니메이션을 순차적으로 처리하는 코루틴
    /// </summary>
    private IEnumerator DrawProcessCoroutine()
    {
        // --- 1단계: 준비 ---
        // 카드(UI)를 슬롯에서 분리하고 시작점으로 이동
        Debug.Log("0. 시퀀스 준비");
        if (_originalParent == null)
        {
            _originalParent = objectToMove.parent;
        }
        DOTween.Kill(objectToMove);
        objectToMove.position = startPoint.position;
        objectToMove.rotation = startPoint.rotation;


        // --- 2단계: 자리 만들기 ---
        // 빈 슬롯(_originalParent)을 먼저 HandPoint에 넣습니다.
        // 이 순간 CardHandArranger가 자동으로 '자리 만들기' 애니메이션을 시작합니다.
        Debug.Log("1. 자리 만들기 시작: 빈 슬롯을 핸드에 추가합니다.");

        if (objectToMove.parent != null)
        {
            objectToMove.SetParent(null, true);
        }

        _originalParent.SetParent(HandPoint);


        // --- 3단계: 대기 ---
        // CardHandArranger의 정렬 애니메이션 시간(0.2초)만큼 기다려줍니다.
        float arrangementDuration = 0.2f;
        Debug.Log($"2. {arrangementDuration}초 동안 자리 만들기를 기다립니다.");
        yield return new WaitForSeconds(arrangementDuration);


        // --- 4단계: 카드의 비행 애니메이션 실행 ---
        // 이제 슬롯이 최종 위치에 도착했으므로, 그 위치를 목적지로 삼아 비행 시퀀스를 생성하고 실행합니다.
        Debug.Log("3. 카드를 빈 자리로 비행시킵니다.");
        Sequence flightSequence = CreateFlightSequence(_originalParent.position, _originalParent.rotation);
        flightSequence.Play();
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

        // Part 1: 시작 -> 중간
        mySequence.Append(objectToMove.DOMove(midPoint.position, durationToMid).SetEase(easeType))
                  .Join(objectToMove.DORotate(Vector3.zero, durationToMid).SetEase(easeType));

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
        });

        return mySequence;
    }
}
