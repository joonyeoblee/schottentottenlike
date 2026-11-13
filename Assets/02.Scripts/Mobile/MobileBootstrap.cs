using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 모바일 공통 설정. 게임 시작 시 자동으로 실행됩니다 (씬에 배치할 필요 없음).
/// - 가로 화면 고정(왼쪽/오른쪽 자동 회전), 60fps, 화면 꺼짐 방지
/// - 게임 보드 씬: 화면비에 맞춰 카메라 조절
/// - 모든 씬: 기존 UI를 안전 영역(노치) 안으로 보정
/// </summary>
public static class MobileBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        if (Application.isMobilePlatform)
            Screen.orientation = ScreenOrientation.AutoRotation;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 카메라 화면비 대응 (게임 보드 씬)
        var cam = Camera.main;
        if (cam != null && cam.orthographic && scene.name.Contains("GameBoard") && cam.GetComponent<CameraFitter>() == null)
            cam.gameObject.AddComponent<CameraFitter>();

        // 기존 씬 UI 안전 영역 보정
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
                foreach (RectTransform child in canvas.transform)
                {
                    if (child == null || IsFullScreen(child) || child.GetComponent<SafeAreaNudge>() != null) continue;
                    child.gameObject.AddComponent<SafeAreaNudge>();
                }
            }
        }
    }

    private static bool IsFullScreen(RectTransform rt)
    {
        return rt.anchorMin.x <= 0.01f && rt.anchorMin.y <= 0.01f && rt.anchorMax.x >= 0.99f && rt.anchorMax.y >= 0.99f;
    }
}
