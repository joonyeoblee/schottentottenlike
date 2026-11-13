using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using Random = System.Random;
public class CardDeck : MonoBehaviour
{
    public Stack<Card> cards = new Stack<Card>();
    public event Action OnCardSuffle;

    public void StartDeckSuffle()
    {
        // 마스터만 셔플
        bool isMultiplayer = PhotonNetwork.InRoom;
        bool isMaster = PhotonNetwork.IsMasterClient;

        if (isMultiplayer && !isMaster)
            return;

        List<Card> cardList = new List<Card>(GameManager.Instance.GetAllPossibleCards());

        // 셔플 Fisher-Yates shuffle algorithm
        Random rng = new Random();

        int n = cardList.Count;

        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card temp = cardList[k];
            cardList[k] = cardList[n];
            cardList[n] = temp;
        }

        cards = new Stack<Card>(cardList);
        _order = new List<Card>(cardList);

        // 멀티플레이일 때만 덱 동기화
        if (isMultiplayer && isMaster)
        {
            int[] nums = new int[cardList.Count];
            int[] colors = new int[cardList.Count];
            for (int i = 0; i < cardList.Count; i++)
            {
                nums[i] = cardList[i].CardNumber;
                colors[i] = (int)cardList[i].Color;
            }
            BattleField.Instance.photonView.RPC(nameof(BattleField.RPC_SyncDeck), RpcTarget.Others, nums, colors);
        }

        OnCardSuffle?.Invoke();
    }

    public void SyncDeckFromData(int[] nums, int[] colors)
    {
        // 방장과 같은 순서/같은 뽑기 방향으로 구성 (예전에는 뒤집힌 순서였지만 방장만 뽑아서 드러나지 않았음)
        _order = new List<Card>();
        for (int i = 0; i < nums.Length; i++)
            _order.Add(new Card(nums[i], (ECardColor)colors[i]));

        cards = new Stack<Card>(_order);
        OnCardSuffle?.Invoke();
    }

    // ---- 재연결/방장 교체 지원 ----
    private List<Card> _order = new List<Card>(); // 셔플된 전체 덱 순서 (양쪽 클라이언트가 동일하게 보유)

    public bool HasOrder => _order.Count > 0;

    public void GetOrder(out int[] nums, out int[] colors)
    {
        nums = new int[_order.Count];
        colors = new int[_order.Count];
        for (int i = 0; i < _order.Count; i++) { nums[i] = _order[i].CardNumber; colors[i] = (int)_order[i].Color; }
    }

    /// <summary>
    /// 이미 뽑힌 카드(필드 + 양쪽 손패)를 제외하고 남은 덱을 같은 순서로 재구성합니다.
    /// 방장이 바뀌거나(새 방장이 뽑기 담당) 재연결로 상태를 복원할 때 사용합니다.
    /// </summary>
    public void RebuildRemaining(IEnumerable<Card> drawn)
    {
        var used = new HashSet<(int, ECardColor)>();
        foreach (var c in drawn) if (c != null) used.Add((c.CardNumber, c.Color));
        var remaining = new List<Card>();
        foreach (var c in _order) if (!used.Contains((c.CardNumber, c.Color))) remaining.Add(c);
        cards = new Stack<Card>(remaining);
        Debug.Log($"[CardDeck] 남은 덱 재구성: {cards.Count}장");
    }

    public void SetOrder(int[] nums, int[] colors)
    {
        _order = new List<Card>();
        for (int i = 0; i < nums.Length; i++) _order.Add(new Card(nums[i], (ECardColor)colors[i]));
    }

    
    public Card GetCard()
    {
        //return cards.Pop();
        Debug.Log($"cards.Count: {cards.Count}");
        if (cards.Count == 0)
        {
            Debug.Log("덱이 비었습니다."); // 덱 소진은 정상 진행이므로 경고가 아닌 로그
            return null;
        }

        Card card = cards.Pop();
        GameManager.Instance.RecordUsedCard(card); // 사용 기록
        return card;
    }
}
