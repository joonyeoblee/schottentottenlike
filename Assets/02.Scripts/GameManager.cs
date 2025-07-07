using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 족보 판정
public enum HandRank
{
    StraightFlush = 5,
    ThreeOfAKind = 4,
    Straight = 3,
    Flush = 2,
    CardSum = 1
}

public struct JudgeResult
{
    public int Winner; // 1: 플레이어1, 2: 플레이어2, 0: 무승부/미정
    public HandRank Player1Rank;
    public HandRank Player2Rank;
}


public class GameManager : Singleton<GameManager>
{
    [Header("쇼텐토텐 룰")]
    [SerializeField]
    private int _stoneCount = 9;

    private List<Stack<Card>> _player1Stones;
    private List<Stack<Card>> _player2Stones;
    private Stack<Card> _deck;
    //private int[] RoundOwners;
    public int[] RoundOwners; // 0: 무소유, 1: 플레이어1, 2: 플레이어2

    private List<Card> _usedCards = new List<Card>();

    public void RecordUsedCard(Card card)
    {
        _usedCards.Add(card);
    }


    protected override void Awake()
    {
        base.Awake();
        InitializeGame();
    }

    protected override void Start()
    {
        base.Start();
        Application.targetFrameRate = 60;
    }

    private void InitializeGame()
    {
        _player1Stones = new List<Stack<Card>>();
        _player2Stones = new List<Stack<Card>>();
        for (int i = 0; i < _stoneCount; i++)
        {
            _player1Stones.Add(new Stack<Card>());
            _player2Stones.Add(new Stack<Card>());
        }
        _deck = GetAllPossibleCards();
        RoundOwners = new int[_stoneCount];
    }

    // 경계석 점령 판정
    public int JudgeStone(int stoneIndex)
    {
        var p1 = _player1Stones[stoneIndex].ToList();
        var p2 = _player2Stones[stoneIndex].ToList();

        if (p1.Count == 3 && p2.Count == 3)
        {
            return CompareHands(p1, p2);
        }
        else if (p1.Count == 3 && p2.Count < 3)
        {
            var unused = GetUnusedCards().ToList();
            return IsProvenWin(p1, p2, unused) ? 1 : 0;
        }
        else if (p2.Count == 3 && p1.Count < 3)
        {
            var unused = GetUnusedCards().ToList();
            return IsProvenWin(p2, p1, unused) ? 2 : 0;
        }
        return 0;
    }

    // 새로 추가: Rounds의 카드 리스트를 직접 받아서 판정
    public int JudgeStone(List<Card> playerCards, List<Card> enemyCards, List<Card> unusedCards = null)
    {
        // unusedCards는 미사용 카드(확정승 판정용), 필요시 BattleField에서 전달
        if (playerCards.Count == 3 && enemyCards.Count == 3)
        {
            return CompareHands(playerCards, enemyCards);
        }
        else if (playerCards.Count == 3 && enemyCards.Count < 3)
        {
            if (unusedCards == null)
                return 0;

            return IsProvenWin(playerCards, enemyCards, unusedCards) ? 1 : 0;
        }
        else if (enemyCards.Count == 3 && playerCards.Count < 3)
        {
            if (unusedCards == null)
                return 0;

            return IsProvenWin(enemyCards, playerCards, unusedCards) ? 2 : 0;
        }
        return 0;
    }

    //족보 반환
    public JudgeResult JudgeStoneWithRank(List<Card> playerCards, List<Card> enemyCards, List<Card> unusedCards = null)
    {
        var result = new JudgeResult();
        result.Player1Rank = EvaluateHand(playerCards);
        result.Player2Rank = EvaluateHand(enemyCards);

        if (playerCards.Count == 3 && enemyCards.Count == 3)
        {
            result.Winner = CompareHands(playerCards, enemyCards);
        }
        else if (playerCards.Count == 3 && enemyCards.Count < 3)
        {
            if (unusedCards == null) result.Winner = 0;
            else result.Winner = IsProvenWin(playerCards, enemyCards, unusedCards) ? 1 : 0;
        }
        else if (enemyCards.Count == 3 && playerCards.Count < 3)
        {
            if (unusedCards == null) result.Winner = 0;
            else result.Winner = IsProvenWin(enemyCards, playerCards, unusedCards) ? 2 : 0;
        }
        else
        {
            result.Winner = 0;
        }
        return result;
    }

    public void UpdateRoundOwnerAndCheckWin(int roundIndex, int winner)
    {
        // winner: 1(플레이어1), -1(플레이어2), 0(무승부/미정)
        if (winner == 1)
            RoundOwners[roundIndex] = 1;
        else if (winner == 2)
            RoundOwners[roundIndex] = 2;
        else
            RoundOwners[roundIndex] = 0;

        CheckGameWinCondition();
    }

