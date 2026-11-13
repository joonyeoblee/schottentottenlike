#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 플레이 영상 녹화용 데모 플레이어 (에디터/개발 빌드 전용).
/// AI 모드에서 사람처럼 카드에 마우스를 올려 살펴보고, 곡선을 그리며 끌어서 칸에 놓습니다.
/// 튜토리얼이 열려 있으면 페이지를 넘기고, 게임이 끝나면 멈춥니다.
/// </summary>
public class ShowcasePlayer : MonoBehaviour
{
    public static ShowcasePlayer Instance { get; private set; }
    public bool Finished { get; private set; }
    public int Moves { get; private set; }

    public static string Begin()
    {
        if (Instance != null) return "already running";
        var go = new GameObject("ShowcasePlayer");
        Instance = go.AddComponent<ShowcasePlayer>();
        Instance.StartCoroutine(Instance.Run());
        return "showcase started";
    }

    public static string Status() => Instance == null ? "not running" : $"moves={Instance.Moves} finished={Instance.Finished}";

    // ---- 개발 빌드 녹화 모드: "-showcase <프레임저장폴더>" ----
    // AI 대전 씬을 바로 열고(튜토리얼부터), 데모 플레이어가 한 판을 둔 뒤 종료합니다.
    // 고정 30fps(Time.captureFramerate)로 매 프레임을 JPG로 저장 → 에디터에서 MP4로 인코딩
    private static string _frameDir;
    private int _frame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void FromCommandLine()
    {
        var args = System.Environment.GetCommandLineArgs();
        int i = System.Array.IndexOf(args, "-showcase");
        if (i < 0) return;
        _frameDir = i + 1 < args.Length ? args[i + 1] : System.IO.Path.Combine(Application.persistentDataPath, "showcase");
        System.IO.Directory.CreateDirectory(_frameDir);

        PlayerPrefs.DeleteKey("tutorial_done_v1");
        PlayerPrefs.DeleteKey("coach_done_v1");
        Time.captureFramerate = 30;

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) =>
        {
            if (scene.buildIndex != 3 || Instance != null) return;
            Begin();
            Instance.StartCoroutine(Instance.CaptureFrames());
        };
        UnityEngine.SceneManagement.SceneManager.LoadScene(3);
    }

    private IEnumerator CaptureFrames()
    {
        while (!Finished)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(_frameDir, $"f{_frame:D5}.jpg"), tex.EncodeToJPG(90));
            Destroy(tex);
            _frame++;
        }
        Debug.Log($"[Showcase] captured {_frame} frames -> {_frameDir}");
        Application.Quit();
    }

    private IEnumerator Run()
    {
        // 1) 튜토리얼 페이지 넘기기 (열려 있으면)
        yield return new WaitForSeconds(1.5f);
        while (UI_Tutorial.IsOpen)
        {
            yield return new WaitForSeconds(2.8f); // 녹화 시 Recorder의 고정 프레임 시간 기준으로 진행
            ClickButton("다음", "시작하기");
        }

        // 2) 게임 진행
        var bf = BattleField_AI.Instance;
        var gm = GameManager.Instance;
        while (bf != null && !gm.IsGameOver)
        {
            if (bf.IsPlayerTurn)
            {
                yield return new WaitForSeconds(0.6f);
                if (!gm.IsGameOver && bf.IsPlayerTurn) yield return PlayOneCard(bf, gm);
            }
            yield return new WaitForSeconds(0.2f);
        }

        // 3) 결과 화면 감상
        yield return new WaitForSeconds(4f);
        Finished = true;
    }

    private IEnumerator PlayOneCard(BattleField_AI bf, GameManager gm)
    {
        bool Same(Card a, Card b) => a.CardNumber == b.CardNumber && a.Color == b.Color;
        var field = bf.Rounds.SelectMany(r => r.PlayerCardSlots.Concat(r.EnemyCardSlots)).Where(s => s.IsOccupied).Select(s => s.Card).ToList();
        var fieldUnused = gm.GetAllPossibleCards().Where(c => !field.Any(f => Same(f, c))).ToList();
        var handSlots = bf.HandCardManagers[0].HandCardSlots;
        var hand = handSlots.Select(s => s.HasCard ? s.Card : null).ToList();
        var view = new AIBrain.BoardView
        {
            AiCards = bf.Rounds.Select(r => r.PlayerCardSlots.Where(s => s.IsOccupied).Select(s => s.Card).ToList()).ToArray(),
            PlayerCards = bf.Rounds.Select(r => r.EnemyCardSlots.Where(s => s.IsOccupied).Select(s => s.Card).ToList()).ToArray(),
            Owners = gm.RoundOwners.Select(o => o == 0 ? 0 : (o == 1 ? 2 : 1)).ToArray(),
            Hand = hand,
            FieldUnused = fieldUnused,
            Unseen = fieldUnused.Where(c => !hand.Any(h => h != null && Same(h, c))).ToList(),
            SlotsPerSide = bf.Rounds[0].PlayerCardSlots.Length,
        };
        var mv = AIBrain.ChooseMove(view);
        if (!mv.HasValue) yield break;

        var handSlot = handSlots[mv.Value.HandIndex];
        var dragger = handSlot.GetComponentInChildren<UI_CardDragger>(true);
        var target = bf.Rounds[mv.Value.RoundIndex].FirstEmptySlot(true);
        var mouse = new PointerEventData(EventSystem.current) { pointerId = -1 };

        // 마우스를 올려 카드 살펴보기
        dragger.OnPointerEnter(mouse);
        yield return new WaitForSeconds(0.7f);

        // 집어서 곡선으로 이동
        dragger.OnPointerDown(mouse);
        Vector3 from = dragger.transform.position;
        Vector3 to = new Vector3(target.transform.position.x, target.transform.position.y, from.z);
        Vector3 ctrl = (from + to) * 0.5f + Vector3.up * 1.2f;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.65f;
            float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
            Vector3 p = Vector3.Lerp(Vector3.Lerp(from, ctrl, e), Vector3.Lerp(ctrl, to, e), e);
            dragger.transform.position = p;
            yield return null;
        }
        dragger.OnPointerUp(mouse);
        dragger.OnPointerExit(mouse);
        Moves++;
        yield return new WaitForSeconds(0.4f);
    }

    private static void ClickButton(params string[] labels)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            var t = b.GetComponentInChildren<TMPro.TMP_Text>();
            if (t != null && labels.Contains(t.text) && b.isActiveAndEnabled) { b.onClick.Invoke(); return; }
        }
    }
}
#endif
