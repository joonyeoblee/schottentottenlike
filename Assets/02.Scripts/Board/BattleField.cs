using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
// Action을 사용하기 위해 추가

// 턴을 구분하기 위한 Enum
public enum ETurn
{
    Player1 = 0,
    Player2 = 1
}

public class BattleField : SingletonPhoton<BattleField>
{
    [Header("관리 매니저")]
    public HandCardManager[] HandCardManagers; // [0] = 내 카드, [1] = 상대 카드
    public CardDeck CardDeck;

    [Header("슬롯")]
    public RoundSlot[] Rounds;

    [Header("게임 상태")]
    public ETurn CurrentTurn;

    private bool _isShuffled;

    [Header("애니메이션을 위한 참조")]
    public EnemyHandDrawAnimation EnemyHandDrawAnimation;
    private int _judgeRound;
    public RoundDicator RoundDicator;

    public int MyplayerNumber { get; private set; }
    public int EnemyPlayerNumber => MyplayerNumber == 1 ? 2 : 1;


    // 현재 턴의 다음 턴을 반환하는 헬퍼 프로퍼티
    private ETurn GetNextTurn() => CurrentTurn == ETurn.Player1 ? ETurn.Player2 : ETurn.Player1;



    private void Start()
    {
        InitializeRoundSlots();
        InitializeHandCardSlots();
        MyplayerNumber = PhotonNetwork.IsMasterClient ? 1 : 2;
        CardDeck.OnCardSuffle += OnShuffled;
    }

