using UnityEngine;

public class RoundDicator : MonoBehaviour
{
    [SerializeField] private Texture2D MyTurn;
    [SerializeField] private Texture2D EnemyTurn;

    [SerializeField] private Vector3 MyTurnRotation;
    [SerializeField] private Vector3 EnemyTurnRotaion;

    public bool IsMyTurn;

    public void Turn(int i)
    {
        //i가 1이면 적, 0이면 우리
        Debug.Log($"Turn {i}");

        this.transform.localRotation = IsMyTurn ? MyTurnRotation : EnemyTurnRotaion;
    }

}
