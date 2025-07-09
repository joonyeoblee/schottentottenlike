using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class BattleField_AI : Singleton<BattleField_AI>
{
    [Header("관리 매니저")]
    public HandCardManager[] HandCardManagers; // [0] = 내 카드, [1] = 상대 카드
    public CardDeck CardDeck;

    [Header("슬롯")]
    public RoundSlot[] Rounds;

    [Header("게임 상태")]
    public ETurn CurrentTurn;

    private bool _isShuffled;
    public static BattleField_AI Instance;

    // 현재 턴의 다음 턴을 반환하는 헬퍼 프로퍼티
    private ETurn GetNextTurn() => CurrentTurn == ETurn.Player1 ? ETurn.Player2 : ETurn.Player1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeRoundSlots();
        InitializeHandCardSlots();
        CardDeck.OnCardSuffle += OnShuffled;
        GameStart();

    }

    /// <summary>
    /// 게임 시작 또는 재시작 시 호출됩니다.
    /// </summary>
    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        ClearAllCardSlots();
        StartCoroutine(GameStartSequence());
    }

    /// <summary>
    /// 모든 카드 슬롯(라운드, 핸드)을 초기 상태로 되돌립니다.
    /// </summary>
    public void ClearAllCardSlots()
    {
        foreach (var round in Rounds)
        {
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

    private void InitializeHandCardSlots()
    {
        for (int i = 0; i < HandCardManagers.Length; i++)
        {
            HandCardManagers[i].Index = i;

            for (int j = 0; j < HandCardManagers[i].HandCardSlots.Length; j++)
            {
                var slot = HandCardManagers[i].HandCardSlots[j];
                slot.IsMine = (i == 0);
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
        yield return new WaitForSeconds(0.5f);

        // 선공 턴 랜덤 결정
        int rand = Random.Range(0, 2);
        SetTurn(rand);

        // 첫 패 분배
        DealFirstTurnCards();

        if (CurrentTurn == ETurn.Player2)
        {
            StartAITurn();
        }
    }

    private void OnShuffled()
    {
        _isShuffled = true;
    }

    private void DealFirstTurnCards()
    {
        int handSize = HandCardManagers[0].HandCardSlots.Length;

        // 플레이어/AI 패 분배
        for (int i = 0; i < handSize; i++)
        {
            var card1 = CardDeck.GetCard();
            HandCardManagers[0].HandCardSlots[i].Refresh(i, card1, true);
            if (HandCardManagers[0].HandCardSlots[i].MyCard?.Rend != null)
                HandCardManagers[0].HandCardSlots[i].MyCard.Rend.enabled = true;

            var card2 = CardDeck.GetCard();
            HandCardManagers[1].HandCardSlots[i].Refresh(i, card2, false);
        }

        PlayEnemyHandAnimation();

        Debug.Log("손패 배분 완료 (솔로플레이)");
    }

    /// <summary>
    /// 내 카드가 필드에 놓였을 때 호출됩니다.
    /// </summary>
    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        StartCoroutine(HandleCardPlacedSequence());
        JudgeAllRoundsWinner();
    }

    /// <summary>
    /// 카드 제출 후 처리 시퀀스
    /// </summary>
    private IEnumerator HandleCardPlacedSequence()
    {
        yield return new WaitForSeconds(0.05f);

        // 비어있는 내 손패에 카드 드로우
        RequestDrawCard();

        // 턴 변경
        SetTurn((int)GetNextTurn());

        // 다음 슬롯 활성화
        UpdateSlotActivation();

        // --- 추가: 턴이 AI로 바뀌면 AI 턴 시작 ---
        if (CurrentTurn == ETurn.Player2)
        {
            StartAITurn();
        }
    }


    private void RequestDrawCard()
    {
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            if (!HandCardManagers[0].HandCardSlots[i].HasCard)
            {
                var card = CardDeck.GetCard();
                if (card != null)
                {
                    HandCardManagers[0].HandCardSlots[i].Refresh(i, card, true);
                    if (HandCardManagers[0].HandCardSlots[i].MyCard?.Rend != null)
                        HandCardManagers[0].HandCardSlots[i].MyCard.Rend.enabled = true;
                }
                break;
            }
        }
    }

    private void UpdateSlotActivation()
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

    public void SetTurn(int turn)
    {
        CurrentTurn = (ETurn)turn;
        Debug.Log($"턴이 {CurrentTurn}으로 변경됨");
        // TODO: 현재 턴에 따라 UI 업데이트
    }

    /// <summary>
    /// 플레이어가 카드를 두는 UI에서 호출
    /// </summary>
    public void OnPlayerWantsToPlaceCard(int handIndex, int roundIndex, int slotIndex)
    {
        var handSlot = HandCardManagers[0].HandCardSlots[handIndex];
        var card = handSlot.Card;
        if (card == null) return;

        var fieldSlot = Rounds[roundIndex].PlayerCardSlots[slotIndex];
        if (fieldSlot.IsOccupied) return;

        fieldSlot.Refresh(card);
        handSlot.Clear();
        GameManager.Instance.RecordFirstPlayerOnStone(roundIndex, 1);
        OnMyCardPlaced(fieldSlot);
    }

    /// <summary>
    /// AI의 턴 처리
    /// </summary>
    public void StartAITurn()
    {
        StartCoroutine(AITurnRoutine());
    }

    private IEnumerator AITurnRoutine()
    {
        yield return new WaitForSeconds(0.7f);

        var handManager = HandCardManagers[1];
        var handSlots = handManager.HandCardSlots;
        var rounds = Rounds;

        int bestHandIdx = -1, bestRoundIdx = -1, bestSlotIdx = -1;
        HandRank bestRank = HandRank.CardSum;
        int bestSum = -1;

        for (int handIdx = 0; handIdx < handSlots.Length; handIdx++)
        {
            var handSlot = handSlots[handIdx];
            if (!handSlot.HasCard) continue;
            var card = handSlot.Card;

            for (int roundIdx = 0; roundIdx < rounds.Length; roundIdx++)
            {
                var enemySlots = rounds[roundIdx].EnemyCardSlots;
                for (int slotIdx = 0; slotIdx < enemySlots.Length; slotIdx++)
                {
                    var slot = enemySlots[slotIdx];
                    if (slot.IsOccupied) continue;

                    var temp = new List<Card>();
                    for (int i = 0; i < enemySlots.Length; i++)
                        if (enemySlots[i].IsOccupied && enemySlots[i].Card != null)
                            temp.Add(enemySlots[i].Card);
                    temp.Add(card);

                    var rank = EvaluateHand(temp);
                    int sum = temp.Sum(c => c.CardNumber);

                    if (rank > bestRank || (rank == bestRank && sum > bestSum))
                    {
                        bestHandIdx = handIdx;
                        bestRoundIdx = roundIdx;
                        bestSlotIdx = slotIdx;
                        bestRank = rank;
                        bestSum = sum;
                    }
                }
            }
        }

        if (bestHandIdx != -1 && bestRoundIdx != -1 && bestSlotIdx != -1)
        {
            var handSlot = handSlots[bestHandIdx];
            var card = handSlot.Card;
            var targetSlot = rounds[bestRoundIdx].EnemyCardSlots[bestSlotIdx];

            if (!targetSlot.gameObject.activeSelf)
                targetSlot.gameObject.SetActive(true);

            targetSlot.Refresh(card);
            handSlot.Clear();
            GameManager.Instance.RecordFirstPlayerOnStone(bestRoundIdx, 2);
            JudgeAllRoundsWinner();

            // AI 카드 드로우
            for (int i = 0; i < handManager.HandCardSlots.Length; i++)
            {
                var slot = handManager.HandCardSlots[i];
                if (!slot.HasCard)
                {
                    var newCard = CardDeck.GetCard();
                    if (newCard != null)
                    {
                        slot.Refresh(i, newCard, false);
                        slot.MyCard.Rend.enabled = true;
                    }
                    break;
                }
            }

            // 턴 전환
            SetTurn((int)ETurn.Player1);
        }
        else
        {
            Debug.Log("AI가 낼 카드가 없습니다.");
            SetTurn((int)ETurn.Player1);
        }
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

            Rounds[roundIndex].MoveStoneToOwner(judgeResult.Winner);

            string playerRank = judgeResult.Player1Rank.ToString();
            string enemyRank = judgeResult.Player2Rank.ToString();

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

    private HandRank EvaluateHand(List<Card> cards)
    {
        if (cards.Count < 1) return HandRank.CardSum;
        var numbers = cards.Select(c => c.CardNumber).OrderBy(n => n).ToArray();
        bool isFlush = cards.All(c => c.Color == cards[0].Color);
        bool isStraight = cards.Count == 3 && numbers[2] - numbers[0] == 2 && numbers.Distinct().Count() == 3;
        bool isThree = cards.GroupBy(c => c.CardNumber).Any(g => g.Count() == 3);

        if (isFlush && isStraight) return HandRank.StraightFlush;
        if (isThree) return HandRank.ThreeOfAKind;
        if (isFlush) return HandRank.Flush;
        if (isStraight) return HandRank.Straight;
        return HandRank.CardSum;
    }

    private enum HandRank
    {
        StraightFlush = 5,
        ThreeOfAKind = 4,
        Flush = 3,
        Straight = 2,
        CardSum = 1
    }

    /// <summary>
    /// 상대 핸드카드가 펼쳐지는 애니메이션을 재생합니다.
    /// </summary>
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

    //public void MoveStoneToOwner(int roundIndex, int owner)
    //{
    //    Rounds[roundIndex].MoveStoneToOwner(owner);
    //}
}
