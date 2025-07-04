using UnityEngine;

public class AnimationTransforms : MonoBehaviour
{
    public static AnimationTransforms Instance;

    public Transform[] FirstShowTransfroms;
    public Transform ShowTransfrom;

    public Transform DeckTransfrom;

    public Transform PlayerHandTransform;
    public Transform EnemyHandTransform;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }




}
