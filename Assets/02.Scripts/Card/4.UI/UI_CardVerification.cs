using DG.Tweening;
using UnityEngine;

public class UI_CardVerification : MonoBehaviour
{
    // 흔들 대상만 지정해주면 됩니다.
    public Transform Target;

    // 인스펙터에서 테스트용으로 실행할 수 있는 메뉴
    [ContextMenu("Execute Shake")]
    public void Shake()
    {
        if (Target == null)
        {
            Debug.LogError("Target is not assigned!");
            return;
        }

        // AnimationTransforms 싱글톤 인스턴스에서 직접 값을 가져옵니다.
        var animManager = AnimationTransforms.Instance;

        if (animManager == null)
        {
            Debug.LogError("AnimationTransforms.Instance is not found in the scene!");
            return;
        }

        // DOTween 시퀀스를 사용하여 두 애니메이션을 동시에 실행
        Sequence sequence = DOTween.Sequence();

        // AnimationTransforms에 정의된 Punch 값들을 사용
        sequence.Join(Target.DOPunchScale(
                animManager.CardPunchPower,
                animManager.CardPunchTime,
                animManager.CardPunchVibrato)
            .SetEase(animManager.CardPunchEase));

        // AnimationTransforms에 정의된 Shake 값들을 사용
        sequence.Join(Target.DOShakeRotation(
                animManager.CardShakeDuration,
                animManager.CardShakePower,
                animManager.CardShakeVibrato)
            .SetEase(animManager.CardShakeEase));
    }
}
