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
        if (!PhotonNetwork.IsMasterClient)
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

        // 덱을 int[]로 변환
        int[] nums = new int[cardList.Count];
        int[] colors = new int[cardList.Count];

        for (int i = 0; i < cardList.Count; i++)
        {
            nums[i] = cardList[i].CardNumber;
            colors[i] = (int)cardList[i].Color;
        }

        // 동기화 RPC 호출
        BattleField.Instance.photonView.RPC(nameof(BattleField.RPC_SyncDeck), RpcTarget.Others, nums, colors);

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
        return cards.Pop();
    }
}
