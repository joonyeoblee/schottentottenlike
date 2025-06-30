using System.Collections.Generic;
using UnityEngine;

public class DebugCardLoader : MonoBehaviour
{
    [Header("CSV에서 카드 데이터를 로드하는 컴포넌트")]
    [SerializeField] private CardDataLoader _loader;

    [Header("생성할 카드 프리팹")]
    [SerializeField] private GameObject _cardPrefab;

    [Header("카드들을 배치할 시작 위치")]
    [SerializeField] private Vector2 startPosition = new Vector2(-6, 3);

    [Header("가로로 몇 개씩 배치할지")]
    [SerializeField] private int cardsPerRow = 9;

    [Header("간격")]
    [SerializeField] private float xSpacing = 100f;
    [SerializeField] private float ySpacing = 200f;

    void Start()
    {
        List<CardData> cards = _loader.LoadCardDataList();

        for (int i = 0; i < cards.Count; i++)
        {
            var cardData = cards[i];
            var cardGO = Instantiate(_cardPrefab);

            // 위치 계산
            int row = i / cardsPerRow;
            int col = i % cardsPerRow;

            cardGO.transform.position = new Vector3(
                startPosition.x + col * xSpacing,
                startPosition.y - row * ySpacing,
                0
            );

            // 카드 데이터 적용
            cardGO.GetComponent<CardSon>().SetData(cardData);
            Debug.Log($"불러올 어드레서블 주소: '{cardData.ImageName}'");
        }
    }
}