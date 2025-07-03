using System;
using UnityEngine;

public class CardDrawTestScripts : MonoBehaviour
{
    public GameObject CardPrefab;
    public Transform DrawPoint;
    public void Draw()
    {
        Instantiate(CardPrefab, DrawPoint);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Draw();
        }
    }
}
