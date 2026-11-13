using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class BattleField_AI : Singleton<BattleField_AI>
{
    [Header("관리 매니저")]
    public HandCardManager[] HandCardManagers; // [0] = 내 카드, [1] = 상대 카드
    public CardDeck CardDeck;

    [Header("슬롯")]
    public RoundSlot[] Rounds;

    [Header("게임 상태")]
    public ETurn CurrentTurn;

    [Header("AI 설정")]
    private AIDifficulty _difficulty;                    // 로비에서 고른 난이도 (AISettings)
    private readonly System.Random _aiRng = new System.Random();
    [SerializeField] private int _lobbySceneIndex = 1;

    private bool _isShuffled;
    private bool _isDealing;
    private UI_MatchHud _hud;
    private TMPro.TMP_Text _aiScoreText;
    public new static BattleField_AI Instance;

    public bool IsPlayerTurn => CurrentTurn == ETurn.Player1 && !_isDealing && !IsGameOver;
    private bool IsGameOver => GameManager.Instance != null && GameManager.Instance.IsGameOver;

    protected override void Awake()
    {
        Instance = this;

        // 메인 씬을 거치지 않고 AI 씬을 바로 실행해도 동작하도록 GameManager를 보장
        if (GameManager.Instance == null)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
        }
    }

    protected override void Start()
    {
        InitializeRoundSlots();
        InitializeHandCardSlots();
        CardDeck.OnCardSuffle += OnShuffled;
        GameManager.OnGameOver += HandleGameOver;

        _hud = UI_MatchHud.Create(transform);
        // 하스스톤식 손패 배치: 상대 손패는 화면 위에서 뒤집힌 부채꼴로, 내 손패는 화면 아래에서 반쯤 올라오게
        HandDock.Attach(HandCardManagers[1].gameObject, HandDock.Edge.Top, 1.0f, 0.72f, 180f, new Vector2(7.3f, 2.19f), 0.13f);
        HandDock.Attach(HandCardManagers[0].gameObject, HandDock.Edge.Bottom, 1.0f, 0.63f, 0f, new Vector2(9.57f, 3.19f), -0.405f);
        _hud.OnHelp += () => UI_Tutorial.Show(_hud.Font, true, null);
        _hud.OnBack += ConfirmExit;

        // 씬에 배치된 AI 정보 패널의 "획득 돌" 텍스트 (있으면 갱신)
        var scoreGo = GameObject.Find("AIScoreText");
        if (scoreGo != null) _aiScoreText = scoreGo.GetComponent<TMPro.TMP_Text>();

        // 난이도 표시 (상대 정보 패널)
        _difficulty = AISettings.Difficulty;
        var nameGo = GameObject.Find("AINameText");
        if (nameGo != null)
        {
            var nameText = nameGo.GetComponent<TMPro.TMP_Text>();
            nameText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            nameText.text = $"AI ({AISettings.DisplayName(_difficulty)})";
        }

        // 처음 플레이하면 규칙 튜토리얼을 먼저 보여주고, 첫 판은 안내 힌트와 함께 진행
        _coachMoves = PlayerPrefs.GetInt(CoachKey, 0) == 1 ? 0 : 3;
        if (!UI_Tutorial.IsDone)
            UI_Tutorial.Show(_hud.Font, false, GameStart);
        else
            GameStart();
    }

    private void ConfirmExit()
    {
        _hud.ShowConfirm("게임을 나갈까요?", "진행 중인 판은 저장되지 않아요", "나가기",
            () => SceneManager.LoadScene(_lobbySceneIndex), "계속하기");
    }

    // ---- 첫 판 안내 (코치) ----
    private const string CoachKey = "coach_done_v1";
    private int _coachMoves;      // 남은 안내 횟수
    private bool _claimHintShown;

    private void CoachOnPlayerTurn()
    {
        if (_coachMoves <= 0 || IsGameOver) return;
        _hud.ShowHint("손패의 카드를 끌어서 <color=#B8862F><b>빛나는 칸</b></color>에 놓으세요");
        DropHighlight.Coach(true);
    }

    private void CoachOnPlaced()
    {
        if (_coachMoves <= 0) return;
        DropHighlight.Coach(false);
        _coachMoves--;
        if (_coachMoves == 0)
        {
            PlayerPrefs.SetInt(CoachKey, 1);
            PlayerPrefs.Save();
            _hud.ShowHint("좋아요! 같은 색이나 연속 숫자를 모아 강한 조합을 만들어 보세요", 3.5f);
        }
        else
        {
            _hud.ShowHint("좋아요! 이제 AI 차례예요", 2f);
        }
    }

    private void CoachOnClaim(int owner)
    {
        if (_claimHintShown) return;
        _claimHintShown = true;
        _hud.ShowHint(owner == 1
            ? "경계석을 차지했어요! <b>이웃한 3개</b> 또는 <b>5개</b>를 모으면 승리!"
            : "AI가 경계석을 차지했어요. 이웃한 3개를 내주지 않게 조심하세요!", 4f);
    }

    private void OnDestroy()
    {
        if (CardDeck != null) CardDeck.OnCardSuffle -= OnShuffled;
        GameManager.OnGameOver -= HandleGameOver;
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 게임 시작 또는 재시작 시 호출됩니다.
    /// </summary>
    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        StopAllCoroutines();
        GameManager.Instance.ResetGame();
        ClearAllCardSlots();
        foreach (var round in Rounds) round.ResetStone();
        _hud.HideResult();
        UpdateHud();
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
        _isDealing = true;
        _isShuffled = false;
        CardDeck.StartDeckSuffle();
        yield return new WaitUntil(() => _isShuffled);
        yield return new WaitForSeconds(0.5f);

        // 첫 패 분배
        DealFirstTurnCards();
        yield return new WaitForSeconds(0.6f);
        _isDealing = false;

        // 선공 턴 랜덤 결정
        SetTurn(Random.Range(0, 2));

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
    /// 플레이어가 이 슬롯에 카드를 낼 수 있는지 (드래그 앤 드롭 검증용)
    /// </summary>
    public bool CanPlayerPlaceOn(CardSlot slot)
    {
        if (!IsPlayerTurn || slot == null || !slot.IsMine || slot.IsOccupied) return false;
        if (slot.RoundIndex < 0 || slot.RoundIndex >= Rounds.Length) return false;
        if (GameManager.Instance.IsStoneClaimed(slot.RoundIndex)) return false;
        return Rounds[slot.RoundIndex].FirstEmptySlot(true) == slot;
    }

    /// <summary>
    /// 내 카드가 필드에 놓였을 때 호출됩니다. (UI_CardDragger에서 1회 호출)
    /// </summary>
    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        if (CurrentTurn != ETurn.Player1) return; // 중복 호출 방지

        int round = placedSlot.RoundIndex;
        GameManager.Instance.RecordStoneCompleted(round, 1, Rounds[round].CountCards(true));

        // 턴을 즉시 넘겨서 연출 중에 다른 카드를 또 내지 못하게 함
        CurrentTurn = ETurn.Player2;
        CoachOnPlaced();

        JudgeAllRoundsWinner();
        StartCoroutine(HandleCardPlacedSequence());
    }

    /// <summary>
    /// 카드 제출 후 처리 시퀀스
    /// </summary>
    private IEnumerator HandleCardPlacedSequence()
    {
        yield return new WaitForSeconds(0.05f);

        // 비어있는 내 손패에 카드 드로우
        DrawCard(0, true);

        if (IsGameOver) yield break;

        SetTurn((int)ETurn.Player2);
        StartAITurn();
    }

    private void DrawCard(int handManagerIndex, bool visible)
    {
        var slots = HandCardManagers[handManagerIndex].HandCardSlots;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].HasCard) continue;

            var card = CardDeck.GetCard();
            if (card != null)
            {
                slots[i].Refresh(i, card, visible);
                if (slots[i].MyCard?.Rend != null)
                    slots[i].MyCard.Rend.enabled = true;
            }
            break;
        }
    }

    public void SetTurn(int turn)
    {
        CurrentTurn = (ETurn)turn;
        Debug.Log($"턴이 {CurrentTurn}으로 변경됨");
        UpdateHud();

        if (IsGameOver || _isDealing) return;

        // 낼 수 있는 카드가 없으면 턴을 넘기거나, 양쪽 모두 불가능하면 게임 종료
        bool playerCan = HasLegalMove(true);
        bool aiCan = HasLegalMove(false);
        if (!playerCan && !aiCan)
        {
            GameManager.Instance.EndByStalemate();
            return;
        }

        if (CurrentTurn == ETurn.Player1 && !playerCan)
        {
            Debug.Log("플레이어가 낼 수 있는 카드가 없어 턴을 넘깁니다.");
            _hud.ShowHint("낼 수 있는 칸이 없어 차례를 넘겨요", 2f);
            CurrentTurn = ETurn.Player2;
            UpdateHud();
            StartAITurn();
            return;
        }

        if (CurrentTurn == ETurn.Player1) CoachOnPlayerTurn();
    }

    private bool HasLegalMove(bool player)
    {
        var hand = HandCardManagers[player ? 0 : 1].HandCardSlots;
        if (!hand.Any(s => s.HasCard)) return false;

        for (int r = 0; r < Rounds.Length; r++)
        {
            if (GameManager.Instance.IsStoneClaimed(r)) continue;
            if (Rounds[r].FirstEmptySlot(player) != null) return true;
        }
        return false;
    }

    /// <summary>
    /// 플레이어가 카드를 두는 UI에서 호출 (드래그 이외의 입력 방식용)
    /// </summary>
    public void OnPlayerWantsToPlaceCard(int handIndex, int roundIndex, int slotIndex)
    {
        var handSlot = HandCardManagers[0].HandCardSlots[handIndex];
        var card = handSlot.Card;
        if (card == null) return;

        var fieldSlot = Rounds[roundIndex].PlayerCardSlots[slotIndex];
        if (!CanPlayerPlaceOn(fieldSlot)) return;

        fieldSlot.Refresh(card);
        handSlot.Clear();
        OnMyCardPlaced(fieldSlot);
    }

    /// <summary>
    /// AI의 턴 처리
    /// </summary>
    public void StartAITurn()
    {
        if (IsGameOver) return;
        StartCoroutine(AITurnRoutine());
    }

    private IEnumerator AITurnRoutine()
    {
        yield return new WaitForSeconds(AISettings.ThinkDelay(_difficulty));
        if (IsGameOver) yield break;

        var handManager = HandCardManagers[1];
        var handSlots = handManager.HandCardSlots;

        var move = AIBrain.ChooseMove(BuildBoardView(), _difficulty, _aiRng);

        if (move.HasValue)
        {
            var handSlot = handSlots[move.Value.HandIndex];
            var card = handSlot.Card;
            var round = Rounds[move.Value.RoundIndex];
            var targetSlot = round.FirstEmptySlot(false);

            if (!targetSlot.gameObject.activeSelf)
                targetSlot.gameObject.SetActive(true);

            targetSlot.Refresh(card);
            handSlot.Clear();
            GameManager.Instance.RecordStoneCompleted(move.Value.RoundIndex, 2, round.CountCards(false));
            JudgeAllRoundsWinner();

            // AI 카드 드로우 (뒷면)
            DrawCard(1, false);
        }
        else
        {
            Debug.Log("AI가 낼 카드가 없습니다.");
        }

        if (IsGameOver) yield break;

        // 턴 전환
        SetTurn((int)ETurn.Player1);
    }

    private AIBrain.BoardView BuildBoardView()
    {
        var view = new AIBrain.BoardView
        {
            AiCards = new List<Card>[Rounds.Length],
            PlayerCards = new List<Card>[Rounds.Length],
            Owners = (int[])GameManager.Instance.RoundOwners.Clone(),
            Hand = HandCardManagers[1].HandCardSlots.Select(s => s.HasCard ? s.Card : null).ToList(),
            FieldUnused = GetUnusedCardsFromField(),
            SlotsPerSide = Rounds[0].EnemyCardSlots.Length,
        };

        for (int r = 0; r < Rounds.Length; r++)
        {
            view.AiCards[r] = GetEnemyCards(r);
            view.PlayerCards[r] = GetPlayerCards(r);
        }

        // AI가 모르는 카드 = 필드에 없는 카드 - AI 손패
        view.Unseen = view.FieldUnused
            .Where(c => !view.Hand.Any(h => h != null && h.CardNumber == c.CardNumber && h.Color == c.Color))
            .ToList();
        return view;
    }

    private void JudgeAllRoundsWinner()
    {
        List<Card> unusedCards = GetUnusedCardsFromField();

        for (int roundIndex = 0; roundIndex < Rounds.Length; roundIndex++)
        {
            if (!GameManager.Instance.IsStoneClaimed(roundIndex))
            {
                List<Card> playerCards = GetPlayerCards(roundIndex);
                List<Card> enemyCards = GetEnemyCards(roundIndex);

                JudgeResult judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards, roundIndex);
                GameManager.Instance.UpdateRoundOwnerAndCheckWin(roundIndex, judgeResult.Winner);

                if (judgeResult.Winner != 0)
                {
                    string who = judgeResult.Winner == 1 ? "플레이어" : "AI";
                    Debug.Log($"경계석 {roundIndex}: {who} 점령 ({judgeResult.Player1Rank} vs {judgeResult.Player2Rank})");
                    if (GameManager.Instance.RoundOwners[roundIndex] == judgeResult.Winner)
                        CoachOnClaim(judgeResult.Winner);
                }
            }

            int owner = GameManager.Instance.RoundOwners[roundIndex];
            Rounds[roundIndex].MoveStoneToOwner(owner);
            Rounds[roundIndex].RefreshSlotVisibility(owner != 0);
        }

        UpdateHud();
    }

    private void UpdateHud()
    {
        if (_hud == null || GameManager.Instance == null) return;

        var owners = GameManager.Instance.RoundOwners;
        int aiStones = owners.Count(o => o == 2);
        _hud.SetScore(owners.Count(o => o == 1), aiStones, "나", "AI");
        if (_aiScoreText != null) _aiScoreText.text = $"획득 돌: {aiStones}개";

        if (IsGameOver)
            _hud.SetTurn(null, false);
        else if (_isDealing)
            _hud.SetTurn("카드 분배 중", true);
        else
            _hud.SetTurn(CurrentTurn == ETurn.Player1 ? "내 차례" : "AI 차례", CurrentTurn == ETurn.Player1);
    }

    private void HandleGameOver(int winner)
    {
        StopAllCoroutines();
        DropHighlight.Coach(false);
        _hud.HideHint();
        UpdateHud();

        string title = winner == 1 ? "승리" : winner == 2 ? "패배" : "무승부";
        string sub = DescribeResult(winner) + $"\n<size=26>AI 난이도: {AISettings.DisplayName(_difficulty)}</size>";

        StartCoroutine(ShowResultDelayed(title, winner == 1, sub));
    }

    private IEnumerator ShowResultDelayed(string title, bool isWin, string sub)
    {
        // 마지막 카드/경계석 연출을 볼 수 있도록 잠시 대기
        yield return new WaitForSeconds(1.0f);
        _hud.ShowResult(title, isWin, sub,
            "다시 하기", GameStart,
            "로비로", () => SceneManager.LoadScene(_lobbySceneIndex));
    }

    private string DescribeResult(int winner)
    {
        var owners = GameManager.Instance.RoundOwners;
        int mine = owners.Count(o => o == 1);
        int ai = owners.Count(o => o == 2);
        if (winner == 0) return $"경계석 {mine} : {ai} — 더 이상 낼 카드가 없습니다";

        int w = winner;
        for (int i = 0; i <= owners.Length - 3; i++)
            if (owners[i] == w && owners[i + 1] == w && owners[i + 2] == w)
                return $"연속된 경계석 3개 점령!  (경계석 {mine} : {ai})";

        if ((w == 1 ? mine : ai) >= 5)
            return $"경계석 5개 점령!  (경계석 {mine} : {ai})";

        return $"경계석 {mine} : {ai}";
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
}
