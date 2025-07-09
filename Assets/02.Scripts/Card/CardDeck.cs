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
        Stack<Card> syncedDeck = new Stack<Card>();
        for (int i = nums.Length - 1; i >= 0; i--)
        {
            syncedDeck.Push(new Card(nums[i], (ECardColor)colors[i]));
        }

        cards = syncedDeck;
        OnCardSuffle?.Invoke();
    }

    
    public Card GetCard()
    {
        //return cards.Pop();
        Debug.Log($"cards.Count: {cards.Count}");
        if (cards.Count == 0)
        {
            Debug.LogWarning("더 이상 나올 카드가 없습니다!");
            return null;
        }

        Card card = cards.Pop();
        GameManager.Instance.RecordUsedCard(card); // 사용 기록
        return card;
    }
}
