using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;

public class HandCardSlot : MonoBehaviourPunCallbacks
{
    // 카드 각각에 들어가야 하는 코드
    // 리프레시 포함시킴
    public bool IsMine;
    public BattleField BattleField;
    public HandCardManager HandCardManager;
    public int Index;
    public int HandCardIndex { get;  set; }
    public Sprite DefaultCardSprite;
    public Card Card;

    //UI로 쓰일 Card 이미지
    public UI_Cards MyCard;
    public bool IsEmpty;
    private void Start()
    {
        IsEmpty = true;
        BattleField = GetComponentInParent<BattleField>();
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

        // if ( CardSprite == null)
        //     return;

        MyCard.Rend.sprite = null;
        MyCard.Rend.color = Color.white;

        string address = isVisible ? card.CardImageAddress : "Black";

        Addressables.LoadAssetAsync<Sprite>(address).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                MyCard.frontTexture = handle.Result.texture;
                MyCard.ApplyTextures();
                IsEmpty = false;

            }
        };
    }



    public void Clear()
    {
        Card =  null;
        MyCard.Rend.enabled = false;
        IsEmpty = true;

    }
}
