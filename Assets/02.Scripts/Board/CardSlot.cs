using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
public class CardSlot : MonoBehaviour
{
    private Card _card;
    public Card Card => _card;
    public Sprite DefaultSprite;
    private SpriteRenderer _cardSprite;
    public bool IsMine; // true면 내 카드, false면 상대 카드
    public BattleField BattleField;
    public UI_CardSet MySetAnimation;
    public EnemyHandDrawAnimation EnemySetAnimaion;
    public UI_CardVerification CardVerification;
    public int Index;
    public bool IsOccupied => _card != null;
    public int RoundIndex { get; set; }
    private void Start()
    {
        _cardSprite = GetComponent<SpriteRenderer>();
        BattleField = GetComponentInParent<BattleField>();
        CardVerification = GetComponent<UI_CardVerification>();
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
            CardSprite = handle.Result;

            // 콜라이더 비활성화
            var boxcollider = GetComponent<Collider2D>();
            if (boxcollider != null)
                boxcollider.enabled = false;

            // --- 분기: 솔로/멀티 ---
            var battleFieldAI = FindObjectOfType<BattleField_AI>();
            bool isSolo = battleFieldAI != null;

            if (isSolo)
            {
                // 솔로플레이
                if (IsMine)
                {
                    _cardSprite.color = Color.white;
                    _cardSprite.sprite = CardSprite;
                    MySetAnimation.PlayAnimation(() =>
                    {
                        // 솔로: 네트워크 동기화 없이 바로 후처리
                        battleFieldAI.OnMyCardPlaced(this);
                    });
                }
                else
                {
                    if (_cardSprite != null && CardSprite != null)
                    {
                        _cardSprite.enabled = true;
                        _cardSprite.color = Color.white;
                        _cardSprite.sprite = CardSprite;
                    }

                    EnemySetAnimaion.PlaySetAnimation(this.transform, CardSprite.texture, () =>
                    {
                        if (_cardSprite != null && CardSprite != null)
                        {
                            _cardSprite.color = Color.white;
                            _cardSprite.sprite = CardSprite;
                        }

                        if (!gameObject.activeInHierarchy)
                            gameObject.SetActive(true);


                        Card card = BattleField.CardDeck.GetCard();

                        if (card != null)
                        {
                            StartCoroutine(EnemyDeckDraw());
                        }

                        photonView.RPC(nameof(BattleField.RPC_Judging),RpcTarget.All,RoundIndex);

                    });
                }
            }
            else
            {
                // 멀티플레이(Photon)
                if (PhotonNetwork.IsConnected)
                {
                    if (IsMine)
                    {
                        _cardSprite.color = Color.white;
                        _cardSprite.sprite = CardSprite;
                        MySetAnimation.PlayAnimation(() =>
                        {
                            BattleField.photonView.RPC(nameof(BattleField.SetCard), RpcTarget.Others, RoundIndex, Index, _card.CardNumber, (int)_card.Color);
                            BattleField.OnMyCardPlaced(this);
                        });
                    }
                    else
                    {
                        EnemySetAnimaion.PlaySetAnimation(this.transform, CardSprite.texture, () =>
                        {
                            _cardSprite.color = Color.white;
                            _cardSprite.sprite = CardSprite;
                            StartCoroutine(EnemyDeckDraw());
                        });
                    }
                }
            }
        };

        // 콜라이더 비활성화
        var boxcollider = GetComponent<Collider2D>();
        if (boxcollider != null)
            boxcollider.enabled = false;

        if (IsMine && PhotonNetwork.IsConnected)
        {
            BattleField.photonView.RPC(nameof(BattleField.SetCard), RpcTarget.Others, RoundIndex, Index, _card.CardNumber, (int)_card.Color);
            BattleField.OnMyCardPlaced(this);
        }

    /// <summary>
    /// 테스트용 연출을 위한 메소드입니다. 추후 원한다면 다른 곳에 삽입하셔도 됩니다.
    /// </summary>
    private IEnumerator EnemyDeckDraw()
    {
        yield return new WaitForSeconds(1f);
        EnemySetAnimaion.EnemySetAnimation();

    }

    public void Clear()
    {
        _card = null;

        if (_cardSprite != null)
        {
            _cardSprite.sprite = DefaultSprite;
            _cardSprite.color = new Color(1f, 1f, 1f, 122f / 255f);
        }

        var boxcollider = GetComponent<Collider2D>();
        if (boxcollider != null)
            boxcollider.enabled = true;
    }

}
