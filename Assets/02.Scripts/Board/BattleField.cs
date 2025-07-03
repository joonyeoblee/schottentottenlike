using System.Collections;
using Photon.Pun;
using UnityEngine;
public class BattleField : Singleton<BattleField>
{
    public HandCardManager[] HandCardManagers; // 카드 각각 넣어야 함
    public RoundSlot[] Rounds;
    public PhotonView PhotonView;
    public CardDeck CardDeck;

    private void Start()
    {
        int i = 0;
        foreach (RoundSlot round in Rounds)
        {
            round.index = i++;

            int j = 0;
            foreach (CardSlot playerSlot in round.PlayerCardSlots)
            {
                playerSlot.IsMine = true;
                playerSlot.Index = j++;
                playerSlot.RoundIndex = round.index;
            }

            j = 0;
            foreach (CardSlot enemySlot in round.EnemyCardSlots)
            {
                enemySlot.IsMine = false;
                enemySlot.Index = j++;
                enemySlot.RoundIndex = round.index;
            }
        }

        foreach (HandCardManager handCardManager in HandCardManagers)
        {
            handCardManager.Index = i++;
            int j = 0;
            foreach (HandCardSlot playerHandCardSlot in handCardManager.HandCardSlots)
            {
                playerHandCardSlot.IsMine = true;
                playerHandCardSlot.Index = j++;
                playerHandCardSlot.HandCardIndex = handCardManager.Index;
            }
        }
    }

    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        StartCoroutine(GameStartSequence());
    }

    private IEnumerator GameStartSequence()
    {
        // 1. 셔플 시작
        CardDeck.StartDeckSuffle();

        // 2. 셔플 완료까지 대기 (이벤트로 처리됨)
        bool isShuffled = false;
        void OnShuffled() => isShuffled = true;
        CardDeck.OnCardSuffle += OnShuffled;

        // 대기
        yield return new WaitUntil(() => isShuffled);
        CardDeck.OnCardSuffle -= OnShuffled;

        Debug.Log("덱 셔플 완료됨, 카드 배분 시작");

        // 3. 셔플 후 애니메이션 대기 시간 (ex. 딜레이)
        yield return new WaitForSeconds(0.5f);

        // 4. 손패 배분
        yield return DealFirstTurnCardsCoroutine();

        Debug.Log("첫 손패 배분 완료, 게임 시작 준비됨");
    }

    private IEnumerator DealFirstTurnCardsCoroutine()
    {
        int i = 0;

        // 내 카드
        foreach (var slot in HandCardManagers[0].HandCardSlots)
        {
            var card = CardDeck.GetCard();
            slot.Refresh(i++, card, true); // 내 카드
            yield return new WaitForSeconds(0.2f); // 애니메이션을 위한 대기
        }

        // 상대 카드
        i = 0;
        foreach (var slot in HandCardManagers[1].HandCardSlots)
        {
            var card = CardDeck.GetCard();
            slot.Refresh(i++, card, false); // 상대 카드
            yield return new WaitForSeconds(0.2f); // 애니메이션을 위한 대기
        }
    }

    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        // 상대방 입장에서 Enemy 슬롯에 추가
        Rounds[roundIndex].EnemyCardSlots[slotIndex].Refresh(card);
    }

    // 덱 데이터 수신 RPC
    [PunRPC]
    public void RPC_SyncDeck(int[] nums, int[] colors)
    {
        CardDeck.SyncDeckFromData(nums, colors);
    }

}
