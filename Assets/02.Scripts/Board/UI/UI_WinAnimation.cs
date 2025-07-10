using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections; // IEnumerator를 위해 추가

public class UI_WinAnimation : MonoBehaviour
{
    [Header("===== 대상 오브젝트 =====")]
    [Tooltip("승리 시 위로 솟아오르며 날아갈 돌들")]
    public UI_StoneWinAnimation[] WinStones;

    [Tooltip("패배 시 파괴될 상대방 카드들")]
    public UI_CardWinAnimation[] CardWins;

    [Tooltip("돌을 던질 투석기 오브젝트들")]
    public GameObject[] StoneThrowers;

    [Header("===== 애니메이션 타겟 =====")]
    [Tooltip("승리한 돌들이 날아갈 플레이어측 최종 위치")]
    public Transform PlayerTargetPosition;

    [Header("===== UI 요소 =====")]
    [Tooltip("화면을 어둡게 할 검은색 Image 패널")]
    public Image FadeOutPanel;

    [Tooltip("마지막에 나타날 승리/패배 UI")]
    public GameObject VictoryUI;


    private void Start()
    {
        StartVictorySequence();
    }

    // 외부에서 이 메서드를 호출하여 전체 시퀀스를 시작합니다.
    [ContextMenu("Execute Victory Animation")] // 테스트용 메뉴
    public void StartVictorySequence()
    {
        Debug.Log("승리 애니메이션 스타트");
        // DOTween의 시퀀스 기능을 사용하여 순차적인 애니메이션 그룹을 만듭니다.
        Sequence mainSequence = DOTween.Sequence();

        // --- Phase 1: 돌들이 솟아오르고 플레이어에게 날아감 ---
        // 여러 돌이 동시에 움직이도록 루프를 사용합니다.
        for (int i = 0; i < WinStones.Length; i++)
        {
            // 각 돌의 애니메이션 시퀀스를 가져옵니다.
            Sequence stoneSeq = WinStones[i].PlayRiseAndFlyAnimation(PlayerTargetPosition.position);
            // 메인 시퀀스에 0.1초씩 딜레이를 주어 합류시킵니다. (돌들이 순차적으로 움직이는 효과)
            mainSequence.Insert(i * 0.1f, stoneSeq);
        }

        // --- Phase 2: 투석기 등장 및 발사 ---
        // 돌 애니메이션이 끝난 후 투석기가 나타나도록 AppendCallback 사용
        mainSequence.AppendCallback(() =>
        {
            foreach (var thrower in StoneThrowers)
            {
                thrower.SetActive(true);
                // 투석기에 Animator가 있다면 발사 애니메이션을 트리거할 수 있습니다.
                // 예: thrower.GetComponent<Animator>()?.SetTrigger("Fire");
            }
        });
        mainSequence.AppendInterval(1.0f); // 투석기 발사 애니메이션 시간만큼 대기

        // --- Phase 3: 상대 카드 파괴 ---
        // 투석기 발사와 동시에 카드가 터지도록 Insert 사용
        Sequence cardDestructionSequence = DOTween.Sequence();
        foreach (var card in CardWins)
        {
            // 각 카드의 파괴 애니메이션을 가져와 동시에 실행(Join)
            cardDestructionSequence.Join(card.PlayDestructionAnimation());
        }
        mainSequence.Insert(mainSequence.Duration() - 1.0f, cardDestructionSequence);


        // --- Phase 4: 화면 페이드 아웃 및 승리 UI 표시 ---
        mainSequence.AppendInterval(0.5f); // 모든 애니메이션이 끝난 후 잠시 대기
        mainSequence.Append(FadeOutPanel.DOFade(0.5f, 1.0f)); // 1초에 걸쳐 50% 불투명하게
        mainSequence.AppendCallback(() =>
        {
            VictoryUI.SetActive(true);
        });
        mainSequence.SetUpdate(true);

        mainSequence.Play();
    }
}
