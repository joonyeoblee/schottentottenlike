using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드를 끌고 있을 때(또는 튜토리얼 안내 중) 카드를 놓을 수 있는 칸을 금빛으로 깜빡이게 표시합니다.
/// </summary>
public class DropHighlight : MonoBehaviour
{
    private static DropHighlight _runner;
    private static readonly List<CardSlot> Active = new List<CardSlot>();
    private static bool _drag;
    private static bool _coach;

    /// <summary>드래그 중 표시</summary>
    public static void Show(bool on)
    {
        _drag = on;
        Refresh();
    }

    /// <summary>튜토리얼 안내용 표시 (드래그와 별개로 유지)</summary>
    public static void Coach(bool on)
    {
        _coach = on;
        Refresh();
    }

    private static void Refresh()
    {
        foreach (var s in Active) if (s != null) s.SetHighlight(false, 0);
        Active.Clear();
        if (!_drag && !_coach) return;

        foreach (var round in FindObjectsByType<RoundSlot>(FindObjectsSortMode.None))
        {
            var slot = round.FirstEmptySlot(true);
            if (slot == null || !CanPlace(slot)) continue;
            Active.Add(slot);
        }

        if (_runner == null)
        {
            var go = new GameObject("DropHighlight");
            go.hideFlags = HideFlags.HideInHierarchy;
            _runner = go.AddComponent<DropHighlight>();
        }
    }

    private static bool CanPlace(CardSlot slot)
    {
        if (BattleField_AI.Instance != null) return BattleField_AI.Instance.CanPlayerPlaceOn(slot);
        if (BattleField.Instance != null) return BattleField.Instance.CanPlaceOn(slot);
        return false;
    }

    private void Update()
    {
        if (Active.Count == 0) return;
        float k = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f); // 항상 충분히 밝게 + 은은한 깜빡임
        foreach (var s in Active) if (s != null) s.SetHighlight(true, k);
    }
}
