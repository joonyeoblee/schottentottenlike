using System;
using System.Collections.Generic;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    public Stack<Card> cards = new Stack<Card>();
    public event Action OnCardSuffle; // 임시코드
    private void Start()
    {
        DeckSuffle();
    }

    private void DeckSuffle()
    {
        cards = GameManager.Instance.GetAllPossibleCards();
        OnCardSuffle?.Invoke();
    }

    public Card GetCard()
    {
        return cards.Pop();
    }
}
