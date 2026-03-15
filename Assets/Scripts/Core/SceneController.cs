using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환·퍼즈·종료를 담당합니다.
/// 각 씬에 오브젝트를 만들어 부착하고 Button.OnClick에 직접 연결하세요.
/// </summary>
public class SceneController : MonoBehaviour
{
    // ──────────────────────────────────────────
    // Private Fields
    // ──────────────────────────────────────────
    //private const int MainSceneIndex     = 0;
    private const int OutGameSceneIndex  = 0;
    private const int InGameSceneIndex   = 1;
    private const int InGame2SceneIndex  = 2;

    // ──────────────────────────────────────────
    // 씬 전환
    // ──────────────────────────────────────────

    /// <summary>0_Main 씬으로 이동합니다.</summary>
    // public void LoadMain()
    // {
    //     Time.timeScale = 1f;
    //     SceneManager.LoadScene(MainSceneIndex);
    // }

    /// <summary>1_OutGame 씬으로 이동합니다.</summary>
    public void LoadOutGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(OutGameSceneIndex);
    }

    /// <summary>2_Ingame 씬으로 이동합니다.</summary>
    public void LoadInGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(InGameSceneIndex);
    }

    /// <summary>3_Ingame 씬으로 이동합니다.</summary>
    public void LoadInGame2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(InGame2SceneIndex);
    }

    /// <summary>게임을 종료합니다. 퍼즈 상태에서 종료해도 정상 처리됩니다.</summary>
    public void Quit()
    {
        Application.Quit();
    }
}
