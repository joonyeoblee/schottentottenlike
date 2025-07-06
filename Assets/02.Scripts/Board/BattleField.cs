using System.Collections;
using Photon.Pun;
using UnityEngine;

public enum ETurn
{
    Player1 = 0,
    Player2 = 1
}

public class BattleField : SingletonPhoton<BattleField>
{
    public HandCardManager[] HandCardManagers; // [0] = 내 카드, [1] = 상대 카드
    public RoundSlot[] Rounds;
    public CardDeck CardDeck;
    public ETurn CurrentTurn;
    private bool _isShuffled;

    private ETurn GetNextTurn()
    {
        return CurrentTurn == ETurn.Player1 ? ETurn.Player2 : ETurn.Player1;
    }

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
            Rounds[i].Index = i;

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
            }
            for (int j = 0; j < Rounds[i].PlayerCardSlots.Length; j++)
            {
                var slot = Rounds[i].PlayerCardSlots[j];
                slot.IsMine = true;
                slot.Index = j;
                slot.RoundIndex = i;

                // 첫 슬롯만 활성화, 나머지는 비활성화
                slot.gameObject.SetActive(j == 0);
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
        {
            int rand = Random.Range(0, 2); // 마스터 클라이언트만 턴 결정
            photonView.RPC(nameof(SetTurn), RpcTarget.All, rand);
            SendFirstTurnDealToAll(); // 카드 뽑고 전송
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
    public void SetTurn(int turn)
    {
        CurrentTurn = (ETurn)turn;
        Debug.Log($"턴이 {CurrentTurn}으로 변경됨");
    }

    [PunRPC]
    private void ReceiveFirstTurnCards(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        StartCoroutine(DealFirstTurnCardsCoroutine(myColors, myNumbers, enemyColors, enemyNumbers));
    }


    private IEnumerator DealFirstTurnCardsCoroutine(int[] myColors, int[] myNumbers, int[] enemyColors, int[] enemyNumbers)
    {
        yield return new WaitForSeconds(0.5f);

        // 내 손패: 0번
        for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
        {
            Card card = new Card(myNumbers[i], (ECardColor)myColors[i]);
            HandCardManagers[0].HandCardSlots[i].Refresh(i, card, true); // 무조건 보이게
            yield return new WaitForSeconds(0.2f);
        }

        // 상대 손패: 1번
        for (int i = 0; i < HandCardManagers[1].HandCardSlots.Length; i++)
        {
            Card card = new Card(enemyNumbers[i], (ECardColor)enemyColors[i]);
            HandCardManagers[1].HandCardSlots[i].Refresh(i, card, false); // 무조건 뒷면
            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log("손패 배분 완료 (로컬 기준)");
    }


    public void OnMyCardPlaced(CardSlot placedSlot)
    {
        StartCoroutine(HandleCardPlaced());
    }

    private IEnumerator HandleCardPlaced()
    {
        yield return new WaitForSeconds(0.05f); // 카드가 핸드에서 제거되도록 잠시 대기

        DrawCardToHand();

        ETurn nextTurn = GetNextTurn();
        photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)nextTurn);

        photonView.RPC(nameof(RPC_UpdateSlotActivation), RpcTarget.All);
    }


    private void DrawCardToHand()
    {
        // 내 핸드에서 비어있는 슬롯 찾기
        var hand = HandCardManagers[0]; // 항상 내 핸드
        for (int i = 0; i < hand.HandCardSlots.Length; i++)
        {
            if (!hand.HandCardSlots[i].HasCard) // 예시: HasCard는 bool 프로퍼티로 구현 필요
            {
                var card = CardDeck.GetCard();

                if (card == null)
                {
                    Debug.LogWarning("핸드에 추가할 카드가 없습니다.");
                    return;
                }

                hand.HandCardSlots[i].Refresh(i, card, true); // 보이게
                break;
            }
        }
    }
    [PunRPC]
    private void RPC_UpdateSlotActivation()
    {
        foreach (var round in Rounds)
        {
            foreach (var slot in round.PlayerCardSlots)
            {
                int i = slot.Index;
                var slots = round.PlayerCardSlots;

                if (i < slots.Length - 1 && slots[i].IsOccupied && !slots[i + 1].gameObject.activeSelf)
                {
                    slots[i + 1].gameObject.SetActive(true);
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
        {
            slot.gameObject.SetActive(true); // 반드시 필요
        }

        slot.Refresh(card);
    }
    [PunRPC]
    public void RPC_SyncDeck(int[] nums, int[] colors)
    {
        CardDeck.SyncDeckFromData(nums, colors);
    }
}
