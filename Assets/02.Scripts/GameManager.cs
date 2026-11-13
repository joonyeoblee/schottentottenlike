using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 족보 판정
public enum HandRank
{
    StraightFlush = 5,
    ThreeOfAKind = 4,
    Flush = 3,
    Straight = 2,
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
    private int[] _firstPlayerOnStone;

    private List<Card> _usedCards = new List<Card>();

    public static event Action<int> OnGameOver; // int: 승자(1,2), 0: 무승부

    // 게임 종료 여부 (한 판에 한 번만 OnGameOver를 발생시키기 위함)
    public bool IsGameOver { get; private set; }

    public void RecordUsedCard(Card card)
    {
        _usedCards.Add(card);
    }

    /// <summary>
    /// 새 판을 시작할 때 호출합니다. GameManager는 DontDestroyOnLoad라서
    /// 이전 판의 점령 정보가 남아 있으면 재시작 직후 바로 승패가 나버립니다.
    /// </summary>
    public void ResetGame()
    {
        InitializeGame();
        _usedCards.Clear();
        IsGameOver = false;
    }

    /// <summary>재연결 동기화용: 경계석별 '먼저 3장을 완성한 플레이어' 기록 (로컬 시점 1/2, 0=없음)</summary>
    public int[] GetCompletionOrder() => (int[])_firstPlayerOnStone.Clone();

    /// <summary>
    /// 재연결 시 판 상태 복원. owners/firsts는 로컬 시점(1=나, 2=상대)이어야 합니다.
    /// </summary>
    public void RestoreState(int[] owners, int[] firsts, bool gameOver)
    {
        InitializeGame();
        for (int i = 0; i < _stoneCount && i < owners.Length; i++)
        {
            RoundOwners[i] = owners[i];
            _firstPlayerOnStone[i] = i < firsts.Length ? firsts[i] : 0;
        }
        IsGameOver = gameOver;
    }

    /// <summary>
    /// 해당 경계석이 이미 점령되었는지 여부. 점령된 경계석에는 더 이상 카드를 낼 수 없습니다.
    /// </summary>
    public bool IsStoneClaimed(int stoneIndex)
    {
        return RoundOwners != null && stoneIndex >= 0 && stoneIndex < RoundOwners.Length && RoundOwners[stoneIndex] != 0;
    }

    /// <summary>
    /// 양쪽 모두 더 이상 낼 수 없을 때 호출합니다. 점령한 경계석이 많은 쪽이 승리합니다.
    /// </summary>
    public void EndByStalemate()
    {
        if (IsGameOver) return;
        int p1Count = RoundOwners.Count(x => x == 1);
        int p2Count = RoundOwners.Count(x => x == 2);
        int winner = p1Count > p2Count ? 1 : p2Count > p1Count ? 2 : 0;
        Debug.Log($"더 이상 낼 카드가 없어 게임 종료 (P1 {p1Count} : P2 {p2Count})");
        FinishGame(winner);
    }

