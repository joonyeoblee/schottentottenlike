using System;
using Photon.Pun;
using UnityEngine;
public class BattleField : MonoBehaviour
{
    public RoundSlot[] Rounds;
    public PhotonView PhotonView;

    private void Start()
    {
        foreach (var round in Rounds)
        {
            foreach (var playerSlot in round.PlayerCardSlots)
            {
                playerSlot.IsMine = true;
            }
            foreach (var enemySlot in round.EnemyCardSlots)
            {
                enemySlot.IsMine = false;
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
