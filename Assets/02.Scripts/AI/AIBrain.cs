using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 솔로 모드 AI의 수 선택 로직.
/// 각 (손패 카드, 경계석) 조합에 대해 "완성 가능한 족보의 기대 강도"와
/// 상대 진영과의 비교, 경계석 위치(연속 3개 노림/방어)를 점수화하여 가장 좋은 수를 고릅니다.
/// AI는 자신의 손패와 필드만 볼 수 있고, 플레이어의 손패는 모르는 카드로 취급합니다.
/// </summary>
public static class AIBrain
{
    public struct Move
    {
        public int HandIndex;
        public int RoundIndex;
        public float Score;
    }

    public class BoardView
    {
        public List<Card>[] AiCards;      // 경계석별 AI 쪽 카드
        public List<Card>[] PlayerCards;  // 경계석별 플레이어 쪽 카드
        public int[] Owners;              // 0: 미점령, 1: 플레이어, 2: AI
        public List<Card> Hand;           // AI 손패 (null 가능 = 빈 슬롯)
        public List<Card> Unseen;         // AI 입장에서 아직 보지 못한 카드 (덱 + 플레이어 손패)
        public List<Card> FieldUnused;    // 필드에 나오지 않은 모든 카드 (확정승 판정용)
        public int SlotsPerSide = 3;
    }

    // 3장 강도: 족보 * 100 + 합
    public static int Strength(Card a, Card b, Card c)
    {
        int x = a.CardNumber, y = b.CardNumber, z = c.CardNumber;
        // 정렬
        if (x > y) { int t = x; x = y; y = t; }
        if (y > z) { int t = y; y = z; z = t; }
        if (x > y) { int t = x; x = y; y = t; }

        bool flush = a.Color == b.Color && b.Color == c.Color;
        bool straight = y == x + 1 && z == y + 1;
        bool three = x == z;

        int rank;
        if (flush && straight) rank = 5;
        else if (three) rank = 4;
        else if (flush) rank = 3;
        else if (straight) rank = 2;
        else rank = 1;
        return rank * 100 + x + y + z;
    }

    private static readonly System.Random SharedRng = new System.Random();

    /// <summary>가장 강한 수 (어려움 난이도와 동일)</summary>
    public static Move? ChooseMove(BoardView board) => ChooseMove(board, AIDifficulty.Hard, SharedRng);

    /// <summary>
    /// 난이도별 수 선택
    /// - 어려움: 위협(상대의 연속 3개/5개 직전)까지 고려한 점수로 항상 최선의 수
    /// - 보통: 기본 점수 상위 3개 중에서 확률적으로 선택 (60% / 25% / 15%)
    /// - 쉬움: 30%는 아무 수나, 나머지는 상위 5개 중 무작위
    /// </summary>
    public static Move? ChooseMove(BoardView board, AIDifficulty difficulty, System.Random rng)
    {
        bool threatAware = difficulty == AIDifficulty.Hard;
        var moves = new List<Move>();

        for (int h = 0; h < board.Hand.Count; h++)
        {
            var card = board.Hand[h];
            if (card == null) continue;

            for (int r = 0; r < board.AiCards.Length; r++)
            {
                if (board.Owners[r] != 0) continue;
                if (board.AiCards[r].Count >= board.SlotsPerSide) continue;

                float score = ScorePlacement(board, h, r, threatAware);
                score += (float)rng.NextDouble() * 0.5f; // 동점일 때 약간의 무작위성
                moves.Add(new Move { HandIndex = h, RoundIndex = r, Score = score });
            }
        }
        if (moves.Count == 0) return null;
        moves.Sort((a, b) => b.Score.CompareTo(a.Score));

        switch (difficulty)
        {
            case AIDifficulty.Easy:
                if (rng.NextDouble() < 0.3) return moves[rng.Next(moves.Count)];
                return moves[rng.Next(Math.Min(5, moves.Count))];

            case AIDifficulty.Normal:
            {
                double roll = rng.NextDouble();
                int pick = roll < 0.6 ? 0 : roll < 0.85 ? 1 : 2;
                return moves[Math.Min(pick, moves.Count - 1)];
            }

            default:
                return moves[0];
        }
    }

    /// <summary>
    /// 이 경계석의 '중요도'. 0 = 평범, 1 = 승부처.
    /// 상대가 이 칸 하나로 이웃한 3개 / 5개를 완성할 수 있거나, 내가 이 칸으로 완성할 수 있으면 승부처입니다.
    /// (Owners: 1 = 상대(플레이어), 2 = 나(AI))
    /// </summary>
    private static float Criticality(int[] owners, int round)
    {
        float crit = 0;
        int oppTotal = 0, myTotal = 0;
        foreach (var o in owners) { if (o == 1) oppTotal++; else if (o == 2) myTotal++; }
        if (oppTotal >= 4 || myTotal >= 4) crit = Math.Max(crit, 0.8f);

        for (int start = round - 2; start <= round; start++)
        {
            if (start < 0 || start + 2 >= owners.Length) continue;
            int opp = 0, mine = 0;
            for (int i = start; i < start + 3; i++) { if (owners[i] == 1) opp++; else if (owners[i] == 2) mine++; }
            if (opp == 2 && mine == 0) crit = 1f;               // 막지 않으면 짐
            else if (mine == 2 && opp == 0) crit = Math.Max(crit, 0.9f); // 이기면 끝
        }
        return crit;
    }

