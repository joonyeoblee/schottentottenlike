using System.Collections.Generic;
using UnityEngine;

public class DeckManager : Singleton<DeckManager>
{
    [Header("외부 연결")]
    [SerializeField] private CardDataLoader dataLoader;
    [SerializeField] private GameObject cardPrefab;

    private List<CardData> deck = new List<CardData>();

    private void Start()
    {
        LoadDeck();
    }

    private void LoadDeck()
    {
        deck = dataLoader.LoadCardDataList();
        Shuffle();
    }

    public void Shuffle()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            int rand = Random.Range(i, deck.Count);
            (deck[i], deck[rand]) = (deck[rand], deck[i]);
        }
    }

    public void Draw()
    {
        if (deck.Count == 0)
        {
            Debug.Log("덱이 비었습니다!");
            return;
        }

        var cardData = deck[0];
        deck.RemoveAt(0);

        SpawnCard(cardData);
    }

    public int Remaining => deck.Count;


    // 디버그용
    public void PrintDeck()
    {
        Debug.Log("덱 순서:");
        foreach (var card in deck)
        {
            Debug.Log(card.ID + " / " + card.ImageName);
        }
    }

    private void SpawnCard(CardData data)
    {
        GameObject card = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
        card.GetComponent<CardSon>().SetData(data);
    }
}
