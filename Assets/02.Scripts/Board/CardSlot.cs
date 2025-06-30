using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
public class CardSlot : MonoBehaviourPunCallbacks
{
    private Card _card;
    public Card Card => _card;
    private SpriteRenderer _cardSprite;
    public bool IsMine; // true면 내 카드, false면 상대 카드
    public BattleField BattleField;
    private void Start()
    {
        _cardSprite = GetComponent<SpriteRenderer>();
        BattleField = GetComponentInParent<BattleField>();
        if (!IsMine)
        {
            transform.Rotate(Vector3.up * 180f); // 또는 2D 기준으로 180도 회전
        }
    }
    public bool IsOccupied => _card != null;

    public void Refresh(Card card)
    {
        _card = card;

        Addressables.LoadAssetAsync<Sprite>(_card.CardImageAddress).Completed += handle =>
        {
            _cardSprite.color = Color.white;
            _cardSprite.sprite = handle.Result;
        };

        // 내 카드일 때만 상대에게 알림
        if (IsMine && PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            BattleField.PhotonView.RPC("SetCard", RpcTarget.Others, 0, _card.CardNumber, (int)_card.Color);
        }
    }

    public void Clear()
    {
        _card = null;
    }

   

}
