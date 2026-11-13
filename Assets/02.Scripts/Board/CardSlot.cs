using System.Collections;
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

    public Sprite CardSprite;
    private bool _initialized;

    private void Start()
    {
        Init();
    }

    // 슬롯이 비활성 상태에서 바로 채워지는 경우(Start 전)를 대비해 지연 초기화
    private void Init()
    {
        if (_initialized) return;
        _initialized = true;

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
        Init();
        _card = card;

        // 멀티: 내 카드는 즉시 상대에게 전송합니다.
        // (연출이 끝난 뒤 보내면 뒤이어 보내는 턴 전환 RPC가 먼저 도착해 양쪽 판정이 어긋날 수 있음)
        if (IsMine && BattleField_AI.Instance == null && PhotonNetwork.InRoom && BattleField != null)
        {
            BattleField.photonView.RPC(nameof(BattleField.SetCard), RpcTarget.Others, RoundIndex, Index, _card.CardNumber, (int)_card.Color);
        }

        Addressables.LoadAssetAsync<Sprite>(_card.CardImageAddress).Completed += handle =>
        {
            _cardSprite.color = Color.white;
            _cardSprite.sprite = handle.Result;
            CardSprite = handle.Result;

            // 콜라이더 비활성화
            var boxcollider = GetComponent<Collider2D>();
            if (boxcollider != null)
                boxcollider.enabled = false;

            // 로드가 끝나기 전에 슬롯이 비워졌다면(재시작 등) 무시
            if (_card == null)
            {
                Clear();
                return;
            }

            // --- 분기: 솔로/멀티 ---
            var battleFieldAI = BattleField_AI.Instance;
            bool isSolo = battleFieldAI != null;

            if (isSolo)
            {
                // 솔로플레이: 턴 진행은 BattleField_AI가 담당하므로 여기서는 연출만 재생합니다.
                // (예전에는 여기서도 OnMyCardPlaced를 호출해 드로우/턴 전환이 두 번 일어났음)
                if (IsMine)
                {
                    _cardSprite.color = Color.white;
                    _cardSprite.sprite = CardSprite;
                    if (MySetAnimation != null)
                        MySetAnimation.PlayAnimation(null);
                }
                else
                {
                    if (_cardSprite != null && CardSprite != null)
                    {
                        _cardSprite.enabled = true;
                        _cardSprite.color = Color.white;
                        _cardSprite.sprite = CardSprite;
                    }

                    if (EnemySetAnimaion != null)
                    {
                        EnemySetAnimaion.PlaySetAnimation(this.transform, CardSprite.texture, () =>
                        {
                            if (_cardSprite != null && CardSprite != null)
                            {
                                _cardSprite.color = Color.white;
                                _cardSprite.sprite = CardSprite;
                            }

                            if (!gameObject.activeInHierarchy)
                                gameObject.SetActive(true);

                            // AI의 실제 드로우는 BattleField_AI에서 처리하고, 여기서는 드로우 연출만 재생
                            if (battleFieldAI.CardDeck.cards.Count > 0 && isActiveAndEnabled)
                                StartCoroutine(EnemyDeckDraw());
                        });
                    }
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
                        if (MySetAnimation != null)
                            MySetAnimation.PlayAnimation(null);
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
    /// 테스트용 연출을 위한 메소드입니다. 추후 원한다면 다른 곳에 삽입하셔도 됩니다.
    /// </summary>
    private IEnumerator EnemyDeckDraw()
    {
        yield return new WaitForSeconds(1f);
        EnemySetAnimaion.EnemySetAnimation();

    }

    private static readonly Color EmptyColor = new Color(1f, 1f, 1f, 122f / 255f);
    private static readonly Color HighlightColor = new Color(1f, 0.92f, 0.55f, 1f);

    /// <summary>
    /// 빈 칸 강조 표시 (카드를 놓을 수 있는 칸). k: 0~1 깜빡임 강도
    /// </summary>
    public void SetHighlight(bool on, float k)
    {
        Init();
        if (_cardSprite == null || IsOccupied) return;
        _cardSprite.color = on ? Color.Lerp(EmptyColor, HighlightColor, k) : EmptyColor;
        transform.localScale = on ? Vector3.one * (1f + 0.04f * k) : Vector3.one;
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
