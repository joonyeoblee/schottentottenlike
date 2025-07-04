#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShotenToTenAITestTool : EditorWindow
{
    private List<Card> deck = new List<Card>();
    private List<Card> player1Hand = new List<Card>();
    private List<Card> player2Hand = new List<Card>();
    private List<Card>[] player1Fields = new List<Card>[9];
    private List<Card>[] player2Fields = new List<Card>[9];
    private string resultLog = "";
    private int currentPlayer = 1; // 1: 플레이어1, 2: AI
    private bool gameEnded = false;
    private Vector2 deckScroll;
    private Vector2 mainScroll; // 전체 스크롤

    [MenuItem("Tools/ShotenToTen AI Test Tool")]
    public static void ShowWindow()
    {
        GetWindow<ShotenToTenAITestTool>("ShotenToTen AI Test Tool");
    }

    private void OnEnable()
    {
        ResetGame();
    }

    private void OnGUI()
    {
        mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("게임 리셋", GUILayout.Height(30)))
        {
            ResetGame();
        }
        if (GUILayout.Button("AI 자동진행", GUILayout.Height(30)))
        {
            while (!gameEnded)
                PlayAITurn();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 덱 정보 (색상별 정렬, 스크롤, 박스, 줄바꿈)
        EditorGUILayout.LabelField($"[덱] 남은 카드: {deck.Count}", EditorStyles.boldLabel);
        deckScroll = EditorGUILayout.BeginScrollView(deckScroll, GUILayout.Height(160));
        var colorGroups = deck
            .OrderBy(c => c.Color.ToString())
            .ThenBy(c => c.CardNumber)
            .GroupBy(c => c.Color)
            .OrderBy(g => g.Key.ToString());
        foreach (var group in colorGroups)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(group.Key.ToString(), EditorStyles.boldLabel, GUILayout.Width(70));
            foreach (var card in group)
            {
                DrawDeckCardBox(card);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // 필드 정보 (9개)
        EditorGUILayout.LabelField("[필드 상황]", EditorStyles.boldLabel);
        for (int i = 0; i < 9; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"필드 {i + 1}");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("플레이어 1:", GUILayout.Width(70));
            foreach (var card in player1Fields[i])
                DrawCardLabel(card);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("AI:", GUILayout.Width(70));
            foreach (var card in player2Fields[i])
                DrawCardLabel(card);
            EditorGUILayout.EndHorizontal();

            // 승패 판정
            if (player1Fields[i].Count == 3 && player2Fields[i].Count == 3)
            {
                var judge = JudgeField(player1Fields[i], player2Fields[i]);
                EditorGUILayout.HelpBox(judge, MessageType.Info);
            }
            EditorGUILayout.EndVertical();
        }

        GUILayout.Space(10);

        // 플레이어 1 핸드
        EditorGUILayout.LabelField("[플레이어 1 패]", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < player1Hand.Count; i++)
        {
            if (currentPlayer == 1 && !gameEnded)
            {
                if (GUILayout.Button(CardToString(player1Hand[i]), GUILayout.Width(80)))
                {
                    ShowFieldSelectPopup(1, i);
                }
            }
            else
            {
                DrawCardLabel(player1Hand[i]);
            }
        }
        EditorGUILayout.EndHorizontal();

        // AI 핸드
        EditorGUILayout.LabelField("[AI 패]", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < player2Hand.Count; i++)
        {
            if (currentPlayer == 2 && !gameEnded)
            {
                if (GUILayout.Button(CardToString(player2Hand[i]), GUILayout.Width(80)))
                {
                    ShowFieldSelectPopup(2, i);
                }
            }
            else
            {
                DrawCardLabel(player2Hand[i]);
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 턴 및 결과 안내
        if (!gameEnded)
        {
            string turnText = currentPlayer == 1 ? "플레이어 1" : "AI";
            EditorGUILayout.HelpBox($"현재 턴: {turnText}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(resultLog, MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCardLabel(Card card)
    {
        EditorGUILayout.LabelField(CardToString(card), GUILayout.Width(80));
    }

    // 덱 카드 박스 표시
    private void DrawDeckCardBox(Card card)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(60));
        EditorGUILayout.HelpBox(CardToString(card), MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private string CardToString(Card card)
    {
        return $"{card.Color} {card.CardNumber}";
    }

    private void ResetGame()
    {
        // 덱 생성 (색상 6종, 숫자 1~9)
        deck = new List<Card>();
        foreach (ECardColor color in System.Enum.GetValues(typeof(ECardColor)))
            for (int num = 1; num <= 9; num++)
                deck.Add(new Card(num, color));

        // 셔플
        deck = deck.OrderBy(x => Random.value).ToList();

        // 핸드 분배 (각 6장)
        player1Hand = deck.Take(6).ToList();
        player2Hand = deck.Skip(6).Take(6).ToList();
        deck = deck.Skip(12).ToList();

        // 필드 초기화 (9개)
        for (int i = 0; i < 9; i++)
        {
            player1Fields[i] = new List<Card>();
            player2Fields[i] = new List<Card>();
        }

        currentPlayer = 1;
        gameEnded = false;
        resultLog = "";
    }

    private void ShowFieldSelectPopup(int player, int handIdx)
    {
        GenericMenu menu = new GenericMenu();
        for (int i = 0; i < 9; i++)
        {
            int fieldIndex = i; 
            bool canPlace = (player == 1 ? player1Fields[fieldIndex] : player2Fields[fieldIndex]).Count < 3;
            if (canPlace)
            {
                menu.AddItem(new GUIContent($"필드 {fieldIndex + 1}"), false, () => PlaceCard(player, handIdx, fieldIndex));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent($"필드 {fieldIndex + 1} (가득참)"));
            }
        }
        menu.ShowAsContext();
    }


    private void PlaceCard(int player, int handIdx, int fieldIdx)
    {
        if (gameEnded) return;
        if (fieldIdx < 0 || fieldIdx >= 9) return; // 인덱스 체크 추가

        if (player == 1)
        {
            if (handIdx < 0 || handIdx >= player1Hand.Count) return; // 인덱스 체크
            var card = player1Hand[handIdx];
            player1Hand.RemoveAt(handIdx);
            player1Fields[fieldIdx].Add(card);

            // 카드 제출 후 덱에서 1장 뽑기
            if (deck.Count > 0)
            {
                player1Hand.Add(deck[0]);
                deck.RemoveAt(0);
            }

            currentPlayer = 2;
        }
        else
        {
            if (handIdx < 0 || handIdx >= player2Hand.Count) return; // 인덱스 체크
            var card = player2Hand[handIdx];
            player2Hand.RemoveAt(handIdx);
            player2Fields[fieldIdx].Add(card);

            // 카드 제출 후 덱에서 1장 뽑기
            if (deck.Count > 0)
            {
                player2Hand.Add(deck[0]);
                deck.RemoveAt(0);
            }

            currentPlayer = 1;
        }

        CheckGameEnd();
        Repaint();
    }

    private void PlayAITurn()
    {
        if (gameEnded) return;

        if (currentPlayer == 1 && player1Hand.Count > 0)
        {
            int fieldIdx = System.Array.FindIndex(player1Fields, f => f.Count < 3);
            if (fieldIdx != -1)
            {
                PlaceCard(1, 0, fieldIdx);
            }
        }
        else if (currentPlayer == 2 && player2Hand.Count > 0)
        {
            int fieldIdx = System.Array.FindIndex(player2Fields, f => f.Count < 3);
            if (fieldIdx != -1)
            {
                PlaceCard(2, 0, fieldIdx);
            }
        }
    }

    private void CheckGameEnd()
    {
        bool allFieldsFull = true;
        for (int i = 0; i < 9; i++)
        {
            if (player1Fields[i].Count < 3 || player2Fields[i].Count < 3)
            {
                allFieldsFull = false;
                break;
            }
        }
        if (allFieldsFull || (player1Hand.Count == 0 && player2Hand.Count == 0))
        {
            gameEnded = true;
            // 최종 결과 계산
            int p1Win = 0, p2Win = 0;
            for (int i = 0; i < 9; i++)
            {
                var result = JudgeField(player1Fields[i], player2Fields[i]);
                if (result.Contains("플레이어 1")) p1Win++;
                else if (result.Contains("AI")) p2Win++;
            }
            resultLog = $"최종 결과: 플레이어 1 {p1Win}승, AI {p2Win}승\n";
            if (p1Win > p2Win) resultLog += "플레이어 1이 최종 승리!";
            else if (p2Win > p1Win) resultLog += "AI가 최종 승리!";
            else resultLog += "무승부!";
        }
    }

    private string JudgeField(List<Card> p1, List<Card> p2)
    {
        if (p1.Count < 3 || p2.Count < 3)
            return "아직 카드가 부족합니다.";

        var rank1 = EvaluateHand(p1);
        var rank2 = EvaluateHand(p2);

        if (rank1 > rank2)
            return $"플레이어 1 승 (족보: {rank1} vs {rank2})";
        else if (rank2 > rank1)
            return $"AI 승 (족보: {rank1} vs {rank2})";
        else
        {
            int sum1 = p1.Sum(c => c.CardNumber);
            int sum2 = p2.Sum(c => c.CardNumber);
            if (sum1 > sum2)
                return $"플레이어 1 승 (합: {sum1} vs {sum2})";
            else if (sum2 > sum1)
                return $"AI 승 (합: {sum1} vs {sum2})";
            else
                return "무승부";
        }
    }

    private enum HandRank
    {
        StraightFlush = 5,
        ThreeOfAKind = 4,
        Straight = 3,
        Flush = 2,
        CardSum = 1
    }

    private HandRank EvaluateHand(List<Card> cards)
    {
        var numbers = cards.Select(c => c.CardNumber).OrderBy(n => n).ToArray();
        bool isFlush = cards.All(c => c.Color == cards[0].Color);
        bool isStraight = numbers.Length == 3 && numbers[2] - numbers[0] == 2 && numbers.Distinct().Count() == 3;
        bool isThree = cards.GroupBy(c => c.CardNumber).Any(g => g.Count() == 3);

        if (isFlush && isStraight) return HandRank.StraightFlush;
        if (isThree) return HandRank.ThreeOfAKind;
        if (isStraight) return HandRank.Straight;
        if (isFlush) return HandRank.Flush;
        return HandRank.CardSum;
    }
}
#endif
