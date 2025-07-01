using Photon.Pun;
using UnityEngine;
public class BattleField : MonoBehaviour
{
    public RoundSlot[] Rounds;
    public PhotonView PhotonView;

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
    }

    [PunRPC]
    public void SetCard(int roundIndex, int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        // 상대방 입장에서 Enemy 슬롯에 추가
        Rounds[roundIndex].EnemyCardSlots[slotIndex].Refresh(card);
    }
}
