using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

public class CardSlot : MonoBehaviourPunCallbacks
{
    private Card _card;
    public Sprite DefaultSprite;
    public Card Card => _card;
    private SpriteRenderer _cardSprite;
    public bool IsMine; // true면 내 카드, false면 상대 카드
    public BattleField BattleField;
    public UI_CardSet MySetAnimation;
    public EnemyHandDrawAnimation EnemySetAnimaion;
    public int Index;
    public bool IsOccupied => _card != null;
    public int RoundIndex { get; set; }

    private Sprite CardSprite;
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
                    EnemySetAnimaion.PlaySetAnimation(this.transform, CardSprite.texture, () =>
                    {
                        if (_cardSprite != null && CardSprite != null)
                        {
                            _cardSprite.color = Color.white;
                            _cardSprite.sprite = CardSprite;
                        }
                        StartCoroutine(EnemyDeckDraw());
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
    }


    /// <summary>
    /// 테스트용 연출을 위한 메소드입니다
    /// </summary>
    private IEnumerator EnemyDeckDraw()
    {
        yield return new WaitForSeconds(3f);
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
