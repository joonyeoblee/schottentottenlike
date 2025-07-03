using UnityEngine;

public class CardDrawTestScripts : MonoBehaviour
{
    public GameObject CardPrefab;
    public Transform DrawPoint;
    public void Draw()
    {
        Instantiate(CardPrefab, DrawPoint);
    }
}
