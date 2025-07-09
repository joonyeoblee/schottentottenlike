using DG.Tweening;
using UnityEngine;

public class AnimationTransforms : MonoBehaviour
{
    public static AnimationTransforms Instance;

    [Header("===== Object Transforms =====")]
    public Transform[] FirstShowTransfroms;
    public Transform ShowTransfrom;
    public Transform DeckTransfrom;
    public Transform PlayerHandTransform;
    public Transform EnemyHandTransform;
    public Transform EnemyShowTransform;
    public Transform EnemyResetTransfrom;

    [Header("===== Card Shake Animation Values =====")]
    [Header("Punch Scale Settings")]
    public Vector3 CardPunchPower = new Vector3(0.2f, 0.2f, 0);
    public float CardPunchTime = 0.3f;
    public int CardPunchVibrato = 30;
    public Ease CardPunchEase = Ease.InOutElastic;

    [Header("Shake Rotation Settings")]
    public float CardShakeDuration = 0.3f;
    public Vector3 CardShakePower = new Vector3(0, 0, 20f);
    public int CardShakeVibrato = 50;
    public Ease CardShakeEase = Ease.InOutBounce;

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