    private static float ScorePlacement(BoardView board, int handIndex, int round, bool threatAware)
    {
        var card = board.Hand[handIndex];
        var mine = new List<Card>(board.AiCards[round]) { card };
        var opp = board.PlayerCards[round];

        // AI가 앞으로 쓸 수 있는 카드 = 아직 못 본 카드 + 이번에 내지 않는 손패
        var pool = new List<Card>(board.Unseen);
        for (int i = 0; i < board.Hand.Count; i++)
            if (i != handIndex && board.Hand[i] != null) pool.Add(board.Hand[i]);

        float score;

        if (mine.Count == 3)
        {
            int myStrength = Strength(mine[0], mine[1], mine[2]);

            if (opp.Count == 3)
            {
                int oppStrength = Strength(opp[0], opp[1], opp[2]);
                // 동점이면 상대가 먼저 완성했으므로 상대 승
                score = myStrength > oppStrength ? 1000 + myStrength : -200 - (CardValue(card) * 5);
            }
            else if (GameManager.IsProvenWin(mine, opp, board.FieldUnused))
            {
                score = 900 + myStrength;
            }
            else
            {
                Potential(opp, board.Unseen, out float oppMean, out float oppMax);
                // 상대 기대값보다 강하면 유리
                score = 300 + (myStrength - oppMean) * 1.2f;
                if (myStrength >= oppMax) score += 150;
            }
        }
        else
        {
            Potential(mine, pool, out float myMean, out float myMax);
            score = myMean * 0.7f + myMax * 0.3f;

            if (opp.Count > 0)
            {
                Potential(opp, board.Unseen, out float oppMean, out _);
                score += (score - oppMean) * 0.5f;
            }

            // 빈 경계석을 새로 여는 것보다는 이미 진행 중인 경계석을 이어가는 것을 약간 선호
            if (mine.Count == 2) score += 20;
        }

        // 경계석 위치 가중치: 연속 3개 / 상대의 연속 3개 저지
        score += AdjacencyBonus(board.Owners, round);

        // (어려움) 승부처에서는 좋은 수를 더 높이, 약한 카드로 버리는 수는 더 낮게 평가
        // → 강한 조합은 승부처에 집중하고, 약한 카드는 덜 중요한 경계석에 버림
        if (threatAware)
        {
            float crit = Criticality(board.Owners, round);
            if (crit > 0) score += crit * (score - 250f) * 0.6f;
        }

        return score;
    }

    /// <summary>
    /// 부분 패(partial)에 pool의 카드를 채워 3장을 만들 때의 평균/최대 강도.
    /// </summary>
    private static void Potential(List<Card> partial, List<Card> pool, out float mean, out float max)
    {
        int need = 3 - partial.Count;
        mean = 0; max = 0;

        if (need <= 0)
        {
            mean = max = Strength(partial[0], partial[1], partial[2]);
            return;
        }

        long total = 0;
        int count = 0;
        int best = 0;

        if (need == 1)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                int s = Strength(partial[0], partial[1], pool[i]);
                total += s; count++;
                if (s > best) best = s;
            }
        }
        else if (need == 2)
        {
            for (int i = 0; i < pool.Count; i++)
                for (int j = i + 1; j < pool.Count; j++)
                {
                    int s = Strength(partial[0], pool[i], pool[j]);
                    total += s; count++;
                    if (s > best) best = s;
                }
        }
        else
        {
            // 빈 진영: 대략적인 평균 강도
            mean = 160; max = 524;
            return;
        }

        if (count == 0)
        {
            // 더 이상 완성할 카드가 없음
            mean = 0; max = 0;
            return;
        }
        mean = (float)total / count;
        max = best;
    }

    private static float AdjacencyBonus(int[] owners, int round)
    {
        float bonus = 0;
        for (int d = -2; d <= 2; d++)
        {
            if (d == 0) continue;
            int i = round + d;
            if (i < 0 || i >= owners.Length) continue;
            float w = Mathf.Abs(d) == 1 ? 1f : 0.5f;
            if (owners[i] == 2) bonus += 15 * w;  // 내 연속 3개 노림
            if (owners[i] == 1) bonus += 10 * w;  // 상대 연속 3개 저지
        }
        return bonus;
    }

    private static int CardValue(Card c) => c.CardNumber;
}
