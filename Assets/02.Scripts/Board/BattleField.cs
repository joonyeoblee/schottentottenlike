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





    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        Rounds[roundIndex].EnemyCardSlots[slotIndex].Refresh(card);
    }

    [PunRPC]
    public void RPC_SyncDeck(int[] nums, int[] colors)
    {
        CardDeck.SyncDeckFromData(nums, colors);
    }
}
