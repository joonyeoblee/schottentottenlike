using UnityEngine;

/// <summary>
/// 전적 기록 (기기에 저장). AI 난이도별 + 온라인 대전.
/// 결과: 1 = 승, -1 = 패, 0 = 무
/// </summary>
public static class PlayerStats
{
    public struct Record
    {
        public int Wins, Losses, Draws, Streak, BestStreak;
        public int Games => Wins + Losses + Draws;
        public int WinRate => Games == 0 ? 0 : Mathf.RoundToInt(100f * Wins / Games);
    }

    private static string AiKey(AIDifficulty d) => "stats_ai_" + (int)d;
    private const string OnlineKey = "stats_online";

    public static Record Get(string key) => new Record
    {
        Wins = PlayerPrefs.GetInt(key + "_w", 0),
        Losses = PlayerPrefs.GetInt(key + "_l", 0),
        Draws = PlayerPrefs.GetInt(key + "_d", 0),
        Streak = PlayerPrefs.GetInt(key + "_s", 0),
        BestStreak = PlayerPrefs.GetInt(key + "_b", 0),
    };

    public static Record GetAI(AIDifficulty d) => Get(AiKey(d));
    public static Record GetOnline() => Get(OnlineKey);

    private static void Add(string key, int result)
    {
        var r = Get(key);
        if (result > 0) { r.Wins++; r.Streak++; r.BestStreak = Mathf.Max(r.BestStreak, r.Streak); }
        else if (result < 0) { r.Losses++; r.Streak = 0; }
        else r.Draws++;
        PlayerPrefs.SetInt(key + "_w", r.Wins);
        PlayerPrefs.SetInt(key + "_l", r.Losses);
        PlayerPrefs.SetInt(key + "_d", r.Draws);
        PlayerPrefs.SetInt(key + "_s", r.Streak);
        PlayerPrefs.SetInt(key + "_b", r.BestStreak);
        PlayerPrefs.Save();
    }

    public static void RecordAI(AIDifficulty d, int result) => Add(AiKey(d), result);
    public static void RecordOnline(int result) => Add(OnlineKey, result);

    public static string Summary(Record r)
    {
        if (r.Games == 0) return "아직 기록 없음";
        string s = $"{r.Wins}승 {r.Losses}패" + (r.Draws > 0 ? $" {r.Draws}무" : "") + $" (승률 {r.WinRate}%)";
        if (r.Streak >= 2) s += $" · {r.Streak}연승 중!";
        return s;
    }

    public static string AISummary(AIDifficulty d) => $"{AISettings.DisplayName(d)} 전적: {Summary(GetAI(d))}";
    public static string OnlineSummary() => $"온라인 전적: {Summary(GetOnline())}";
}
