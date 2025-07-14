using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UI_WinAnimation : MonoBehaviour
{
    // ... 변수 선언부는 이전과 동일 ...
    [Header("===== 대상 오브젝트 =====")]
    public UI_StoneWinAnimation[] WinStones;
    public UI_CardWinAnimation[] OpponentCards;
    public UI_CardWinAnimation[] PlayerCards;
    public GameObject[] StoneThrowers;
    [Header("===== 애니메이션 타겟 =====")]
    public Transform PlayerTargetPosition;
    public Transform OpponentTargetPosition;
    [Header("===== UI 요소 =====")]
    public Image FadeOutPanel;
    public GameObject VictoryUI;
    public GameObject DefeatUI;

    // ... Start, ContextMenu 메서드는 이전과 동일 ...
    private void Start()
    {
        StartVictorySequence(1);
    }
    public void StartOpponentLoseSequence() => StartVictorySequence(0);
    public void StartPlayerLoseSequence() => StartVictorySequence(1);

    public void StartVictorySequence(int index)
    {
        UI_CardWinAnimation[] allCards = (index == 0) ? OpponentCards : PlayerCards;
        List<UI_CardWinAnimation> activeCards = allCards.Where(card => card.gameObject.activeInHierarchy).ToList();

        Sequence mainSequence = DOTween.Sequence();
        Transform finalTargetPosition = (index == 0) ? PlayerTargetPosition : OpponentTargetPosition;

        bool isPlayerTarget = (index == 0);
        for (int i = 0; i < WinStones.Length; i++)
        {
            if (finalTargetPosition != null)
            {
                Sequence stoneSeq = WinStones[i].PlayRiseAndFlyAnimation(finalTargetPosition.position, isPlayerTarget);
                mainSequence.Insert(i * 0.05f, stoneSeq);
            }
        }

        mainSequence.AppendCallback(() =>
        {
            foreach (var thrower in StoneThrowers) { thrower.SetActive(true); }
        });
        mainSequence.AppendInterval(0.5f);

        // --- Phase 3: 카드 순차 파괴 ---
        Sequence cardDestructionSequence = DOTween.Sequence();

        // ▼▼▼▼▼▼ 변경된 부분 (지속적인 카메라 흔들림) ▼▼▼▼▼▼
        Tween cameraShakeTween = null; // 카메라 흔들림 트윈을 제어할 변수

        // 카드 파괴가 시작될 때, 지속적인 흔들림 시작
        cardDestructionSequence.OnStart(() =>
        {
            if (Camera.main != null)
            {
                // 약한 강도로 계속 흔들리는 트윈 생성
                cameraShakeTween = Camera.main.DOShakePosition(1f, 0.5f, 10, 90)
                                              .SetLoops(-1, LoopType.Restart); // 무한 반복
            }
        });

        // 카드 파괴가 모두 끝나면, 흔들림을 멈춤
        cardDestructionSequence.OnComplete(() =>
        {
            cameraShakeTween?.Kill(); // 실행 중인 카메라 흔들림 트윈을 즉시 종료
        });
        // ▲▲▲▲▲▲ 변경된 부분 (지속적인 카메라 흔들림) ▲▲▲▲▲▲

        float initialDelay = 0.15f;
        float currentDelay = 0f;

        for (int i = 0; i < activeCards.Count; i++)
        {
            float delayBetweenCards = initialDelay * (1f - (float)i / activeCards.Count);
            Sequence singleCardDestruction = activeCards[i].PlayDestructionAnimation();
            cardDestructionSequence.Insert(currentDelay, singleCardDestruction);
            currentDelay += delayBetweenCards;
        }

        mainSequence.Insert(mainSequence.Duration() - 0.5f, cardDestructionSequence);

        // --- Phase 4: 화면 페이드 아웃 및 결과 UI 표시 ---
        mainSequence.AppendInterval(0.2f);
        mainSequence.AppendCallback(() =>
        {
            if (FadeOutPanel != null)
            {
                FadeOutPanel.gameObject.SetActive(true);
                FadeOutPanel.color = new Color(FadeOutPanel.color.r, FadeOutPanel.color.g, FadeOutPanel.color.b, 0f);
            }
        });
        mainSequence.Append(FadeOutPanel.DOFade(0.7f, 0.5f));
        mainSequence.AppendCallback(() =>
        {
            GameObject uiToShow = (index == 0) ? VictoryUI : DefeatUI;
            if (uiToShow != null)
            {
                AnimateResultUI(uiToShow);
            }
        });

        mainSequence.SetUpdate(true).Play();
    }

    private void AnimateResultUI(GameObject uiToShow)
    {
        CanvasGroup uiCanvasGroup = uiToShow.GetComponent<CanvasGroup>();
        if (uiCanvasGroup == null)
        {
            uiCanvasGroup = uiToShow.AddComponent<CanvasGroup>();
        }

        Vector3 originalScale = uiToShow.transform.localScale;

        uiToShow.SetActive(true);
        uiCanvasGroup.alpha = 0f;
        uiToShow.transform.localScale = originalScale * 2.5f;

        Sequence uiSequence = DOTween.Sequence();

        uiSequence.Join(uiCanvasGroup.DOFade(1f, 0.3f));
        uiSequence.Join(uiToShow.transform.DOScale(originalScale, 0.4f).SetEase(Ease.OutElastic));
        uiSequence.Append(uiToShow.transform.DOPunchScale(originalScale * -0.1f, 0.4f, 2, 0.5f));

        uiSequence.SetUpdate(true).Play();
    }
}
