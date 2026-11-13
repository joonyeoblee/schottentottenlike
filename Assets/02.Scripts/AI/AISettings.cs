using UnityEngine;

public enum AIDifficulty
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
}

/// <summary>
/// AI 대전 난이도. 로비에서 고른 값이 저장되어 다음 실행에도 유지됩니다.
/// </summary>
public static class AISettings
{
    private const string Key = "ai_difficulty";

    public static AIDifficulty Difficulty
    {
        get => (AIDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)AIDifficulty.Normal), 0, 2);
        set { PlayerPrefs.SetInt(Key, (int)value); PlayerPrefs.Save(); }
    }

    public static string DisplayName(AIDifficulty d) => d switch
    {
        AIDifficulty.Easy => "쉬움",
        AIDifficulty.Hard => "어려움",
        _ => "보통",
    };

    public static string Description(AIDifficulty d) => d switch
    {
        AIDifficulty.Easy => "규칙을 익히기 좋아요. AI가 가끔 엉뚱한 수를 둬요.",
        AIDifficulty.Hard => "AI가 매 수 최선을 다해요. 상대의 연속 3개를 끈질기게 막아요.",
        _ => "적당히 머리를 쓰는 AI예요.",
    };

    /// <summary>AI가 생각하는 시간(초). 어려울수록 신중하게(연출상)</summary>
    public static float ThinkDelay(AIDifficulty d) => d switch
    {
        AIDifficulty.Easy => 0.6f,
        AIDifficulty.Hard => 1.0f,
        _ => 0.8f,
    };
}
