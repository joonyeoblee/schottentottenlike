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

        if (_card == null)
        {
            Debug.LogError("CardSlot.Refresh: Card is null");
            return;
        }

        Addressables.LoadAssetAsync<Sprite>(_card.CardImageAddress).Completed += handle =>
        {
            // Addressables 로드 실패 체크
            if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"CardSlot.Refresh: Failed to load sprite for {_card.CardImageAddress}");
                return;
            }

            // SpriteRenderer null 체크
            if (_cardSprite == null)
            {
                Debug.LogError("CardSlot.Refresh: _cardSprite is null");
                return;
            }

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
                    
                    // MySetAnimation null 체크
                    if (MySetAnimation != null)
                    {
                        MySetAnimation.PlayAnimation(() =>
                        {
                            // 솔로: 네트워크 동기화 없이 바로 후처리
                            if (battleFieldAI != null)
                                battleFieldAI.OnMyCardPlaced(this);
                        });
                    }
                    else
                    {
                        Debug.LogWarning("CardSlot.Refresh: MySetAnimation is null");
                        // 애니메이션 없이 바로 후처리
                        if (battleFieldAI != null)
                            battleFieldAI.OnMyCardPlaced(this);
                    }
                }
                else
                {
                    if (_cardSprite != null && CardSprite != null)
                    {
                        _cardSprite.enabled = true;
                        _cardSprite.color = Color.white;
                        _cardSprite.sprite = CardSprite;
                    }

                    // EnemySetAnimaion 및 애니메이션 준비 상태 체크
                    if (EnemySetAnimaion != null && CardSprite != null && CardSprite.texture != null)
                    {
                        // 애니메이션을 실행하기 전 필수 요소들이 준비되어 있는지 확인
                        if (IsEnemyAnimationReady())
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

                                if (BattleField?.CardDeck != null)
                                {
                                    Card nextCard = BattleField.CardDeck.GetCard();
                                    if (nextCard != null)
                                    {
                                        StartCoroutine(EnemyDeckDraw());
                                    }
                                }
                            });
                        }
                        else
                        {
                            Debug.LogWarning("CardSlot.Refresh: EnemySetAnimation is not ready, skipping animation");
                            // 애니메이션을 건너뛰고 스프라이트만 설정
                            if (_cardSprite != null && CardSprite != null)
                            {
                                _cardSprite.color = Color.white;
                                _cardSprite.sprite = CardSprite;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("CardSlot.Refresh: EnemySetAnimaion or CardSprite.texture is null");
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
                        
                        // MySetAnimation null 체크
                        if (MySetAnimation != null)
                        {
                            MySetAnimation.PlayAnimation(() =>
                            {
                                if (BattleField?.photonView != null && _card != null)
                                {
                                    BattleField.photonView.RPC(nameof(BattleField.SetCard), RpcTarget.Others, RoundIndex, Index, _card.CardNumber, (int)_card.Color);
                                }
                            });
                        }
                        else
                        {
                            Debug.LogWarning("CardSlot.Refresh: MySetAnimation is null in multiplayer mode");
                        }
                    }
                    else
                    {
                        // EnemySetAnimaion 및 애니메이션 준비 상태 체크
                        if (EnemySetAnimaion != null && CardSprite != null && CardSprite.texture != null)
                        {
                            // 애니메이션을 실행하기 전 필수 요소들이 준비되어 있는지 확인
                            if (IsEnemyAnimationReady())
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
                            else
                            {
                                Debug.LogWarning("CardSlot.Refresh: EnemySetAnimation is not ready in multiplayer mode, skipping animation");
                                // 애니메이션을 건너뛰고 스프라이트만 설정
                                if (_cardSprite != null && CardSprite != null)
                                {
                                    _cardSprite.color = Color.white;
                                    _cardSprite.sprite = CardSprite;
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("CardSlot.Refresh: EnemySetAnimaion or CardSprite.texture is null in multiplayer mode");
                        }
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

    /// <summary>
    /// EnemySetAnimaion이 애니메이션을 실행할 수 있는 상태인지 확인합니다.
    /// </summary>
    private bool IsEnemyAnimationReady()
    {
        if (EnemySetAnimaion == null)
            return false;

        // EnemyHandDrawAnimation의 필수 요소들을 간접적으로 확인
        try
        {
            // 애니메이션 초기화를 시도해봅니다 (private 메서드이므로 reflection 없이는 직접 호출 불가)
            // 대신 public 필드들을 확인합니다
            
            // AnimationTransforms 인스턴스 확인
            var animTransforms = FindObjectOfType<AnimationTransforms>();
            if (animTransforms == null)
            {
                Debug.LogWarning("AnimationTransforms instance not found");
                return false;
            }

            // EnemyShowTransform이 있는지 확인
            if (animTransforms.EnemyShowTransform == null)
            {
                Debug.LogWarning("EnemyShowTransform not assigned");
                return false;
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error checking enemy animation readiness: {e.Message}");
            return false;
        }
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
