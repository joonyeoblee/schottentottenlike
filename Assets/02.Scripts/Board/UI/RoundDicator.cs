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
        // 차례 판정은 BattleField 기준 (방장 교체에도 바뀌지 않는 ActorNumber 기준)
        var bf = BattleField.Instance;
        if (bf == null) return;
        bool isMyTurn = bf.CurrentTurn == bf.MyTurn;
        IsMyTurn = isMyTurn;
        Vector3 targetRotation = isMyTurn ? MyTurnRotation : EnemyTurnRotaion;
        transform.localRotation = Quaternion.Euler(targetRotation);
    }
}
