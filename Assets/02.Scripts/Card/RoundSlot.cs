using UnityEngine;
using System.Collections;
using System.ComponentModel;

public class RoundSlot : MonoBehaviour
{
    public CardSlot[] PlayerCardSlots;
    public CardSlot[] EnemyCardSlots;
    public int Index;
    public Transform Stone;

    public void MoveStoneToOwner(int owner, bool isMine)
    {
        if (Stone == null) return;

        Vector3 basePos = Vector3.zero;
        float offset = 0.5f;
        Vector3 targetPos = basePos;

        if (owner == 1)
            targetPos = basePos + (isMine ? Vector3.down : Vector3.up) * offset;
        else if (owner == 2)
            targetPos = basePos + (isMine ? Vector3.up : Vector3.down) * offset;
        // 무소유면 중앙

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
}
