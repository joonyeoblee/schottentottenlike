using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

public class CardSlot : MonoBehaviourPunCallbacks
{
    private Card _card;
    public Card Card => _card;
    private SpriteRenderer _cardSprite;
    public bool IsMine; // true면 내 카드, false면 상대 카드
    public BattleField BattleField;
    public UI_CardSet MySetAnimation;
    public EnemyHandDrawAnimation EnemySetAnimaion;
    public int Index;
    public bool IsOccupied => _card != null;
    public int RoundIndex { get; set; }
    private void Start()
    {
        MySetAnimation = GetComponent<UI_CardSet>();
        _cardSprite = GetComponent<SpriteRenderer>();
        BattleField = GetComponentInParent<BattleField>();
        if (!IsMine)
        {
            transform.Rotate(Vector3.up * 180f); // 또는 2D 기준으로 180도 회전
        }
    }


    public void Refresh(Card card)
    {
        _card = card;

        Addressables.LoadAssetAsync<Sprite>(_card.CardImageAddress).Completed += handle =>
        {

            _cardSprite.color = Color.white;
            _cardSprite.sprite = handle.Result;

        };

        // 내 카드일 때만 상대에게 알림

        if(PhotonNetwork.IsConnected)//포톤 네트워크에 연결이 돼 있다면

        {
            if (IsMine) //내 카드 애니메이션 셋
            {
                MySetAnimation.PlayAnimation(() =>
                {
                    BattleField.photonView.RPC(nameof(BattleField.SetCard), RpcTarget.Others, RoundIndex, Index, _card.CardNumber, (int)_card.Color);
                });
            }
            else //내 카드를 셋하는 게 아니라면
            {
                EnemySetAnimaion.PlayDrawAnimation(this.transform);
            }

        }
    }

    public void Clear()
    {
        _card = null;
    }

}
