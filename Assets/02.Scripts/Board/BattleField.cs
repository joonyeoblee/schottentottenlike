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

        private ETurn GetNextTurn() => CurrentTurn == ETurn.Player1 ? ETurn.Player2 : ETurn.Player1;

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

                    slot.gameObject.SetActive(j == 0);
                }
            }
        }

        public void GameStart()
        {
            Debug.Log("GameStart 호출됨 - 덱 셔플 시작");
            ClearAllCardSlots();
            StartCoroutine(GameStartSequence());
            InitializeRoundSlots();
        }

        private IEnumerator GameStartSequence()
        {
            CardDeck.StartDeckSuffle();
            yield return new WaitUntil(() => _isShuffled);
            yield return new WaitForSeconds(0.5f);

            if (PhotonNetwork.IsMasterClient)
            {
                int rand = Random.Range(0, 2);
                photonView.RPC(nameof(SetTurn), RpcTarget.All, rand);
                SendFirstTurnDealToAll();
            }
        }
        private void ClearAllCardSlots()
        {
            foreach (var round in Rounds)
            {
                for (int i = 0; i < round.PlayerCardSlots.Length; i++)
                {
                    var slot = round.PlayerCardSlots[i];
                    slot.Clear();
                    slot.gameObject.SetActive(i == 0); // 0번째만 true, 나머지 false
                }
                for (int i = 0; i < round.EnemyCardSlots.Length; i++)
                {
                    var slot = round.EnemyCardSlots[i];
                    slot.Clear();
                    slot.gameObject.SetActive(i == 0); // 0번째만 true, 나머지 false
                }

            }

            foreach (var handManager in HandCardManagers)
            {
                foreach (var handSlot in handManager.HandCardSlots)
                {
                    handSlot.Clear();
                }
            }

            Debug.Log("모든 슬롯 초기화 완료: Player 0번째만 활성화됨");
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

            for (int i = 0; i < HandCardManagers[0].HandCardSlots.Length; i++)
            {
                Card card = new Card(myNumbers[i], (ECardColor)myColors[i]);
                HandCardManagers[0].HandCardSlots[i].Refresh(i, card, true);
                yield return new WaitForSeconds(0.2f);
            }

            for (int i = 0; i < HandCardManagers[1].HandCardSlots.Length; i++)
            {
                Card card = new Card(enemyNumbers[i], (ECardColor)enemyColors[i]);
                HandCardManagers[1].HandCardSlots[i].Refresh(i, card, false);
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
            yield return new WaitForSeconds(0.05f);

            RequestDrawCard();

            ETurn nextTurn = GetNextTurn();
            photonView.RPC(nameof(SetTurn), RpcTarget.All, (int)nextTurn);

            photonView.RPC(nameof(RPC_UpdateSlotActivation), RpcTarget.All);
        }

        // 변경됨: 마스터에게 카드 요청
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

        // 마스터가 카드 뽑고 카드 정보 전송
        [PunRPC]
        private void RPC_RequestDrawCard(int targetActorNumber, int slotIndex)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Card card = CardDeck.GetCard();
            if (card == null)
            {
                Debug.LogWarning("덱에서 더 이상 뽑을 카드가 없음.");
                return;
            }

            photonView.RPC(nameof(RPC_ReceiveDrawCard), RpcTarget.All, targetActorNumber, slotIndex, card.CardNumber, (int)card.Color);
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
        }

        [PunRPC]
        public void RPC_SyncDeck(int[] nums, int[] colors)
        {
            CardDeck.SyncDeckFromData(nums, colors);
        }
    }
