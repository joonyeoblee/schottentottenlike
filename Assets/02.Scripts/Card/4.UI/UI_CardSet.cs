using UnityEngine;
using DG.Tweening; // DOTween 네임스페이스 추가

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
    private Sequence mySequence;
    void Awake()
    {
        // 애니메이션이 시작되기 전, 이 오브젝트의 원래 크기를 저장합니다.
        _originalScale = transform.localScale;
    }

    void Start()
    {
        CerateSequence();
        // 게임 오브젝트가 활성화되면 자동으로 애니메이션을 재생합니다.
        PlayAnimation();
    }

    private void CerateSequence()
    {
        // DOTween 시퀀스를 생성하여 여러 애니메이션을 순차적으로 연결합니다.
       mySequence  = DOTween.Sequence();

        // 현재 크기를 0으로 만들어 보이지 않게 처리하고 싶다면 아래 주석을 해제하세요.
        // transform.localScale = Vector3.zero;

        // 1. Append: 원래 크기에서 _peakScale 배율만큼 커지는 애니메이션을 추가합니다.
        mySequence.Append(transform.DOScale(_originalScale * _peakScale, _durationUp)
            .SetEase(_easeTypeUp)
            .SetDelay(_startDelay));

        // 2. Append: 최대 크기에서 다시 원래 크기로 돌아오는 애니메이션을 이어서 추가합니다.
        mySequence.Append(transform.DOScale(_originalScale, _durationDown)
            .SetEase(_easeTypeDown));
    }

    /// <summary>
    /// 등장 애니메이션을 재생하는 메서드입니다.
    /// </summary>
    public void PlayAnimation()
    {


        // 생성된 시퀀스를 재생합니다.
        mySequence.Play();
    }
}
