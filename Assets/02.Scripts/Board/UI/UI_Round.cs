using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using DG.Tweening;

public enum RoundWinner
{
    Player,
    Enemy
}

public class UI_Round : MonoBehaviour
{
    [Header("결과 텍스트")]
    public TextMeshProUGUI EnemyResultText;
    public string EnemyResult;
    public TextMeshProUGUI MyResultText;
    public string MyResult;

    [Header("애니메이션 관련")]
    public Transform[] ShowTransforms; // [0,1,2] = Enemy, [3,4,5] = Player
    public Transform Stone;

    [Header("애니메이션 설정 값")]
    [SerializeField] private float _moveDuration = 0.4f;
    [SerializeField] private float _cardShowScaleMultiplier = 1.1f;
    [SerializeField] private float _shakeDuration = 0.3f;
    [SerializeField] private float _shakeStrength = 10f;
    [SerializeField] private float _interval = 0.15f;
    [SerializeField] private float _textFadeInDuration = 0.4f;

    [Header("승리 연출 (돌)")]
    [SerializeField] private float _stoneAnimDuration = 0.8f;
    [SerializeField] private float _stoneScaleMultiplier = 1.5f;
    [SerializeField] private float _stoneFloatHeight = 1.0f;

    public void PlayVerificationAnimation(RoundSlot roundSlot, RoundWinner winner, Action callback = null)
    {
        // --- 초기 위치 및 크기 저장 (슬롯 기준) ---
        var originalPositions = new Dictionary<Transform, Vector3>();
        var originalScales = new Dictionary<Transform, Vector3>();
        var allSlots = roundSlot.EnemyCardSlots.Concat(roundSlot.PlayerCardSlots);
        foreach (var slot in allSlots)
        {
            originalPositions[slot.transform] = slot.transform.position;
            originalScales[slot.transform] = slot.transform.localScale;
        }
        Vector3 stoneOriginalScale = Stone.localScale;

        Sequence mainSequence = DOTween.Sequence();

        EnemyResultText.alpha = 0;
        MyResultText.alpha = 0;

        // --- 1. 슬롯들을 순차적으로 중앙(ShowTransforms)으로 이동 및 확대 ---
        // Enemy 슬롯 이동
        for (int i = 0; i < roundSlot.EnemyCardSlots.Length; i++)
        {
            var slot = roundSlot.EnemyCardSlots[i];
            Vector3 originalScale = originalScales[slot.transform];
            mainSequence.Append(slot.transform.DOMove(ShowTransforms[i].position, _moveDuration).SetEase(Ease.OutQuad))
                        .Join(slot.transform.DOScale(originalScale * _cardShowScaleMultiplier, _moveDuration));
        }
        // Player 슬롯 이동
        for (int i = 0; i < roundSlot.PlayerCardSlots.Length; i++)
        {
            var slot = roundSlot.PlayerCardSlots[i];
            Vector3 originalScale = originalScales[slot.transform];
            mainSequence.Append(slot.transform.DOMove(ShowTransforms[i + 3].position, _moveDuration).SetEase(Ease.OutQuad))
                        .Join(slot.transform.DOScale(originalScale * _cardShowScaleMultiplier, _moveDuration));
        }

        // --- 2. 슬롯들을 순서대로 하나씩 흔들기 ---
        var allShowSlots = roundSlot.EnemyCardSlots.Concat(roundSlot.PlayerCardSlots);
        foreach (var slot in allShowSlots)
        {
            mainSequence.AppendInterval(_interval)
                        .Append(slot.transform.DOShakePosition(_shakeDuration, _shakeStrength));
        }

        // --- 3. 결과 텍스트 표시 ---
        mainSequence.AppendInterval(_interval * 2);
        mainSequence.AppendCallback(() => {
            EnemyResultText.text = EnemyResult;
            MyResultText.text = MyResult;
        });
        mainSequence.Append(EnemyResultText.DOFade(1, _textFadeInDuration));
        mainSequence.Join(MyResultText.DOFade(1, _textFadeInDuration));

        // --- 4. 슬롯들을 원래 위치와 크기로 복귀 (동시에) ---
        mainSequence.AppendInterval(_interval * 4);
        foreach (var slot in allShowSlots)
        {
            mainSequence.Join(slot.transform.DOMove(originalPositions[slot.transform], _moveDuration).SetEase(Ease.InQuad));
            mainSequence.Join(slot.transform.DOScale(originalScales[slot.transform], _moveDuration));
        }

        // --- 5. 승자 쪽으로 돌(Stone) 이동 및 연출 ---
        Transform targetSlotTransform = (winner == RoundWinner.Enemy)
            ? roundSlot.EnemyCardSlots.Last().transform
            : roundSlot.PlayerCardSlots.Last().transform;

        Sequence stoneSequence = DOTween.Sequence();
        stoneSequence
            .Append(Stone.DOScale(stoneOriginalScale * _stoneScaleMultiplier, _stoneAnimDuration / 2).SetEase(Ease.OutQuad))
            .Join(Stone.DOMoveY(Stone.position.y + _stoneFloatHeight, _stoneAnimDuration / 2).SetEase(Ease.OutQuad))
            .Append(Stone.DOMove(targetSlotTransform.position, _stoneAnimDuration / 2).SetEase(Ease.InQuad))
            .Join(Stone.DOScale(stoneOriginalScale, _stoneAnimDuration / 2).SetEase(Ease.InQuad));

        mainSequence.Append(stoneSequence);

        // --- 6. 모든 애니메이션 종료 후 콜백 실행 ---
        mainSequence.OnComplete(() =>
        {
            callback?.Invoke();
        });
    }
}
