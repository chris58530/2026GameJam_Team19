using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 主選單控制器，掛在 MainMenuScene 的 Canvas 上。
/// 
/// Inspector 設定：
///   - Gameplay Scene: 拖入你的遊戲場景檔案（例如 Platformer2D.unity）
/// 
/// 按鈕連接：
///   - Start Game 按鈕 → StartGame()
///   - Quit Game 按鈕  → QuitGame()
///   - Settings 按鈕   → OpenSettings()（目前僅為 placeholder）
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("場景設定（拖入場景檔案）")]
#if UNITY_EDITOR
    [Tooltip("按下 Start Game 時載入的遊戲場景（拖入場景 .unity 檔）")]
    [SerializeField] private SceneAsset gameplaySceneAsset;
#endif

    [Header("場景名稱（自動填入，勿手動修改）")]
    [Tooltip("Runtime 使用的場景名稱，由上方 SceneAsset 自動同步")]
    [SerializeField] private string defaultGameplaySceneName = "Platformer2D";

#if UNITY_EDITOR
    /// <summary>
    /// Editor 中修改 SceneAsset 時，自動同步場景名稱字串。
    /// </summary>
    private void OnValidate()
    {
        if (gameplaySceneAsset != null)
            defaultGameplaySceneName = gameplaySceneAsset.name;
    }
#endif

    /// <summary>
    /// Start Game 按鈕呼叫。透過 LoadingScene 載入遊戲場景。
    /// </summary>
    public void StartGame()
    {
        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] SceneLoadManager 不存在！請確認場景中有 SceneLoadManager 物件。");
            return;
        }

        SceneLoadManager.Instance.LoadSceneWithLoading(defaultGameplaySceneName);
    }

    /// <summary>
    /// Quit Game 按鈕呼叫。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenuController] 退出遊戲");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Settings 按鈕呼叫（目前為 placeholder）。
    /// </summary>
    public void OpenSettings()
    {
        Debug.Log("[MainMenuController] Settings 功能尚未實作。");
    }
}
