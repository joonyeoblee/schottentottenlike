using DG.Tweening;
using UnityEngine;

public class UI_CardDrawShow : MonoBehaviour
{
    [Header("애니메이션 대상")]
    public Transform objectToMove; // 애니메이션을 적용할 오브젝트 (Card_UI)

    [Header("이동 경로")]
    public Transform startPoint;   // 1. 시작 지점
    public Transform midPoint;     // 2. 중간 지점
    public Transform HandPoint;    // 3. 카드가 최종적으로 위치할 부모 오브젝트 (핸드)

    [Header("애니메이션 설정")]
    public float durationToMid = 0.5f;
    public float durationToEnd = 0.5f;
    public Ease easeType = Ease.OutQuad;
  //  public bool faceForward = true;

    void Start()
    {
        if (objectToMove == null || startPoint == null || midPoint == null || HandPoint == null)
        {
            Debug.LogError("필수 Transform이 할당되지 않았습니다. 인스펙터 창을 확인해주세요.");
            return;
        }
        CreateMovementSequence();
    }

    public void CreateMovementSequence()
    {
        Transform finalDestination;

        if (HandPoint.childCount > 0)
        {
            finalDestination = HandPoint.GetChild(HandPoint.childCount - 1);
        }
        else
        {
            finalDestination = HandPoint;
        }

        Sequence mySequence = DOTween.Sequence();

        objectToMove.position = startPoint.position;
        objectToMove.rotation = startPoint.rotation;

        mySequence.Append(objectToMove.DOMove(midPoint.position, durationToMid).SetEase(easeType));
        mySequence.Join(objectToMove.DORotate(new Vector3(0,0,0), durationToMid ).SetEase(easeType));

        // if (faceForward)
        // {
        //     mySequence.Insert(0, objectToMove.DOLookAt(midPoint.position, 0.3f));
        // }

        mySequence.Append(objectToMove.DOMove(finalDestination.position, durationToEnd).SetEase(easeType));
        mySequence.Join(objectToMove.DORotate(finalDestination.rotation.eulerAngles, durationToEnd ).SetEase(easeType));

        // if (faceForward)
        // {
        //     mySequence.Join(objectToMove.DOLookAt(finalDestination.position, 0.3f));
        // }

        // 4. 모든 움직임이 끝난 후 실행할 작업 추가: 'objectToMove의 부모'를 HandPoint의 자식으로 설정
        mySequence.AppendCallback(() => {
            // objectToMove(Card_UI)에 부모(Card_Slot)가 있는지 확인합니다.
            if (objectToMove.parent != null)
            {
                Debug.Log($"애니메이션 완료! {objectToMove.parent.name}을(를) {HandPoint.name}의 자식으로 설정합니다.");
                // objectToMove의 부모(Card_Slot)를 HandPoint의 자식으로 설정합니다.
                objectToMove.parent.SetParent(HandPoint);
                this.transform.localPosition = Vector3.zero;
                this.transform.localRotation = Quaternion.identity;
                GetComponent<UI_CardDragger>().Init();
                HandPoint.GetComponent<CardHandArranger>().ArrangeCards();


            }
            else
            {
                // 혹시 모를 예외 상황: 부모가 없다면 그냥 objectToMove를 자식으로 넣습니다.
                Debug.LogWarning($"{objectToMove.name}에 부모가 없어 직접 {HandPoint.name}의 자식으로 설정합니다.");
                objectToMove.SetParent(HandPoint);
            }
        });
    }
}
