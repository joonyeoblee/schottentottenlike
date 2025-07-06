using System;
using UnityEngine;

public class CardDrawTestScripts : MonoBehaviour
{
    public GameObject CardPrefab;
    public Transform DrawPoint;

    public HandCardManager HandCardManager;
    public EnemyHandDrawAnimation EnemyHandDrawAnimation;
    public void Draw(int i)
    {
        HandCardManager.HandCardSlots[i].MyCard.ShowAnimation.midPoint =
            AnimationTransforms.Instance.FirstShowTransfroms[i];
        HandCardManager.HandCardSlots[i].MyCard.ShowDraw();
        HandCardManager.HandCardSlots[i].MyCard.Rend.enabled = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Draw(0);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Draw(1);

        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Draw(2);

        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            Draw(3);

        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            Draw(4);

        }

    }
}
