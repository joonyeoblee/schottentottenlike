using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class HandCardSlot : MonoBehaviourPunCallbacks
{
    // 카드 각각에 들어가야 하는 코드
    // 리프레시 포함시킴
    public bool IsMine;
    public BattleField BattleField;
    public HandCardManager HandCardManager;
    public int Index;
    public int HandCardIndex { get;  set; }
    private SpriteRenderer _cardSprite;
    public Sprite DefaultCardSprite;
    public Card Card;

    //UI로 쓰일 Card 이미지
    public UI_Cards MyCard;

    private void Start()
    {
        BattleField = GetComponentInParent<BattleField>();
        _cardSprite = GetComponent<SpriteRenderer>();
        if (HandCardManager == null)
        {
            HandCardManager = GetComponentInParent<HandCardManager>();
        }

        if (MyCard == null)
        {
            MyCard = GetComponentInChildren<UI_Cards>();
        }
    }

    public void Refresh(int handIndex, Card card, bool isVisible)
    {
        HandCardIndex = handIndex;
        Card = card;

        if (_cardSprite == null)
            return;

        _cardSprite.sprite = null;
        _cardSprite.color = Color.white;

        string address = isVisible ? card.CardImageAddress : "Black";

        Addressables.LoadAssetAsync<Sprite>(address).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                MyCard.frontTexture = handle.Result.texture;
                MyCard.ApplyTextures();

            }
        };
    }



    public void Clear()
    {
        Card =  null;
        _cardSprite.sprite = DefaultCardSprite;
    }
}
