using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using UnityEngine;
// Action을 사용하기 위해 추가

// 턴을 구분하기 위한 Enum (V2에서 추가)
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

    // 현재 턴의 다음 턴을 반환하는 헬퍼 프로퍼티 (V2에서 추가)
    private ETurn GetNextTurn() => CurrentTurn == ETurn.Player1 ? ETurn.Player2 : ETurn.Player1;


    private void Start()
    {
        InitializeRoundSlots();
        InitializeHandCardSlots();
        CardDeck.OnCardSuffle += OnShuffled;
    }

    /// <summary>
    /// 게임 시작 또는 재시작 시 호출됩니다. (V2 로직 기반)
    /// </summary>
    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        ClearAllCardSlots(); // 모든 슬롯을 깨끗하게 초기화
        StartCoroutine(GameStartSequence());
    }

    /// <summary>
    /// 모든 카드 슬롯(라운드, 핸드)을 초기 상태로 되돌립니다. (V2에서 추가)
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
    /// 핸드 카드 슬롯의 인덱스 정보를 설정합니다. (V1의 안정적인 로직으로 수정)
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

                // V1 로직: 카드 렌더러를 미리 비활성화 (안정성 개선)
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

            // 첫 패 분배
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

        // 마스터에게: 마스터 손패를 0번, 클라 손패를 1번으로
        photonView.RPC(nameof(ReceiveFirstTurnCards), RpcTarget.MasterClient,
            masterColors, masterNumbers, clientColors, clientNumbers);

        // 클라이언트에게: 클라 손패를 0번, 마스터 손패를 1번으로
        photonView.RPC(nameof(ReceiveFirstTurnCards), RpcTarget.Others,
            clientColors, clientNumbers, masterColors, masterNumbers);
    }

    [PunRPC]
    private void ReceiveFirstTurnCards(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        StartCoroutine(DealFirstTurnCardsCoroutine(myColors, myNumbers, enemyColors, enemyNumbers));
    }

    /// <summary>
    /// 실제 카드를 나눠주는 코루틴 (V1의 애니메이션 로직 포함하여 병합)
    /// </summary>
    private IEnumerator DealFirstTurnCardsCoroutine(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        yield return new WaitForSeconds(0.5f);

        // 내 손패(0번) 분배
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            Card card = new Card(myNumbers[i], (ECardColor)myColors[i]);
            HandCardManagers[0].HandCardSlots[i].Refresh(i, card, true); // 무조건 보이게

            // 내 카드 드로우 애니메이션 실행 (V1 로직)
            DrawMyCardAnimation(i);

            yield return new WaitForSeconds(0.2f);
        }

        // 상대 손패(1번) 분배
        for (int i = 0; i < HandCardManagers[1].HandCardSlots.Length; i++)
        {
            Card card = new Card(enemyNumbers[i], (ECardColor)enemyColors[i]);
            HandCardManagers[1].HandCardSlots[i].Refresh(i, card, false); // 무조건 뒷면
            yield return new WaitForSeconds(0.2f);
        }

        // 상대 핸드 애니메이션 실행 (V1 로직)
        PlayEnemyHandAnimation();

        Debug.Log("손패 배분 완료 (로컬 기준)");
    }



    /// <summary>
    /// 내 카드가 필드에 놓였을 때 호출됩니다. (V2에서 추가)
    /// </summary>
    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        StartCoroutine(HandleCardPlacedSequence());
        JudgeAllRoundsWinner();
    }

    /// <summary>
    /// 카드 제출 후 처리 시퀀스 (V2에서 추가)
    /// </summary>
    private IEnumerator HandleCardPlacedSequence()
    {
        yield return new WaitForSeconds(0.05f);

        // 비어있는 내 손패에 카드 드로우 요청
        RequestDrawCard();

        // 턴 변경 RPC 호출
        ETurn nextTurn = GetNextTurn();
        photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)nextTurn);

        // 다음 슬롯 활성화 RPC 호출
        photonView.RPC(nameof(RPC_UpdateSlotActivation), RpcTarget.All);
    }

    /// <summary>
    /// 마스터에게 카드 드로우를 요청합니다. (V2에서 추가)
    /// </summary>
    private void RequestDrawCard()
    {
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            if (!HandCardManagers[0].HandCardSlots[i].HasCard)
            {
                // 로컬 플레이어의 ActorNumber와 빈 슬롯 인덱스를 전달
                photonView.RPC(nameof(RPC_RequestDrawCardFromMaster), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, i);
                break; // 한 번에 하나만 요청
            }
        }
    }

    /// <summary>
    /// 상대방이 제출한 카드를 내 필드에 놓습니다. (V2 로직 기반)
    /// </summary>
    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        var slot = Rounds[roundIndex].EnemyCardSlots[slotIndex];

        // 슬롯이 비활성화 상태라면 활성화
        if (!slot.gameObject.activeSelf)
            slot.gameObject.SetActive(true);

        slot.Refresh(card);
        // 상대 카드가 놓인 후에도 승패 판정
        JudgeAllRoundsWinner();
    }



    private void JudgeRoundWinner(int roundIndex)
    {
        var playerCards = GetCardsFromSlots(Rounds[roundIndex].PlayerCardSlots);
        var enemyCards = GetCardsFromSlots(Rounds[roundIndex].EnemyCardSlots);
        var unusedCards = GetUnusedCardsFromField();

        // 족보를 포함하여 판정
        var judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards);

        // 라운드 소유자 업데이트 및 게임 승리 조건 확인
        GameManager.Instance.UpdateRoundOwnerAndCheckWin(roundIndex, judgeResult.Winner);

        string playerRank = judgeResult.Player1Rank.ToString();
        string enemyRank = judgeResult.Player2Rank.ToString();
        string winnerMessage;

        switch (judgeResult.Winner)
        {
            case 1:
                winnerMessage = $"플레이어1 승리 ({playerRank} vs {enemyRank})";
                break;
            case -1:
                winnerMessage = $"플레이어2 승리 ({playerRank} vs {enemyRank})";
                break;
            default:
                winnerMessage = $"무승부 또는 미정 ({playerRank} vs {enemyRank})";
                break;
        }
        Debug.Log($"라운드 {roundIndex} 판정: {winnerMessage}");
    }
    private void JudgeAllRoundsWinner()
    {
        for (int roundIndex = 0; roundIndex < Rounds.Length; roundIndex++)
        {
            List<Card> playerCards = GetPlayerCards(roundIndex);
            List<Card> enemyCards = GetEnemyCards(roundIndex);
            List<Card> unusedCards = GetUnusedCardsFromField();

            JudgeResult judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards);

            GameManager.Instance.UpdateRoundOwnerAndCheckWin(roundIndex, judgeResult.Winner);

            string playerRank = judgeResult.Player1Rank.ToString();
            string enemyRank = judgeResult.Player2Rank.ToString();

            if (judgeResult.Winner == 1 || judgeResult.Winner == 2)
            {
                int owner = judgeResult.Winner == 1 ? 1 : 2;
                photonView.RPC(nameof(RPC_MoveStone), RpcTarget.All, roundIndex, owner);
            }

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

    private List<Card> GetCardsFromSlots(CardSlot[] slots)
    {
        return slots.Where(s => s.IsOccupied && s.Card != null).Select(s => s.Card).ToList();
    }

    /// <summary>
    /// 내 카드를 뽑는 애니메이션을 보여줍니다.
    /// </summary>
    private void DrawMyCardAnimation(int handSlotIndex)
    {
        var slot = HandCardManagers[0].HandCardSlots[handSlotIndex];
        if (slot.MyCard == null) return;

        // 애니메이션 중간 지점 설정
        HandCardManagers[0].HandCardSlots[handSlotIndex].MyCard.ShowAnimation.midPoint =
            AnimationTransforms.Instance.FirstShowTransfroms[handSlotIndex];
        HandCardManagers[0].HandCardSlots[handSlotIndex].MyCard.ShowDraw();
        slot.MyCard.Rend.enabled = true;
    }

    /// <summary>
    /// 상대 핸드카드가 펼쳐지는 애니메이션을 재생합니다.
    /// </summary>
    private void PlayEnemyHandAnimation()
    {
        var enemyAnimation = HandCardManagers[1].GetComponent<EnemyHandAnimation>();
        if (enemyAnimation == null) return;
        // 2. 애니메이션이 끝나면 모든 적 카드의 렌더러를 켭니다.
        foreach (var enemyCardSlot in HandCardManagers[1].HandCardSlots)
        {
            if (enemyCardSlot?.MyCard?.Rend != null)
            {
                enemyCardSlot.MyCard.Rend.enabled = true;
            }
        }
        // 1. 카드를 모으는 애니메이션을 먼저 재생 (FanIn)
        enemyAnimation.PlayFanInAnimation();
    }



    [PunRPC]
    public void SetTurn(int turn)
    {
        CurrentTurn = (ETurn)turn;
        Debug.Log($"턴이 {CurrentTurn}으로 변경됨");
        // TODO: 현재 턴에 따라 UI 업데이트 (예: '내 턴' 표시)
    }

    /// <summary>
    /// 마스터 클라이언트가 카드 드로우 요청을 받아 처리합니다. (V2에서 추가)
    /// </summary>
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

        // 모든 클라이언트에게 뽑은 카드 정보를 전송
        photonView.RPC(nameof(RPC_ReceiveDrawCard), RpcTarget.All, targetActorNumber, slotIndex, card.CardNumber, (int)card.Color);
    }

    /// <summary>
    /// 모든 클라이언트가 드로우된 카드를 자신의 핸드에 적용합니다. (V2에서 추가)
    /// </summary>
    [PunRPC]
    private void RPC_ReceiveDrawCard(int targetActorNumber, int slotIndex, int cardNumber, int color)
    {
        bool isMine = PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber;
        int handManagerIndex = isMine ? 0 : 1; // 내 것이면 0번, 상대 것이면 1번 매니저

        Card card = new Card(cardNumber, (ECardColor)color);
        var slot = HandCardManagers[handManagerIndex].HandCardSlots[slotIndex];
        slot.Refresh(slotIndex, card, isMine);

        if (isMine)
        {
            // 내가 뽑은 카드일 경우 애니메이션 실행
            DrawMyCardAnimation(slotIndex);
        }
        else
        {
            // 상대가 뽑은 카드 처리 (예: 뒷면으로 렌더러만 켜기)
            if(slot.MyCard?.Rend != null)
            {
                slot.MyCard.Rend.enabled = true;
            }
        }
    }

    /// <summary>
    /// 카드가 놓인 후 다음 놓을 수 있는 슬롯을 활성화합니다. (V2에서 추가)
    /// </summary>
    [PunRPC]
    private void RPC_UpdateSlotActivation()
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

    /// <summary>
    /// 늦게 접속한 클라이언트를 위해 덱 상태를 동기화합니다.
    /// </summary>
    [PunRPC]
    public void RPC_SyncDeck(int[] nums, int[] colors)
    {
        CardDeck.SyncDeckFromData(nums, colors);
    }
    [PunRPC]
    public void RPC_MoveStone(int roundIndex, int owner)
    {
        Rounds[roundIndex].MoveStoneToOwner(owner);
    }

}
