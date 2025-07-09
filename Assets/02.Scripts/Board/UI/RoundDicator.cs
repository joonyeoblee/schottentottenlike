using Photon.Pun;
using UnityEngine;

public class RoundDicator : MonoBehaviour
{
    [SerializeField] private Texture2D MyTurn;
    [SerializeField] private Texture2D EnemyTurn;

    [SerializeField] private Vector3 MyTurnRotation;
    [SerializeField] private Vector3 EnemyTurnRotaion;

    public bool IsMyTurn;

    public void Turn()
    {
        bool isMaster = PhotonNetwork.IsMasterClient;
        ETurn currentTurn = BattleField.Instance.CurrentTurn;
        bool isMyTurn = isMaster && currentTurn == ETurn.Player1 || !isMaster && currentTurn == ETurn.Player2;
        Vector3 targetRotation = isMyTurn ? MyTurnRotation : EnemyTurnRotaion;
        transform.localRotation = Quaternion.Euler(targetRotation);    }

}
