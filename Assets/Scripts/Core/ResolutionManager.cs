using UnityEngine;

/// <summary>
/// 각 씬에 배치해 해당 씬의 카메라 비율을 1200×1920(5:8)으로 고정한다.
/// [DefaultExecutionOrder(100)]으로 PixelPerfectCamera보다 나중에 LateUpdate를 실행해
/// rect 덮어쓰기를 방지한다.
/// </summary>
[DefaultExecutionOrder(100)]
public class ResolutionManager : MonoBehaviour
{
    private const float TargetAspect = 1200f / 1920f;  // 0.625 (5:8)

    // ── MonoBehaviour ─────────────────────────────────────────────
    private void Start()
    {
        // 창 모드: 모니터 높이에 맞는 최대 세로형 창으로 초기화
        // (1200×1920을 그대로 열면 1080p 모니터에서 창이 잘려 PixelPerfectCamera가 오작동함)
        if (!Screen.fullScreen)
        {
            int maxH  = Display.main.systemHeight - 80;   // 작업표시줄 여유
            int winH  = Mathf.Min(960, maxH);             // 960 이하로 제한 (5:8 기준점)
            int winW  = Mathf.RoundToInt(winH * TargetAspect);
            Screen.SetResolution(winW, winH, false);
        }

        ApplyAspectRatio();
    }

    private void LateUpdate()
    {
        // PixelPerfectCamera 등이 rect를 바꿔도 매 프레임 마지막에 덮어씀
        ApplyAspectRatio();
    }

    // ── Private Methods ───────────────────────────────────────────
    private void ApplyAspectRatio()
    {
        float windowAspect  = (float)Screen.width / Screen.height;
        float scaleByHeight = windowAspect / TargetAspect;

        Rect rect;

        if (scaleByHeight > 1f)
        {
            // 화면이 목표보다 넓음 → 좌우 필러박스
            float width   = 1f / scaleByHeight;
            float xOffset = (1f - width) * 0.5f;
            rect = new Rect(xOffset, 0f, width, 1f);
        }
        else if (scaleByHeight < 1f)
        {
            // 화면이 목표보다 세로가 더 긺 → 상하 레터박스
            float height  = scaleByHeight;
            float yOffset = (1f - height) * 0.5f;
            rect = new Rect(0f, yOffset, 1f, height);
        }
        else
        {
            rect = new Rect(0f, 0f, 1f, 1f);
        }

        foreach (Camera cam in Camera.allCameras)
            cam.rect = rect;
    }
}