    /// <summary>
    /// 게임 시작 또는 재시작 시 호출됩니다.
    /// </summary>
    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        ClearAllCardSlots(); // 모든 슬롯을 깨끗하게 초기화
        StartCoroutine(GameStartSequence());
        RoundDicator.Turn();

    }

    /// <summary>
    /// 모든 카드 슬롯(라운드, 핸드)을 초기 상태로 되돌립니다.
    /// </summary>
    public void ClearAllCardSlots()
    {
        foreach (var round in Rounds)
        {
            // 각 라운드의 첫 번째 슬롯만 활성화하고 나머지는 비활성화 및 클리어
            for (int i = 0; i < round.PlayerCardSlots.Length; i++)
            {
                var slot = round.PlayerCardSlots[i];
                slot.Clear();
                slot.gameObject.SetActive(i == 0);
            }
            for (int i = 0; i < round.EnemyCardSlots.Length; i++)
            {
                var slot = round.EnemyCardSlots[i];
                slot.Clear();
                slot.gameObject.SetActive(i == 0);
            }
        }

        foreach (var handManager in HandCardManagers)
        {
            foreach (var handSlot in handManager.HandCardSlots)
            {
                handSlot.Clear();
            }
        }
        Debug.Log("모든 슬롯 초기화 완료: 각 라운드의 0번째 슬롯만 활성화됨");
    }

    /// <summary>
    /// 라운드 슬롯의 인덱스 및 소유권 정보를 설정합니다.
    /// </summary>
    private void InitializeRoundSlots()
    {
        for (int i = 0; i < Rounds.Length; i++)
        {
            Rounds[i].Index = i;

            for (int j = 0; j < Rounds[i].PlayerCardSlots.Length; j++)
            {
                Rounds[i].PlayerCardSlots[j].IsMine = true;
                Rounds[i].PlayerCardSlots[j].Index = j;
                Rounds[i].PlayerCardSlots[j].RoundIndex = i;
                Rounds[i].PlayerCardSlots[j].Clear();
            }

            for (int j = 0; j < Rounds[i].EnemyCardSlots.Length; j++)
            {
                Rounds[i].EnemyCardSlots[j].IsMine = false;
                Rounds[i].EnemyCardSlots[j].Index = j;
                Rounds[i].EnemyCardSlots[j].RoundIndex = i;
                Rounds[i].EnemyCardSlots[j].Clear();
            }
        }
    }

    /// <summary>
    /// 핸드 카드 슬롯의 인덱스 정보를 설정합니다.
    /// </summary>
    private void InitializeHandCardSlots()
    {
        for (int i = 0; i < HandCardManagers.Length; i++)
        {
            HandCardManagers[i].Index = i;

            for (int j = 0; j < HandCardManagers[i].HandCardSlots.Length; j++)
            {
                var slot = HandCardManagers[i].HandCardSlots[j];
                slot.IsMine = (i == 0); // 0번 매니저가 내 것이라고 가정
                slot.Index = j;
                slot.HandCardIndex = i;

                if (slot.MyCard != null && slot.MyCard.Rend != null)
                {
                    slot.MyCard.Rend.enabled = false;
                }
            }
        }
    }

    private IEnumerator GameStartSequence()
    {
        _isShuffled = false;
        CardDeck.StartDeckSuffle();
        yield return new WaitUntil(() => _isShuffled);
        yield return new WaitForSeconds(0.5f); // 셔플 후 딜레이

        if (PhotonNetwork.IsMasterClient)
        {
            // 선공 턴 랜덤 결정 (V2 로직)
            int rand = Random.Range(0, 2);
            photonView.RPC(nameof(SetTurn), RpcTarget.All, rand);
            SendFirstTurnDealToAll();
        }
    }

    private void OnShuffled()
    {
        _isShuffled = true;
    }

    private void SendFirstTurnDealToAll()
    {
        int handSize = HandCardManagers[0].HandCardSlots.Length;

        int[] masterColors = new int[handSize];
        int[] masterNumbers = new int[handSize];
        int[] clientColors = new int[handSize];
        int[] clientNumbers = new int[handSize];

        for (int i = 0; i < handSize; i++)
        {
            Card card1 = CardDeck.GetCard();
            masterColors[i] = (int)card1.Color;
            masterNumbers[i] = card1.CardNumber;

            Card card2 = CardDeck.GetCard();
            clientColors[i] = (int)card2.Color;
            clientNumbers[i] = card2.CardNumber;
        }

        photonView.RPC(nameof(ReceiveFirstTurnCards), RpcTarget.MasterClient,
            masterColors, masterNumbers, clientColors, clientNumbers);

        photonView.RPC(nameof(ReceiveFirstTurnCards), RpcTarget.Others,
            clientColors, clientNumbers, masterColors, masterNumbers);
    }

    [PunRPC]
    private void ReceiveFirstTurnCards(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        StartCoroutine(DealFirstTurnCardsCoroutine(myColors, myNumbers, enemyColors, enemyNumbers));
    }

    private IEnumerator DealFirstTurnCardsCoroutine(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            Card card = new Card(myNumbers[i], (ECardColor)myColors[i]);
            HandCardManagers[0].HandCardSlots[i].Refresh(i, card, true);
            DrawMyCardAnimation(i);
            yield return new WaitForSeconds(0.2f);
        }

        for (int i = 0; i < HandCardManagers[1].HandCardSlots.Length; i++)
        {
            Card card = new Card(enemyNumbers[i], (ECardColor)enemyColors[i]);
            HandCardManagers[1].HandCardSlots[i].Refresh(i, card, false);
            yield return new WaitForSeconds(0.2f);
        }

        PlayEnemyHandAnimation();
        Debug.Log("손패 배분 완료 (로컬 기준)");
    }

    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        StartCoroutine(HandleCardPlacedSequence());
        GameManager.Instance.RecordFirstPlayerOnStone(placedSlot.RoundIndex, MyplayerNumber);
        JudgeAllRoundsWinner();
    }

    private IEnumerator HandleCardPlacedSequence()
    {
        yield return new WaitForSeconds(0.05f);
        RequestDrawCard();
        ETurn nextTurn = GetNextTurn();
        photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)nextTurn);
        photonView.RPC(nameof(RPC_UpdateSlotActivation), RpcTarget.All);
    }

    private void RequestDrawCard()
    {
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            if (!HandCardManagers[0].HandCardSlots[i].HasCard)
            {
                photonView.RPC(nameof(RPC_RequestDrawCardFromMaster), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, i);
                break;
            }
        }
    }




    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        var slot = Rounds[roundIndex].EnemyCardSlots[slotIndex];

        if (!slot.gameObject.activeSelf)
            slot.gameObject.SetActive(true);

        slot.Refresh(card);
        // 상대 카드가 놓인 후에도 승패 판정
        GameManager.Instance.RecordFirstPlayerOnStone(roundIndex, EnemyPlayerNumber);
        JudgeAllRoundsWinner();

    }


    [PunRPC]
    public void RPC_Judging(int roundIndex)
    {
        JudgeRoundWinner(roundIndex);
    }

    private void JudgeRoundWinner(int roundIndex)
    {
        var round = Rounds[roundIndex];
        var uiRound = round.UI_Round;

        var playerCards = GetCardsFromSlots(round.PlayerCardSlots);
        var enemyCards = GetCardsFromSlots(round.EnemyCardSlots);
        var unusedCards = GetUnusedCardsFromField();

        var judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards);

        GameManager.Instance.UpdateRoundOwnerAndCheckWin(roundIndex, judgeResult.Winner);

        uiRound.MyResult = judgeResult.Player1Rank.ToString();
        uiRound.EnemyResult = judgeResult.Player2Rank.ToString();

        RoundWinner winner;
        switch (judgeResult.Winner)
        {
            case 1: // 플레이어1 승리
                winner = RoundWinner.Player;
                break;
            case -1: // 플레이어2 승리
                winner = RoundWinner.Enemy;
                break;
            default: // 무승부
                Debug.Log($"라운드 {roundIndex} 판정: 무승부 ({uiRound.MyResult} vs {uiRound.EnemyResult})");
                return; // 함수를 즉시 종료하여 애니메이션을 막음
        }



        // 승패가 결정된 경우에만 애니메이션 실행
        uiRound.PlayVerificationAnimation(round, winner, () =>
        {
            string winnerMessage;
            if (winner == RoundWinner.Player)
            {
                winnerMessage = $"플레이어1 승리 ({uiRound.MyResult} vs {uiRound.EnemyResult})";
            }
            else
            {
                winnerMessage = $"플레이어2 승리 ({uiRound.MyResult} vs {uiRound.EnemyResult})";
            }
            Debug.Log($"[애니메이션 종료] 라운드 {_judgeRound} 최종 판정: {winnerMessage}");
        });
    }
    private void JudgeAllRoundsWinner()
    {
        for (int roundIndex = 0; roundIndex < Rounds.Length; roundIndex++)
        {
            List<Card> playerCards = GetPlayerCards(roundIndex);
            List<Card> enemyCards = GetEnemyCards(roundIndex);
            List<Card> unusedCards = GetUnusedCardsFromField();

            JudgeResult judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards, roundIndex);

            GameManager.Instance.UpdateRoundOwnerAndCheckWin(roundIndex, judgeResult.Winner);

            string playerRank = judgeResult.Player1Rank.ToString();
            string enemyRank = judgeResult.Player2Rank.ToString();

            if (judgeResult.Winner == 1 || judgeResult.Winner == 2)
            {
                int owner = judgeResult.Winner == 1 ? 1 : 2;
                photonView.RPC(nameof(RPC_MoveStone), RpcTarget.All, roundIndex, owner);
            }

            if (judgeResult.Winner == 1)
            {
                Debug.Log($"라운드 {roundIndex}: 플레이어1 승리 ({playerRank} vs {enemyRank})");
            }
            else if (judgeResult.Winner == 2)
            {
                Debug.Log($"라운드 {roundIndex}: 플레이어2 승리 ({playerRank} vs {enemyRank})");
            }
            else
            {
                Debug.Log($"라운드 {roundIndex}: 무승부 또는 미정 ({playerRank} vs {enemyRank})");
            }
        }
    }

    private List<Card> GetPlayerCards(int roundIndex)
    {
        CardSlot[] slots = Rounds[roundIndex].PlayerCardSlots;
        List<Card> cards = new List<Card>();
        foreach (CardSlot slot in slots)
            if (slot.IsOccupied && slot.Card != null)
                cards.Add(slot.Card);
        return cards;
    }

    private List<Card> GetEnemyCards(int roundIndex)
    {
        CardSlot[] slots = Rounds[roundIndex].EnemyCardSlots;
        List<Card> cards = new List<Card>();
        foreach (CardSlot slot in slots)
            if (slot.IsOccupied && slot.Card != null)
                cards.Add(slot.Card);
        return cards;
    }

    private List<Card> GetUnusedCardsFromField()
    {
        List<Card> used = new List<Card>();
        foreach (RoundSlot round in Rounds)
        {
            used.AddRange(round.PlayerCardSlots.Where(s => s.IsOccupied && s.Card != null).Select(s => s.Card));
            used.AddRange(round.EnemyCardSlots.Where(s => s.IsOccupied && s.Card != null).Select(s => s.Card));
        }

        List<Card> all = GameManager.Instance.GetAllPossibleCards().ToList();
        return all.Where(c => !used.Any(u => u.CardNumber == c.CardNumber && u.Color == c.Color)).ToList();
    }

    private List<Card> GetCardsFromSlots(CardSlot[] slots)
    {
        return slots.Where(s => s.IsOccupied && s.Card != null).Select(s => s.Card).ToList();
    }

    private void DrawMyCardAnimation(int handSlotIndex)
    {
        var slot = HandCardManagers[0].HandCardSlots[handSlotIndex];
        if (slot.MyCard == null) return;

        HandCardManagers[0].HandCardSlots[handSlotIndex].MyCard.ShowAnimation.midPoint =
        AnimationTransforms.Instance.FirstShowTransfroms[handSlotIndex];
        HandCardManagers[0].HandCardSlots[handSlotIndex].MyCard.ShowDraw();
        slot.MyCard.Rend.enabled = true;
    }

    private void DrawEnemyCardAnimation()
    {
        EnemyHandDrawAnimation.EnemySetAnimation();
    }

    private void PlayEnemyHandAnimation()
    {
        var enemyAnimation = HandCardManagers[1].GetComponent<EnemyHandAnimation>();
        if (enemyAnimation == null) return;

        foreach (var enemyCardSlot in HandCardManagers[1].HandCardSlots)
        {
            if (enemyCardSlot?.MyCard?.Rend != null)
            {
                enemyCardSlot.MyCard.Rend.enabled = true;
            }
        }
        enemyAnimation.PlayFanInAnimation();
    }

    [PunRPC]
    public void SetTurn(int turn)
    {
        CurrentTurn = (ETurn)turn;
        Debug.Log($"턴이 {CurrentTurn}으로 변경됨");
        RoundDicator.Turn();

    }

    [PunRPC]
    private void RPC_RequestDrawCardFromMaster(int targetActorNumber, int slotIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Card card = CardDeck.GetCard();
        if (card == null)
        {
            Debug.LogWarning("덱에서 더 이상 뽑을 카드가 없습니다.");
            return;
        }

        photonView.RPC(nameof(RPC_ReceiveDrawCard), RpcTarget.All, targetActorNumber, slotIndex, card.CardNumber, (int)card.Color);


    }

    [PunRPC]
    private void RPC_ReceiveDrawCard(int targetActorNumber, int slotIndex, int cardNumber, int color)
    {
        bool isMine = PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber;
        int handManagerIndex = isMine ? 0 : 1;

        Card card = new Card(cardNumber, (ECardColor)color);
        var slot = HandCardManagers[handManagerIndex].HandCardSlots[slotIndex];
        slot.Refresh(slotIndex, card, isMine);

        if (isMine)
        {
            DrawMyCardAnimation(slotIndex);
        }
        else
        {
            if(slot.MyCard?.Rend != null)
            {
                slot.MyCard.Rend.enabled = true;
            }

        }
    }


    [PunRPC]
    private void RPC_UpdateSlotActivation()
    {
        foreach (var round in Rounds)
        {
            for (int i = 0; i < round.PlayerCardSlots.Length - 1; i++)
            {
                var current = round.PlayerCardSlots[i];
                var next = round.PlayerCardSlots[i + 1];

                if (current.IsOccupied && !next.gameObject.activeSelf)
                {
                    next.gameObject.SetActive(true);
                }
            }
        }
    }

    [PunRPC]
    public void RPC_SyncDeck(int[] nums, int[] colors)
    {
        CardDeck.SyncDeckFromData(nums, colors);
    }
    [PunRPC]
    public void RPC_MoveStone(int roundIndex, int owner)
    {
        Rounds[roundIndex].MoveStoneToOwner(owner);
    }

}
