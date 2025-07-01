using Photon.Pun;
using UnityEngine;
public class BattleField : MonoBehaviour
{
    public HandCardManager[] HandCardManagers; // 카드 각각 넣어야 함
    public RoundSlot[] Rounds;
    public PhotonView PhotonView;
    public CardDeck CardDeck;
    
    private void Start()
    {
        int i = 0;
        foreach (RoundSlot round in Rounds)
        {
            round.index = i++;

            int j = 0;
            foreach (CardSlot playerSlot in round.PlayerCardSlots)
            {
                playerSlot.IsMine = true;
                playerSlot.Index = j++;
                playerSlot.RoundIndex = round.index;
            }

            j = 0;
            foreach (CardSlot enemySlot in round.EnemyCardSlots)
            {
                enemySlot.IsMine = false;
                enemySlot.Index = j++;
                enemySlot.RoundIndex = round.index;
            }
        }

        foreach (HandCardManager handCardManager in HandCardManagers)
        {
            handCardManager.Index = i++;
            int j = 0;
            foreach (HandCardSlot playerHandCardSlot in handCardManager.HandCardSlots)
            {
                playerHandCardSlot.IsMine = true;
                playerHandCardSlot.Index = j++;
                playerHandCardSlot.HandCardIndex = handCardManager.Index;
            }
        }

        CardDeck.OnCardSuffle += GameStart;
    }

    public void GameStart()
    {
        int i = 0;
        foreach (HandCardManager handCardManager in HandCardManagers)
        {
            foreach (var HandCardSlot in handCardManager.HandCardSlots)
            {
                HandCardSlot.Refresh(i++,CardDeck.GetCard());
            }
        }
    }

    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        // 상대방 입장에서 Enemy 슬롯에 추가
        Rounds[roundIndex].EnemyCardSlots[slotIndex].Refresh(card);
    }

    [PunRPC]
    public void RemoveHandCard(int handCardManagerIndex, int handCardIndex)
    {
        //HandCardSlots[handCardIndex].IsMine = false;
        // HandCardManagers[handCardIndex].EnemyHandSlots[handCardIndex].Refresh(handCardIndex);
        // HandCardManagers[handCardMnagerIndex].EnemyHandSlots[handCardIndex].Refresh(handCardIndex);
        // 상대방 입장에서 EnemyHandSlots 배열에서 카드 제거
        if (handCardManagerIndex < HandCardManagers.Length)
        {
            HandCardManagers[handCardManagerIndex].HandCardSlots[handCardIndex].Refresh(handCardIndex,null);
            // if (targetSlot != null)
            // {
            //     targetSlot.RemoveCard(); // 카드 제거
            //     Debug.Log($"[BattleField] 상대방 {handCardManagerIndex}-{handCardIndex} 카드 제거 RPC 수신 완료");
            // }
            // else
            // {
            //     Debug.LogWarning($"[BattleField] 대상 슬롯이 null임");
            // }
        }
    }
}
