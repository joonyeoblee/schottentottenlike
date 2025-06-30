using System;
using Photon.Pun;
using UnityEngine;
public class BattleField : MonoBehaviour
{
    public CardSlot[] PlayerCardSlots;
    public CardSlot[] EnemyCardSlots;
    public PhotonView PhotonView;

    private void Start()
    {
        for (int i = 0; i < PlayerCardSlots.Length; i++)
        {
            // 예시
            PlayerCardSlots[i].IsMine = true;
            EnemyCardSlots[i].IsMine = false;
        }
    }

    [PunRPC]
    public void SetCard(int slotIndex, int cardNumber, int color)
    {
        Card card = new Card(cardNumber, (ECardColor)color);
        // 상대방 입장에서 Enemy 슬롯에 추가
        EnemyCardSlots[slotIndex].Refresh(card);
    }
}
