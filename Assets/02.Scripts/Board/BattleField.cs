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

    // 판정/점령 정보는 각 클라이언트 '로컬 시점'으로 관리합니다. (1 = 나, 2 = 상대)
    private const int LocalMe = 1;
    private const int LocalEnemy = 2;

    private UI_MatchHud _hud;
    private int _enemyHandCount;
    private bool _matchStarted;
    private bool _dealt;
    private readonly HashSet<int> _rematchReady = new HashSet<int>();

    /// <summary>
    /// 지금이 내 차례인지 (마스터 = Player1, 다른 클라이언트 = Player2)
    /// </summary>
    public bool IsMyTurn
    {
        get
        {
            // 첫 패를 다 받기 전(딜 연출 중)에는 카드를 낼 수 없음
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver || !_matchStarted || !_dealt) return false;
            bool isMaster = PhotonNetwork.IsMasterClient;
            return (isMaster && CurrentTurn == ETurn.Player1) || (!isMaster && CurrentTurn == ETurn.Player2);
        }
    }

    private ETurn MyTurnValue => PhotonNetwork.IsMasterClient ? ETurn.Player1 : ETurn.Player2;

    // 현재 턴의 다음 턴을 반환하는 헬퍼 프로퍼티
    private ETurn GetNextTurn() => CurrentTurn == ETurn.Player1 ? ETurn.Player2 : ETurn.Player1;



    private void Start()
    {
        EnsureInitialized();
    }

    private bool _initialized;

    /// <summary>
    /// 초기화. 게스트 쪽에서는 Field가 네트워크로 생성되는 바로 그 프레임에 방장의 RPC(판 초기화/턴/딜)가
    /// Start()보다 먼저 도착할 수 있으므로, 모든 RPC 진입점에서도 호출합니다.
    /// (예전에는 이때 HUD가 null이라 예외가 나서 게스트에게 영원히 차례가 오지 않았음)
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        InitializeRoundSlots();
        InitializeHandCardSlots();
        MyplayerNumber = PhotonNetwork.IsMasterClient ? 1 : 2;
        CardDeck.OnCardSuffle += OnShuffled;
        GameManager.OnGameOver += HandleGameOver;

        _hud = UI_MatchHud.Create(transform);
        // 하스스톤식 손패 배치: 상대 손패는 화면 위에서 뒤집힌 부채꼴로, 내 손패는 화면 아래에서 반쯤 올라오게
        HandDock.Attach(HandCardManagers[1].gameObject, HandDock.Edge.Top, 1.0f, 0.72f, 180f, new Vector2(7.3f, 2.19f), 0.13f);
        HandDock.Attach(HandCardManagers[0].gameObject, HandDock.Edge.Bottom, 1.0f, 0.63f, 0f, new Vector2(9.57f, 3.19f), -0.405f);
        _hud.OnHelp += () => UI_Tutorial.Show(_hud.Font, false, null); // 멀티는 시간 정지 없이
        _hud.OnBack += () => _hud.ShowConfirm("방을 나갈까요?", "상대에게는 기권으로 처리돼요", "나가기", LeaveToLobby, "계속하기");
        _hud.SetTurn("상대를 기다리는 중", true);
        UpdateScore();

        // 첫 플레이가 멀티 대전이라면 여기서 규칙 안내를 한 번 보여줌 (이후엔 '규칙' 버튼으로)
        if (!UI_Tutorial.IsDone) UI_Tutorial.Show(_hud.Font, false, null);
    }

    private void OnDestroy()
    {
        if (CardDeck != null) CardDeck.OnCardSuffle -= OnShuffled;
        GameManager.OnGameOver -= HandleGameOver;
    }

    /// <summary>
    /// 게임 시작 또는 재시작 시 호출됩니다. (마스터 클라이언트에서만 호출)
    /// </summary>
    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        if (!PhotonNetwork.IsMasterClient) return;

        // 모든 클라이언트의 판 상태 초기화 → 이후 셔플/딜 RPC가 순서대로 도착
        photonView.RPC(nameof(RPC_PrepareMatch), RpcTarget.All);
        StartCoroutine(GameStartSequence());
    }

    [PunRPC]
    private void RPC_PrepareMatch()
    {
        EnsureInitialized();
        StopAllCoroutines();
        GameManager.Instance.ResetGame();
        ClearAllCardSlots(); // 모든 슬롯을 깨끗하게 초기화
        foreach (var round in Rounds) round.ResetStone();
        _rematchReady.Clear();
        _enemyHandCount = 0;
        _dealt = false;
        _matchStarted = true;
        _hud.HideResult();
        UpdateScore();
        if (RoundDicator != null) RoundDicator.Turn();
    }

    /// <summary>
    /// 내가 이 슬롯에 카드를 낼 수 있는지 (드래그 앤 드롭 검증용)
    /// </summary>
    public bool CanPlaceOn(CardSlot slot)
    {
        if (!IsMyTurn || slot == null || !slot.IsMine || slot.IsOccupied) return false;
        if (slot.RoundIndex < 0 || slot.RoundIndex >= Rounds.Length) return false;
        if (GameManager.Instance.IsStoneClaimed(slot.RoundIndex)) return false;
        return Rounds[slot.RoundIndex].FirstEmptySlot(true) == slot;
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
        EnsureInitialized();
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

        _enemyHandCount = 0;
        for (int i = 0; i < HandCardManagers[1].HandCardSlots.Length; i++)
        {
            Card card = new Card(enemyNumbers[i], (ECardColor)enemyColors[i]);
            HandCardManagers[1].HandCardSlots[i].Refresh(i, card, false);
            _enemyHandCount++;
            yield return new WaitForSeconds(0.2f);
        }

        PlayEnemyHandAnimation();
        Debug.Log("손패 배분 완료 (로컬 기준)");
        _dealt = true;
        UpdateTurnHud();
    }

    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        int round = placedSlot.RoundIndex;
        GameManager.Instance.RecordStoneCompleted(round, LocalMe, Rounds[round].CountCards(true));

        // 턴 전환 RPC가 돌아오기 전에 한 장 더 내는 것을 막기 위해 로컬 턴을 먼저 넘김
        ETurn nextTurn = GetNextTurn();
        CurrentTurn = nextTurn;

        JudgeAllRoundsWinner();
        StartCoroutine(HandleCardPlacedSequence(nextTurn));
    }

    private IEnumerator HandleCardPlacedSequence(ETurn nextTurn)
    {
        yield return new WaitForSeconds(0.05f);
        RequestDrawCard();
        if (GameManager.Instance.IsGameOver) yield break;
        photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)nextTurn);
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
        EnsureInitialized();
        Card card = new Card(cardNumber, (ECardColor)color);
        var slot = Rounds[roundIndex].EnemyCardSlots[slotIndex];

        if (!slot.gameObject.activeSelf)
            slot.gameObject.SetActive(true);

        slot.Refresh(card);
        _enemyHandCount = Mathf.Max(0, _enemyHandCount - 1);
        // 상대 카드가 놓인 후에도 승패 판정
        GameManager.Instance.RecordStoneCompleted(roundIndex, LocalEnemy, Rounds[roundIndex].CountCards(false));
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
    /// <summary>
    /// 모든 경계석을 판정합니다. 양쪽 클라이언트가 같은 필드 정보로 각자 판정하며,
    /// 결과는 로컬 시점(1 = 나, 2 = 상대)으로 기록되고 경계석도 로컬 시점으로 이동합니다.
    /// (예전에는 마스터 시점의 owner를 RPC로 보내서 다른 클라이언트에서는 경계석이 반대로 움직였음)
    /// </summary>
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
                    string who = judgeResult.Winner == LocalMe ? "나" : "상대";
                    Debug.Log($"경계석 {roundIndex}: {who} 점령 ({judgeResult.Player1Rank} vs {judgeResult.Player2Rank})");
                }
            }

            int owner = GameManager.Instance.RoundOwners[roundIndex];
            Rounds[roundIndex].MoveStoneToOwner(owner);
            Rounds[roundIndex].RefreshSlotVisibility(owner != 0);
        }

        UpdateScore();
    }

    private void UpdateScore()
    {
        if (_hud == null || GameManager.Instance == null || GameManager.Instance.RoundOwners == null) return;
        var owners = GameManager.Instance.RoundOwners;
        _hud.SetScore(owners.Count(o => o == LocalMe), owners.Count(o => o == LocalEnemy), "나", EnemyNickName());
    }

    private void UpdateTurnHud()
    {
        if (_hud == null) return;
        if (!_matchStarted)
            _hud.SetTurn("상대를 기다리는 중", true);
        else if (GameManager.Instance.IsGameOver)
            _hud.SetTurn(null, false);
        else
        {
            bool mine = CurrentTurn == MyTurnValue;
            _hud.SetTurn(mine ? "내 차례" : "상대 차례", mine);
        }
    }

    private string EnemyNickName()
    {
        var other = PhotonNetwork.PlayerListOthers;
        return other != null && other.Length > 0 && !string.IsNullOrEmpty(other[0].NickName) ? other[0].NickName : "상대";
    }

    private bool HasLegalMove(bool mine)
    {
        bool hasCard = mine ? HandCardManagers[0].HandCardSlots.Any(s => s.HasCard) : _enemyHandCount > 0;
        if (!hasCard) return false;

        for (int r = 0; r < Rounds.Length; r++)
        {
            if (GameManager.Instance.IsStoneClaimed(r)) continue;
            if (Rounds[r].FirstEmptySlot(mine) != null) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // 게임 종료 / 재대결
    // ------------------------------------------------------------------

    private void HandleGameOver(int winner)
    {
        UpdateTurnHud();
        string title = winner == LocalMe ? "승리" : winner == LocalEnemy ? "패배" : "무승부";
        var owners = GameManager.Instance.RoundOwners;
        string sub = $"경계석 {owners.Count(o => o == LocalMe)} : {owners.Count(o => o == LocalEnemy)}";
        StartCoroutine(ShowResultDelayed(title, winner == LocalMe, sub));
    }

    private IEnumerator ShowResultDelayed(string title, bool isWin, string sub)
    {
        yield return new WaitForSeconds(1.0f);
        _hud.ShowResult(title, isWin, sub, "다시 하기", RequestRematch, "로비로", LeaveToLobby);
        RefreshRematchStatus();
    }

    private void RequestRematch()
    {
        photonView.RPC(nameof(RPC_RematchReady), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RPC_RematchReady(int actorNumber)
    {
        EnsureInitialized();
        _rematchReady.Add(actorNumber);
        RefreshRematchStatus();

        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2 &&
            PhotonNetwork.PlayerList.All(p => _rematchReady.Contains(p.ActorNumber)))
        {
            GameStart();
        }
    }

    private void RefreshRematchStatus()
    {
        if (_hud == null || !_hud.IsResultVisible) return;
        bool meReady = _rematchReady.Contains(PhotonNetwork.LocalPlayer.ActorNumber);
        bool otherReady = PhotonNetwork.PlayerListOthers.Any(p => _rematchReady.Contains(p.ActorNumber));

        if (meReady)
            _hud.SetResultStatus("상대의 응답을 기다리는 중...", false);
        else if (otherReady)
            _hud.SetResultStatus("상대가 재대결을 원합니다!", true);
        else
            _hud.SetResultStatus("", true);
    }

    private void LeaveToLobby()
    {
        // UI_Meta.OnLeftRoom 에서 로비 씬으로 이동
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        else UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        StopAllCoroutines();
        _matchStarted = false;
        _hud.SetTurn(null, false);
        _hud.ShowResult("게임 종료", false, "상대가 방을 나갔습니다", null, null, "로비로", LeaveToLobby);
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
        EnsureInitialized();
        CurrentTurn = (ETurn)turn;
        Debug.Log($"턴이 {CurrentTurn}으로 변경됨");
        if (RoundDicator != null) RoundDicator.Turn();
        UpdateTurnHud();

        if (GameManager.Instance.IsGameOver || !_dealt)
            return; // 아직 첫 패를 받기 전

        // 낼 수 있는 카드가 없으면 턴을 넘기고, 양쪽 모두 불가능하면 게임 종료 (양쪽 클라이언트가 동일하게 판정)
        bool myCan = HasLegalMove(true);
        bool enemyCan = HasLegalMove(false);
        if (!myCan && !enemyCan)
        {
            GameManager.Instance.EndByStalemate();
            return;
        }

        if (CurrentTurn == MyTurnValue && !myCan)
        {
            Debug.Log("낼 수 있는 카드가 없어 턴을 넘깁니다.");
            photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)GetNextTurn());
        }
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
        EnsureInitialized();
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
            _enemyHandCount++;
            if(slot.MyCard?.Rend != null)
            {
                slot.MyCard.Rend.enabled = true;
            }

        }
    }


    [PunRPC]
    private void RPC_UpdateSlotActivation()
    {
        for (int i = 0; i < Rounds.Length; i++)
            Rounds[i].RefreshSlotVisibility(GameManager.Instance.IsStoneClaimed(i));
    }

    [PunRPC]
    public void RPC_SyncDeck(int[] nums, int[] colors)
    {
        EnsureInitialized();
        CardDeck.SyncDeckFromData(nums, colors);
    }
    [PunRPC]
    public void RPC_MoveStone(int roundIndex, int owner)
    {
        Rounds[roundIndex].MoveStoneToOwner(owner);
    }

}