    private void FinishGame(int winner)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        OnGameOver?.Invoke(winner);
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
        _firstPlayerOnStone = new int[_stoneCount];
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
    public JudgeResult JudgeStoneWithRank(List<Card> playerCards, List<Card> enemyCards, List<Card> unusedCards = null, int stoneIndex = -1)
    {
        var result = new JudgeResult();
        result.Player1Rank = EvaluateHand(playerCards);
        result.Player2Rank = EvaluateHand(enemyCards);


        if (playerCards.Count == 3 && enemyCards.Count == 3)
        {
            int winner = CompareHands(playerCards, enemyCards);
            // 족보와 합이 모두 같으면 먼저 3장을 완성한 쪽이 승리
            if (winner == 0 && stoneIndex >= 0)
                winner = _firstPlayerOnStone[stoneIndex];
            result.Winner = winner;
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
        // winner: 1(플레이어1), 2(플레이어2), 0(무승부/미정)
        // 한 번 점령된 경계석은 소유자가 바뀌지 않습니다.
        if (RoundOwners[roundIndex] == 0 && (winner == 1 || winner == 2))
            RoundOwners[roundIndex] = winner;

        CheckGameWinCondition();
    }

    public void CheckGameWinCondition()
    {
        if (IsGameOver) return;

        int p1Count = RoundOwners.Count(x => x == 1);
        int p2Count = RoundOwners.Count(x => x == 2);

        // 5개 이상 소유
        if (p1Count >= 5)
        {
            Debug.Log("플레이어1이 5개 라운드 점령! 게임 승리");
            FinishGame(1);
            return;
        }
        if (p2Count >= 5)
        {
            Debug.Log("플레이어2가 5개 라운드 점령! 게임 승리");
            FinishGame(2);
            return;
        }

        // 연속 3개 소유
        for (int i = 0; i <= RoundOwners.Length - 3; i++)
        {
            if (RoundOwners[i] == 1 && RoundOwners[i + 1] == 1 && RoundOwners[i + 2] == 1)
            {
                Debug.Log("플레이어1이 연속 3개 라운드 점령! 게임 승리");
                FinishGame(1);
                return;
            }
            if (RoundOwners[i] == 2 && RoundOwners[i + 1] == 2 && RoundOwners[i + 2] == 2)
            {
                Debug.Log("플레이어2가 연속 3개 라운드 점령! 게임 승리");
                FinishGame(2);
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



    // 3장 족보 판정 (3장 미만이면 CardSum 취급)
    public static HandRank EvaluateHand(List<Card> cards)
    {
        if (cards == null || cards.Count < 3) return HandRank.CardSum;

        int x = cards[0].CardNumber, y = cards[1].CardNumber, z = cards[2].CardNumber;
        if (x > y) { int t = x; x = y; y = t; }
        if (y > z) { int t = y; y = z; z = t; }
        if (x > y) { int t = x; x = y; y = t; }

        bool isFlush = cards[0].Color == cards[1].Color && cards[1].Color == cards[2].Color;
        bool isStraight = y == x + 1 && z == y + 1;
        bool isThree = x == z;

        if (isFlush && isStraight) return HandRank.StraightFlush;
        if (isThree) return HandRank.ThreeOfAKind;
        if (isFlush) return HandRank.Flush;
        if (isStraight) return HandRank.Straight;

        return HandRank.CardSum;
    }

    /// <summary>
    /// 족보 → 합 순으로 비교합니다. (1: hand1 승, 2: hand2 승, 0: 완전히 동일)
    /// 족보가 같으면 숫자 합이 큰 쪽이 이기고, 합까지 같으면 먼저 완성한 쪽이 이깁니다(호출부에서 처리).
    /// </summary>
    public static int CompareHands(List<Card> hand1, List<Card> hand2)
    {
        var rank1 = EvaluateHand(hand1);
        var rank2 = EvaluateHand(hand2);

        if (rank1 > rank2) return 1;
        if (rank2 > rank1) return 2;

        int sum1 = 0, sum2 = 0;
        for (int i = 0; i < hand1.Count; i++) sum1 += hand1[i].CardNumber;
        for (int i = 0; i < hand2.Count; i++) sum2 += hand2[i].CardNumber;
        if (sum1 > sum2) return 1;
        if (sum2 > sum1) return 2;
        return 0;
    }

    /// <summary>
    /// 상대가 남은 카드(필드에 나오지 않은 카드)로 무엇을 채우더라도 내 3장을 이길 수 없으면 true.
    /// 동점이면 이미 3장을 먼저 완성한 내가 이기므로 상대가 '엄격하게' 이겨야만 false 입니다.
    /// </summary>
    public static bool IsProvenWin(List<Card> myCards, List<Card> opponentCards, List<Card> unusedCards)
    {
        int needed = 3 - opponentCards.Count;
        if (needed < 0) return false;
        if (unusedCards.Count < needed) return true; // 상대는 이 경계석을 완성할 수조차 없음

        // 할당을 줄이기 위해 하나의 리스트를 재사용하며 모든 조합을 검사
        var fullOpp = new List<Card>(3);
        fullOpp.AddRange(opponentCards);
        int n = unusedCards.Count;

        if (needed == 0)
            return CompareHands(fullOpp, myCards) != 1;

        for (int i = 0; i < n; i++)
        {
            fullOpp.Add(unusedCards[i]);
            if (needed == 1)
            {
                if (CompareHands(fullOpp, myCards) == 1) return false;
            }
            else
            {
                for (int j = i + 1; j < n; j++)
                {
                    fullOpp.Add(unusedCards[j]);
                    if (needed == 2)
                    {
                        if (CompareHands(fullOpp, myCards) == 1) return false;
                    }
                    else
                    {
                        for (int k = j + 1; k < n; k++)
                        {
                            fullOpp.Add(unusedCards[k]);
                            bool oppWins = CompareHands(fullOpp, myCards) == 1;
                            fullOpp.RemoveAt(fullOpp.Count - 1);
                            if (oppWins) return false;
                        }
                    }
                    fullOpp.RemoveAt(fullOpp.Count - 1);
                }
            }
            fullOpp.RemoveAt(fullOpp.Count - 1);
        }
        return true;
    }

    public static IEnumerable<List<Card>> GetCombinations(List<Card> list, int count)
    {
        return GetCombinations(list, 0, count);
    }

    private static IEnumerable<List<Card>> GetCombinations(List<Card> list, int start, int count)
    {
        if (count == 0)
        {
            yield return new List<Card>();
            yield break;
        }

        for (int i = start; i <= list.Count - count; i++)
        {
            var head = list[i];
            foreach (var tail in GetCombinations(list, i + 1, count - 1))
            {
                tail.Insert(0, head);
                yield return tail;
            }
        }
    }

    /// <summary>
    /// 해당 경계석에서 3장을 먼저 완성한 플레이어를 기록합니다. (족보·합 동점 시 승자 결정용)
    /// 카드가 놓일 때마다 호출해도 되며, 3장이 된 순간에만 기록됩니다.
    /// </summary>
    public void RecordStoneCompleted(int stoneIndex, int player, int cardCountOnSide)
    {
        if (cardCountOnSide >= 3 && _firstPlayerOnStone[stoneIndex] == 0)
            _firstPlayerOnStone[stoneIndex] = player;
    }

}
