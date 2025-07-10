using UnityEngine;
using System.Collections;
public class RoundSlot : MonoBehaviour
{
    public CardSlot[] PlayerCardSlots;
    public CardSlot[] EnemyCardSlots;
    public int Index;
    public Transform Stone;
    // 마지막으로 이동한 타겟 위치 저장
    private Vector3 _lastStoneTargetPos = Vector3.zero;
    private int _currentOwner = 0;
    public void MoveStoneToOwner(int owner)
    {
        if (Stone == null)
            return;
        if (_currentOwner == owner)
            return;

        Vector3 basePos = Vector3.zero;
        float offset = 0.5f;
        Vector3 targetPos = basePos;
        if (owner == 1)
            targetPos = basePos + (-transform.up) * offset;
        else if (owner == 2)
            targetPos = basePos + (transform.up) * offset;

        if (_lastStoneTargetPos == targetPos)
            return;

        _lastStoneTargetPos = targetPos;
        if (owner == 1 || owner == 2)
            _currentOwner = owner;
        else
            _currentOwner = 0;

        StopAllCoroutines();
        StartCoroutine(MoveStoneCoroutine(targetPos));
    }

    private IEnumerator MoveStoneCoroutine(Vector3 target)
    {
        float t = 0;
        Vector3 start = Stone.localPosition;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            Stone.localPosition = Vector3.Lerp(start, target, t);
            yield return null;
        }
        Stone.localPosition = target;
    }
    public UI_Round UI_Round;
}
