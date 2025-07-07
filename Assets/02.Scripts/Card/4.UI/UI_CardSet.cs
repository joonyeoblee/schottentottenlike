using System;
using UnityEngine;
using DG.Tweening;

public class UI_CardSet : MonoBehaviour
{
    [Header("====== Animation Settings ======")]
    [Tooltip("카드가 최대로 커지는 배율입니다. (e.g., 1.2는 120% 크기)")]
    [SerializeField] private float _peakScale = 1.2f;

    [Tooltip("최대 크기까지 커지는 데 걸리는 시간입니다.")]
    [SerializeField] private float _durationUp = 0.2f;

    [Tooltip("최대 크기에서 원래 크기로 돌아오는 데 걸리는 시간입니다.")]
    [SerializeField] private float _durationDown = 0.3f;

    [Tooltip("애니메이션 시작 전 딜레이 시간입니다.")]
    [SerializeField] private float _startDelay = 0f;

    [Header("====== Easing Settings ======")]
    [Tooltip("커질 때의 Ease 타입입니다. Out 계열이 자연스럽습니다.")]
    [SerializeField] private Ease _easeTypeUp = Ease.OutQuad;

    [Tooltip("작아질 때의 Ease 타입입니다. In 계열이 자연스럽습니다.")]
    [SerializeField] private Ease _easeTypeDown = Ease.InQuad;

    private Vector3 _originalScale; // 카드의 초기 크기를 저장할 변수
    // private Sequence mySequence; // 미리 만들어둘 필요 없으므로 삭제

    void Awake()
    {
        // 애니메이션이 시작되기 전, 이 오브젝트의 원래 크기를 저장합니다.
        _originalScale = transform.localScale;
    }

    // Start()와 CreateSequence()는 필요 없으므로 삭제합니다.

    /// <summary>
    /// 등장 애니메이션을 재생하는 메서드입니다.
    /// </summary>
    public void PlayAnimation(Action callback = null)
    {
        // 이전에 실행되던 DOTween 애니메이션이 있다면 확실하게 종료
        transform.DOKill();
        // 크기를 즉시 원본으로 되돌려서 여러 번 호출해도 문제가 없도록 함
        transform.localScale = _originalScale;

        // 시퀀스를 이 메서드 안에서 새로 생성합니다.
        Sequence sequence = DOTween.Sequence();

        // 1. 커지는 애니메이션 추가
        sequence.Append(transform.DOScale(_originalScale * _peakScale, _durationUp)
            .SetEase(_easeTypeUp)
            .SetDelay(_startDelay));

        // 2. 다시 작아지는 애니메이션 추가
        sequence.Append(transform.DOScale(_originalScale, _durationDown)
            .SetEase(_easeTypeDown));

        // 3. 콜백 등록 (가장 마지막에 체인으로 연결)
        //    전달받은 콜백이 null이 아닐 경우에만 실행되도록 합니다.
        sequence.OnComplete(() =>
        {
            callback?.Invoke();
        });

        // 시퀀스는 생성과 동시에 자동 재생되므로 Play()를 호출할 필요가 없습니다.
    }
}
