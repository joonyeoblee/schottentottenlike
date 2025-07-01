using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.XR;

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
    
    public Card Card;
    
    private void Start()
    {
        BattleField = GetComponentInParent<BattleField>();
        _cardSprite = GetComponent<SpriteRenderer>();
        if (HandCardManager == null)
        {
            HandCardManager = GetComponentInParent<HandCardManager>();
        }
    }

    public void Refresh(int handIndex, Card card, bool isMine)
    {
        HandCardIndex = handIndex;
        Card = card;

        if (card == null)
        {
            _cardSprite.sprite = null;
            return;
        }

        _cardSprite.color = Color.white;

        if (isMine)
        {
            Addressables.LoadAssetAsync<Sprite>(card.CardImageAddress).Completed += handle =>
            {
                _cardSprite.sprite = handle.Result;
            };
        }
        else
        {
            // 상대 카드용 뒷면 로딩
            Addressables.LoadAssetAsync<Sprite>("black").Completed += handle =>
            {
                _cardSprite.sprite = handle.Result;
            };
        }
    }


    public void Clear()
    {
        Card =  null;
        _cardSprite.sprite = null;
    }
}