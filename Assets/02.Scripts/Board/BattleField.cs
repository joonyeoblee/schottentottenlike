using System.Collections;
using Photon.Pun;
using UnityEngine;
public class BattleField : SingletonPhoton<BattleField>
{
    public HandCardManager[] HandCardManagers; // [0] = 내 카드, [1] = 상대 카드
    public RoundSlot[] Rounds;
    public CardDeck CardDeck;

    private bool _isShuffled;

    private void Start()
    {
        InitializeRoundSlots();
        InitializeHandCardSlots();
        CardDeck.OnCardSuffle += OnShuffled;
    }

    private void InitializeRoundSlots()
    {
        for (int i = 0; i < Rounds.Length; i++)
        {
            Rounds[i].index = i;

            for (int j = 0; j < Rounds[i].PlayerCardSlots.Length; j++)
            {
                Rounds[i].PlayerCardSlots[j].IsMine = true;
                Rounds[i].PlayerCardSlots[j].Index = j;
                Rounds[i].PlayerCardSlots[j].RoundIndex = i;
            }

            for (int j = 0; j < Rounds[i].EnemyCardSlots.Length; j++)
            {
                Rounds[i].EnemyCardSlots[j].IsMine = false;
                Rounds[i].EnemyCardSlots[j].Index = j;
                Rounds[i].EnemyCardSlots[j].RoundIndex = i;
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
                HandCardManagers[i].HandCardSlots[j].IsMine = true;
                HandCardManagers[i].HandCardSlots[j].Index = j;
                HandCardManagers[i].HandCardSlots[j].HandCardIndex = i;
                if(HandCardManagers[i].HandCardSlots[j].MyCard == null)Debug.LogWarning("내 가진 카드가 없소");
                if(HandCardManagers[i].HandCardSlots[j].MyCard.Rend == null)Debug.LogWarning("내 가진 렌더러가 없소");

                HandCardManagers[i].HandCardSlots[j].MyCard.Rend.enabled = false;
            }
        }
    }

    public void GameStart()
    {
        Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
        StartCoroutine(GameStartSequence());
    }

