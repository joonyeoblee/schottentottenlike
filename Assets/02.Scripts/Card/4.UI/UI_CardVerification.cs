using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class UI_CardVerification : MonoBehaviour
{
    public Transform Target;
private Vector3 _punchPower = new Vector3(0.2f, 0.2f,0);
 private float _punchTime = 0.3f;
 private int _punchVibrato = 30;
private Ease _ease = Ease.InOutElastic;


 private float _shakeDuration = 0.3f;
     private Vector3 _shakePower = new Vector3(0,0,0.2f);
     private int _shakeVibrato = 50;
     private Ease _shakeEase = Ease.InOutBounce;


    public void Shake()
    {
        Target.DOPunchScale(_shakePower, _punchTime, _shakeVibrato).SetEase(_ease);
        Target.DOShakeRotation(_shakeDuration, _shakePower, _shakeVibrato).SetEase(_shakeEase);
    }
}
