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

    /// <summary>
    /// 새 판 시작 시 경계석을 중앙으로 되돌립니다.
    /// </summary>
    public void ResetStone()
    {
        StopAllCoroutines();
        _currentOwner = 0;
        _lastStoneTargetPos = Vector3.zero;
        if (Stone != null)
            Stone.localPosition = Vector3.zero;
    }

    public int CountCards(bool mine)
    {
        int count = 0;
        foreach (var slot in mine ? PlayerCardSlots : EnemyCardSlots)
            if (slot != null && slot.IsOccupied) count++;
        return count;
    }

    /// <summary>
    /// 아직 비어 있는 첫 번째 슬롯 (없으면 null)
    /// </summary>
    public CardSlot FirstEmptySlot(bool mine)
    {
        foreach (var slot in mine ? PlayerCardSlots : EnemyCardSlots)
            if (slot != null && !slot.IsOccupied) return slot;
        return null;
    }

    /// <summary>
    /// 슬롯 표시 갱신: 각 진영에서 '다음에 놓을 수 있는 칸' 하나만 보이게 하고,
    /// 경계석이 점령되었으면 빈 칸은 모두 숨깁니다.
    /// </summary>
    public void RefreshSlotVisibility(bool claimed)
    {
        RefreshSide(PlayerCardSlots, claimed, true);
        // 상대 쪽은 다음 칸이 상대 카드 위에 겹쳐 보이므로, 아무 카드도 없을 때만 빈 칸을 표시
        RefreshSide(EnemyCardSlots, claimed, false);
    }

    private static void RefreshSide(CardSlot[] slots, bool claimed, bool showNextAfterCards)
    {
        bool nextShown = false;
        bool anyOccupied = false;
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            if (slot.IsOccupied)
            {
                anyOccupied = true;
                if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);
                continue;
            }

            bool show = !claimed && !nextShown && (showNextAfterCards || !anyOccupied);
            if (show) nextShown = true;
            if (slot.gameObject.activeSelf != show) slot.gameObject.SetActive(show);
        }
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