    private IEnumerator GameStartSequence()
    {
        CardDeck.StartDeckSuffle();
        yield return new WaitUntil(() => _isShuffled);
        yield return new WaitForSeconds(0.5f); // 셔플 후 애니메이션 딜레이

        if (PhotonNetwork.IsMasterClient)
            SendFirstTurnDealToAll(); // 카드 뽑고 전송
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

    private void Draw(int i)
    {
        HandCardManagers[0].HandCardSlots[i].MyCard.ShowAnimation.midPoint =
            AnimationTransforms.Instance.FirstShowTransfroms[i];
        HandCardManagers[0].HandCardSlots[i].MyCard.ShowDraw();
        HandCardManagers[0].HandCardSlots[i].MyCard.Rend.enabled = true;


    }

    private IEnumerator DealFirstTurnCardsCoroutine(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        yield return new WaitForSeconds(0.5f);

        // 내 손패: 0번
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            Card card = new Card(myNumbers[i], (ECardColor)myColors[i]);

            HandCardManagers[0].HandCardSlots[i].Refresh(i, card, true); // 무조건 보이게

            Draw(i);


            yield return new WaitForSeconds(0.2f);
        }

        // 상대 손패: 1번
        for (int i = 0; i < HandCardManagers[1].HandCardSlots.Length; i++)
        {
            Card card = new Card(enemyNumbers[i], (ECardColor)enemyColors[i]);
            HandCardManagers[1].HandCardSlots[i].Refresh(i, card, false); // 무조건 뒷면

            yield return new WaitForSeconds(0.2f);
        }

        var enemyAnimation = HandCardManagers[1].GetComponent<EnemyHandAnimation>();
        ;

        // 1. "FanIn" 애니메이션을 시작합니다.
        enemyAnimation.PlayFanOutAnimation(() =>
        {

            // 2. 원하는 작업: 모든 적 카드의 렌더러를 켭니다.
            foreach (var enemyCardSlot in HandCardManagers[1].HandCardSlots)
            {
                if (enemyCardSlot != null && enemyCardSlot.MyCard != null && enemyCardSlot.MyCard.Rend != null)
                {
                    enemyCardSlot.MyCard.Rend.enabled = true;
                }
            }

            // 3. 이전 작업이 모두 끝났으면, "FanOut" 애니메이션을 시작합니다.
            enemyAnimation.PlayFanInAnimation();
        });

        Debug.Log("손패 배분 완료 (로컬 기준)");
    }


    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        StartCoroutine(HandleCardPlaced());
        JudgeAllRoundsWinner();
    }

    private IEnumerator HandleCardPlaced()
    {
        yield return new WaitForSeconds(0.05f);

        RequestDrawCard();

        ETurn nextTurn = GetNextTurn();
        photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)nextTurn);

        photonView.RPC(nameof(RPC_UpdateSlotActivation), RpcTarget.All);
    }

    private void RequestDrawCard()
    {
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            if (!HandCardManagers[0].HandCardSlots[i].HasCard)
            {
                photonView.RPC(nameof(RPC_RequestDrawCard), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, i);
                break;
            }
        }
    }

    //족보 X
    //private void JudgeRoundWinner(int roundIndex)
    //{
    //    var playerCards = GetPlayerCards(roundIndex); // Rounds[roundIndex].PlayerCardSlots에서 추출
    //    var enemyCards = GetEnemyCards(roundIndex);   // Rounds[roundIndex].EnemyCardSlots에서 추출
    //    // 미사용 카드 리스트도 BattleField에서 만들어 전달
    //    var unusedCards = GetUnusedCardsFromField();

    //    int result = GameManager.Instance.JudgeStone(playerCards, enemyCards, unusedCards);

    //    if (result == 1)
    //    {
    //        Debug.Log($"라운드 {roundIndex}: 플레이어1 승리");
    //    }
    //    else if (result == -1)
    //    {
    //        Debug.Log($"라운드 {roundIndex}: 플레이어2 승리");
    //    }
    //    else
    //    {
    //        Debug.Log($"라운드 {roundIndex}: 무승부 또는 미정");
    //    }
    //}

    //족보 O
    private void JudgeRoundWinner(int roundIndex)
    {
        var playerCards = GetPlayerCards(roundIndex);
        var enemyCards = GetEnemyCards(roundIndex);
        var unusedCards = GetUnusedCardsFromField();

        var judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards);

        GameManager.Instance.UpdateRoundOwnerAndCheckWin(roundIndex, judgeResult.Winner);


        string playerRank = judgeResult.Player1Rank.ToString();
        string enemyRank = judgeResult.Player2Rank.ToString();

        if (judgeResult.Winner == 1)
        {
            Debug.Log($"라운드 {roundIndex}: 플레이어1 승리 ({playerRank} vs {enemyRank})");
        }
        else if (judgeResult.Winner == -1)
        {
            Debug.Log($"라운드 {roundIndex}: 플레이어2 승리 ({playerRank} vs {enemyRank})");
        }
        else
        {
            Debug.Log($"라운드 {roundIndex}: 무승부 또는 미정 ({playerRank} vs {enemyRank})");
        }
    }

    private void JudgeAllRoundsWinner()
    {
        for (int roundIndex = 0; roundIndex < Rounds.Length; roundIndex++)
        {
            var playerCards = GetPlayerCards(roundIndex);
            var enemyCards = GetEnemyCards(roundIndex);
            var unusedCards = GetUnusedCardsFromField();

            var judgeResult = GameManager.Instance.JudgeStoneWithRank(playerCards, enemyCards, unusedCards);

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
        LogAllRoundOwners();
    }



    private List<Card> GetPlayerCards(int roundIndex)
    {
        var slots = Rounds[roundIndex].PlayerCardSlots;
        var cards = new List<Card>();
        foreach (var slot in slots)
            if (slot.IsOccupied && slot.Card != null)
                cards.Add(slot.Card);
        return cards;
    }

    private List<Card> GetEnemyCards(int roundIndex)
    {
        var slots = Rounds[roundIndex].EnemyCardSlots;
        var cards = new List<Card>();
        foreach (var slot in slots)
            if (slot.IsOccupied && slot.Card != null)
                cards.Add(slot.Card);
        return cards;
    }

    private List<Card> GetUnusedCardsFromField()
    {
        var used = new List<Card>();
        foreach (var round in Rounds)
        {
            used.AddRange(round.PlayerCardSlots.Where(s => s.IsOccupied && s.Card != null).Select(s => s.Card));
            used.AddRange(round.EnemyCardSlots.Where(s => s.IsOccupied && s.Card != null).Select(s => s.Card));
        }

        var all = GameManager.Instance.GetAllPossibleCards().ToList();
        return all.Where(c => !used.Any(u => u.CardNumber == c.CardNumber && u.Color == c.Color)).ToList();
    }




    // 모든 유저가 자신의 핸드에 카드 적용
    [PunRPC]
    private void RPC_ReceiveDrawCard(int targetActorNumber, int slotIndex, int cardNumber, int color)
    {
        bool isMine = PhotonNetwork.LocalPlayer.ActorNumber == targetActorNumber;
        int handIndex = isMine ? 0 : 1;

        Card card = new Card(cardNumber, (ECardColor)color);
        HandCardManagers[handIndex].HandCardSlots[slotIndex].Refresh(slotIndex, card, isMine);
    }

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


    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        var slot = Rounds[roundIndex].EnemyCardSlots[slotIndex];

        if (!slot.gameObject.activeSelf)
            slot.gameObject.SetActive(true);

        slot.Refresh(card);
        JudgeAllRoundsWinner();
    }
    private void LogAllRoundOwners()
    {
        var owners = GameManager.Instance.RoundOwners;
        string log = "[소유된 라운드 현황] ";
        bool hasOwner = false;
        for (int i = 0; i < owners.Length; i++)
        {
            string ownerStr = owners[i] switch
            {
                1 => "플레이어1",
                2 => "플레이어2",
                _ => null
            };
            if (ownerStr != null)
            {
                log += $"[{i}:{ownerStr}] ";
                hasOwner = true;
            }
        }
        if (hasOwner)
            Debug.Log(log);
        else
            Debug.Log("[소유된 라운드 없음]");
        Rounds[roundIndex].EnemyCardSlots[slotIndex].Refresh(card);
    }

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
