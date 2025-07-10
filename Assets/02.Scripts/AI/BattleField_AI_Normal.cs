using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 휴리스틱 기반 상위 난이도 AI
/// </summary>
public class BattleField_AI_Normal : BattleField_AI
{
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        base.Start();
    }

    /// <summary>
    /// 휴리스틱 기반 AI 턴 루틴
    /// </summary>
    protected override IEnumerator AITurnRoutine()
    {
        yield return new WaitForSeconds(0.7f);

        var handManager = HandCardManagers[1];
        var handSlots = handManager.HandCardSlots;
        var rounds = Rounds;

        int bestHandIdx = -1, bestRoundIdx = -1, bestSlotIdx = -1;
        int bestScore = int.MinValue;
        string bestLog = "";

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

                    int score = EvaluateHeuristic(handIdx, roundIdx, slotIdx, out string reasonLog);

                    // 로그: 각 후보의 판단 근거
                    Debug.Log($"[AI 후보] handIdx:{handIdx}, roundIdx:{roundIdx}, slotIdx:{slotIdx}, 카드:{card.CardNumber}-{card.Color}, 점수:{score}\n{reasonLog}");

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestHandIdx = handIdx;
                        bestRoundIdx = roundIdx;
                        bestSlotIdx = slotIdx;
                        bestLog = $"[AI 선택] handIdx:{handIdx}, roundIdx:{roundIdx}, slotIdx:{slotIdx}, 카드:{card.CardNumber}-{card.Color}, 점수:{score}\n{reasonLog}";
                    }
                }
            }
        }

        if (bestHandIdx != -1 && bestRoundIdx != -1 && bestSlotIdx != -1)
        {
            Debug.Log(bestLog);

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
                        if (slot.MyCard?.Rend != null)
                            slot.MyCard.Rend.enabled = true;
                    }
                    break;
                }
            }

            SetTurn((int)ETurn.Player1);
        }
        else
        {
            Debug.Log("AI가 낼 카드가 없습니다.");
            SetTurn((int)ETurn.Player1);
        }
    }

    /// <summary>
    /// 휴리스틱 평가 함수 (판단 근거 로그 포함, 손패 전체 전략적 가치 반영)
    /// </summary>
    private int EvaluateHeuristic(int handIdx, int roundIdx, int slotIdx, out string reasonLog)
    {
        var handManager = HandCardManagers[1];
        var handSlot = handManager.HandCardSlots[handIdx];
        var card = handSlot.Card;

        var round = Rounds[roundIdx];
        var enemySlots = round.EnemyCardSlots;
        var temp = new List<Card>();
        for (int i = 0; i < enemySlots.Length; i++)
            if (enemySlots[i].IsOccupied && enemySlots[i].Card != null)
                temp.Add(enemySlots[i].Card);
        temp.Add(card);

        int score = 0;
        var rank = EvaluateHand(temp);
        int sum = temp.Sum(c => c.CardNumber);

        string log = $" - 조합: [{string.Join(", ", temp.Select(c => $"{c.CardNumber}-{c.Color}"))}]\n";
        log += $" - 족보: {rank}, 합: {sum}\n";

        // 1. 족보별 가중치 (핵심)
        int rankScore = 0;
        if (temp.Count == 3)
        {
            switch (rank)
            {
                case HandRank.StraightFlush: rankScore = 8000; break;
                case HandRank.ThreeOfAKind: rankScore = 4000; break;
                case HandRank.Flush: rankScore = 1800; break;
                case HandRank.Straight: rankScore = 900; break;
                case HandRank.CardSum: rankScore = sum * 10; break;
            }
        }
        else
        {
            // 3장이 안되면 카드합만 반영
            rankScore = sum * 2; // 예시: 2장일 때는 합에만 소폭 가중치
        }
        score += rankScore;
        log += $" - 족보 가중치: {rankScore}\n";

        // 2. 이미 내가 낸 카드 수 가중치 (라운드 집중)
        int myCardCount = enemySlots.Count(s => s.IsOccupied);
        int myCardScore = myCardCount * 80;
        score += myCardScore;
        log += $" - 내 카드 수({myCardCount}) 가중치: {myCardScore}\n";

        // 3. 플레이어가 아직 카드를 내지 않은 라운드 가중치
        var playerSlots = round.PlayerCardSlots;
        int playerCardCount = playerSlots.Count(s => s.IsOccupied);
        int playerScore = (playerCardCount == 0) ? 120 : 0;
        score += playerScore;
        log += $" - 플레이어 카드 없음 가중치: {playerScore}\n";

        // 4. 손패 관리 전략
        var handCards = GetHandCards(1);
        Card usedCard = null;
        if (handIdx >= 0 && handIdx < handCards.Count)
        {
            usedCard = handCards[handIdx];
            handCards.RemoveAt(handIdx);
        }
        var combos = EvaluateHandCombinations(handCards);

        int maxSum = int.MinValue;
        HandRank bestRank = HandRank.CardSum;
        bool canMakeStrongHand = false;
        foreach (var (cards, r, s) in combos)
        {
            if (s > maxSum) maxSum = s;
            if (r > bestRank) bestRank = r;
            if (r >= HandRank.Flush) canMakeStrongHand = true;
        }

        int handManageScore = 0;
        // 족보 완성 가능성 평가
        var (canThreeOfAKind, canFlush, canStraight) = EvaluatePotential(handCards);

        int lostPotential = 0;
        if (usedCard != null)
        {
            // 트리플 가능성 상실
            if (canThreeOfAKind && !handCards.Any(c => c.CardNumber == usedCard.CardNumber))
                lostPotential -= 200;
            // 플러시 가능성 상실
            if (canFlush && !handCards.Any(c => c.Color == usedCard.Color))
                lostPotential -= 200;
            // 스트레이트 가능성 상실
            var numbers = handCards.Select(c => c.CardNumber).Distinct().OrderBy(n => n).ToArray();
            bool straightPossible = false;
            for (int i = 0; i < numbers.Length - 1; i++)
            {
                if (Mathf.Abs(numbers[i + 1] - numbers[i]) == 1)
                {
                    straightPossible = true;
                    break;
                }
            }
            if (canStraight && !straightPossible)
                lostPotential -= 200;

            // 약한 카드 소진 보상
            if (usedCard.CardNumber <= 5)
                lostPotential += 120;
        }

        handManageScore += lostPotential;
        if (lostPotential < 0)
            log += $" - 손패 관리: 족보 완성 가능성 상실, 감점({lostPotential})\n";
        else if (lostPotential > 0)
            log += $" - 손패 관리: 약한 카드 소진, 가산({lostPotential})\n";

        score += handManageScore;

        // 5. 라운드별 승리/패배 상황 인식
        int roundSituationScore = 0;
        int myCount = myCardCount;
        int playerCount = playerCardCount;

        if (myCount == 2 && playerCount <= 1)
        {
            if (usedCard != null && usedCard.CardNumber >= 8)
            {
                roundSituationScore -= 350;
                log += $" - 라운드 상황: 이미 유리한 라운드에 강한 카드 사용, 감점(-350)\n";
            }
            else if (usedCard != null && usedCard.CardNumber <= 5)
            {
                roundSituationScore += 120;
                log += $" - 라운드 상황: 이미 유리한 라운드에 약한 카드 사용, 가산(+120)\n";
            }
        }
        else if (playerCount == 2 && myCount <= 1)
        {
            if (usedCard != null && usedCard.CardNumber >= 8)
            {
                roundSituationScore -= 350;
                log += $" - 라운드 상황: 이미 불리한 라운드에 강한 카드 사용, 감점(-350)\n";
            }
            else if (usedCard != null && usedCard.CardNumber <= 5)
            {
                roundSituationScore += 120;
                log += $" - 라운드 상황: 이미 불리한 라운드에 약한 카드 사용, 가산(+120)\n";
            }
        }
        else if (myCount == 1 && playerCount == 1)
        {
            if (usedCard != null && usedCard.CardNumber >= 8)
            {
                roundSituationScore += 250;
                log += $" - 라운드 상황: 경쟁 라운드에 강한 카드 사용, 가산(+250)\n";
            }
        }
        score += roundSituationScore;

        // 6. 플레이어 행동 예측
        int playerPredictScore = 0;
        bool playerHasStrongCard = false;
        foreach (var playerSlot in round.PlayerCardSlots)
        {
            if (playerSlot.IsOccupied && playerSlot.Card != null && playerSlot.Card.CardNumber >= 8)
            {
                playerHasStrongCard = true;
                break;
            }
        }
        if (playerHasStrongCard && usedCard != null && usedCard.CardNumber >= 8)
        {
            playerPredictScore -= 250;
            log += $" - 플레이어 예측: 플레이어가 강한 카드 낸 라운드에 강한 카드 사용, 감점(-250)\n";
        }
        if (playerCount == 0 && usedCard != null && usedCard.CardNumber >= 8)
        {
            playerPredictScore -= 180;
            log += $" - 플레이어 예측: 플레이어가 아직 카드를 내지 않은 라운드에 강한 카드 사용, 감점(-180)\n";
        }
        bool playerOnlyWeak = playerCount > 0 && round.PlayerCardSlots.All(s => !s.IsOccupied || (s.Card != null && s.Card.CardNumber <= 5));
        if (playerOnlyWeak && usedCard != null && usedCard.CardNumber >= 8)
        {
            playerPredictScore += 180;
            log += $" - 플레이어 예측: 플레이어가 약한 카드만 낸 라운드에 강한 카드 사용, 가산(+180)\n";
        }
        score += playerPredictScore;

        // 7. 게임 전체 승리 조건 (최우선)
        int gameWinScore = 0;
        int[] roundOwners = (int[])GameManager.Instance.RoundOwners.Clone();
        roundOwners[roundIdx] = 2; // 이 수로 해당 라운드를 AI가 점령한다고 가정

        int consecutive = 0;
        bool hasThreeConsecutive = false;
        for (int i = 0; i < roundOwners.Length; i++)
        {
            if (roundOwners[i] == 2)
            {
                consecutive++;
                if (consecutive >= 3)
                {
                    hasThreeConsecutive = true;
                    break;
                }
            }
            else
            {
                consecutive = 0;
            }
        }
        int totalOwned = roundOwners.Count(o => o == 2);
        if (hasThreeConsecutive || totalOwned >= 5)
        {
            gameWinScore += 100000; // 즉시 승리 가능성이 있으면 매우 큰 가중치
            log += $" - 게임 승리 조건: 이 수로 승리 확정! 가중치(+100000)\n";
        }
        else if (consecutive == 2 || totalOwned == 4)
        {
            gameWinScore += 2000;
            log += $" - 게임 승리 조건: 승리 직전, 가중치(+2000)\n";
        }
        score += gameWinScore;

        // 8. 남은 손패 전략가치 (최대합, 족보)
        int handPotentialScore = 0;
        if (bestRank >= HandRank.Flush) handPotentialScore += 300;
        handPotentialScore += maxSum * 2;
        score += handPotentialScore;
        log += $" - 남은 손패 전략가치 가중치: {handPotentialScore}\n";

        reasonLog = log;
        return score;
    }

    /// <summary>
    /// 남은 손패에서 족보(트리플, 플러시, 스트레이트) 완성 가능성이 있는지 평가
    /// </summary>
    private (bool canThreeOfAKind, bool canFlush, bool canStraight) EvaluatePotential(List<Card> handCards)
    {
        bool canThreeOfAKind = false;
        bool canFlush = false;
        bool canStraight = false;

        // 3장 미만이면 불가
        if (handCards.Count < 3)
            return (false, false, false);

        // 숫자별, 색상별 그룹핑
        var numberGroups = handCards.GroupBy(c => c.CardNumber);
        var colorGroups = handCards.GroupBy(c => c.Color);

        // 트리플 가능성: 같은 숫자가 2장 이상
        canThreeOfAKind = numberGroups.Any(g => g.Count() >= 2);

        // 플러시 가능성: 같은 색이 2장 이상
        canFlush = colorGroups.Any(g => g.Count() >= 2);

        // 스트레이트 가능성: 숫자 정렬 후, 2장 이상 연속 숫자 존재
        var numbers = handCards.Select(c => c.CardNumber).Distinct().OrderBy(n => n).ToArray();
        for (int i = 0; i < numbers.Length - 1; i++)
        {
            if (numbers[i + 1] - numbers[i] == 1)
            {
                canStraight = true;
                break;
            }
        }

        return (canThreeOfAKind, canFlush, canStraight);
    }




    /// <summary>
    /// 족보 평가 함수 (BattleField_AI와 동일)
    /// </summary>
    private HandRank EvaluateHand(List<Card> cards)
    {
        if (cards.Count < 1)
            return HandRank.CardSum;

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
}