    public void CheckGameWinCondition()
    {
        int p1Count = RoundOwners.Count(x => x == 1);
        int p2Count = RoundOwners.Count(x => x == 2);

        // 5개 이상 소유
        if (p1Count >= 5)
        {
            Debug.Log("플레이어1이 5개 라운드 점령! 게임 승리");
            // 게임 종료 처리
            return;
        }
        if (p2Count >= 5)
        {
            Debug.Log("플레이어2가 5개 라운드 점령! 게임 승리");
            // 게임 종료 처리
            return;
        }

        // 연속 3개 소유
        for (int i = 0; i <= RoundOwners.Length - 3; i++)
        {
            if (RoundOwners[i] == 1 && RoundOwners[i + 1] == 1 && RoundOwners[i + 2] == 1)
            {
                Debug.Log("플레이어1이 연속 3개 라운드 점령! 게임 승리");
                // 게임 종료 처리
                return;
            }
            if (RoundOwners[i] == 2 && RoundOwners[i + 1] == 2 && RoundOwners[i + 2] == 2)
            {
                Debug.Log("플레이어2가 연속 3개 라운드 점령! 게임 승리");
                // 게임 종료 처리
                return;
            }
        }
    }

    // 전체 미사용 카드 반환 (Stack 버전)
    public Stack<Card> GetUnusedCards()
    {
        List<Card> all = GetAllPossibleCards().ToList();
        List<Card> unused = all.Where(c => !_usedCards.Any(u => u.CardNumber == c.CardNumber && u.Color == c.Color)).ToList();
        return new Stack<Card>(unused);
    }


    // 전체 카드 생성 (색상 6종, 숫자 1~9)
    public Stack<Card> GetAllPossibleCards()
    {
        Stack<Card> all = new Stack<Card>();
        foreach (ECardColor color in Enum.GetValues(typeof(ECardColor)))
        {
            for (int num = 1; num <= 9; num++)
            {
                Card card = new Card(num, color);
                all.Push(card);
            }
        }
        return all;
    }

    // 카드 동등성 비교
    private static bool ContainsCard(IEnumerable<Card> list, Card card)
    {
        return list.Any(c => c.CardNumber == card.CardNumber && c.Color == card.Color);
    }



    private HandRank EvaluateHand(List<Card> cards)
    {
        var numbers = cards.Select(c => c.CardNumber).OrderBy(n => n).ToArray();
        bool isFlush = cards.All(c => c.Color == cards[0].Color);
        bool isStraight = numbers.Length == 3 && numbers[2] - numbers[0] == 2 && numbers.Distinct().Count() == 3;
        bool isThree = cards.GroupBy(c => c.CardNumber).Any(g => g.Count() == 3);
        bool isPair = cards.GroupBy(c => c.CardNumber).Any(g => g.Count() == 2);

        if (isFlush && isStraight) return HandRank.StraightFlush;
        if (isThree) return HandRank.ThreeOfAKind;
        if (isStraight) return HandRank.Straight;
        if (isFlush) return HandRank.Flush;

        return HandRank.CardSum;
    }

    private int CompareHands(List<Card> hand1, List<Card> hand2)
    {
        var rank1 = EvaluateHand(hand1);
        var rank2 = EvaluateHand(hand2);

        if (rank1 > rank2) return 1;
        if (rank2 > rank1) return 2;

        if (rank1 == HandRank.CardSum)
        {
            int sum1 = hand1.Sum(c => c.CardNumber);
            int sum2 = hand2.Sum(c => c.CardNumber);
            if (sum1 > sum2) return 1;
            if (sum2 > sum1) return 2;
            return 0;
        }
        else
        {
            var sorted1 = hand1.Select(c => c.CardNumber).OrderByDescending(n => n).ToArray();
            var sorted2 = hand2.Select(c => c.CardNumber).OrderByDescending(n => n).ToArray();
            for (int i = 0; i < 3; i++)
            {
                if (sorted1[i] > sorted2[i]) return 1;
                if (sorted2[i] > sorted1[i]) return 2;
            }
            return 0;
        }
    }

    private bool IsProvenWin(List<Card> myCards, List<Card> opponentCards, List<Card> unusedCards)
    {
        int needed = 3 - opponentCards.Count;
        var possibleHands = GetCombinations(unusedCards, needed);

        foreach (var oppAdd in possibleHands)
        {
            var fullOpp = new List<Card>(opponentCards);
            fullOpp.AddRange(oppAdd);

            var myRank = EvaluateHand(myCards);
            var oppRank = EvaluateHand(fullOpp);

            if (oppRank > myRank)
                return false;
            if (oppRank == myRank)
            {
                if (myRank == HandRank.CardSum)
                {
                    int mySum = myCards.Sum(c => c.CardNumber);
                    int oppSum = fullOpp.Sum(c => c.CardNumber);
                    if (oppSum > mySum)
                        return false;
                }
                else
                {
                    var myNums = myCards.Select(c => c.CardNumber).OrderByDescending(n => n).ToArray();
                    var oppNums = fullOpp.Select(c => c.CardNumber).OrderByDescending(n => n).ToArray();
                    for (int i = 0; i < 3; i++)
                    {
                        if (oppNums[i] > myNums[i])
                            return false;
                        if (myNums[i] > oppNums[i])
                            break;
                    }
                }
            }
        }
        return true;
    }

    private IEnumerable<List<Card>> GetCombinations(List<Card> list, int count)
    {
        if (count == 0)
        {
            yield return new List<Card>();
            yield break;
        }

        for (int i = 0; i < list.Count; i++)
        {
            var head = list[i];
            var rest = list.Skip(i + 1).ToList();
            foreach (var tail in GetCombinations(rest, count - 1))
            {
                tail.Insert(0, head);
                yield return tail;
            }
        }
    }


}
