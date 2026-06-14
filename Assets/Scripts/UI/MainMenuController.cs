using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 主選單控制器，掛在 MainMenuScene 的 Canvas 上。
/// 
/// Inspector 設定（拖入 .unity 場景檔案即可，名稱自動同步）：
///   - targetSceneAsset: LevelSelectorScene.unity
/// 
/// 按鈕連接：
///   - Start Game 按鈕 → StartGame()
///   - Quit Game 按鈕  → QuitGame()
///   - Settings 按鈕   → OpenSettings()（placeholder）
/// </summary>
public class MainMenuController : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("場景設定（拖入 .unity 場景檔案，名稱自動同步）")]
    [Tooltip("按下 Start Game 時前往的場景")]
    [SerializeField] private SceneAsset targetSceneAsset;
#endif

    [Header("場景名稱（由上方 SceneAsset 自動填入）")]
    [SerializeField] private string targetSceneName = "LevelSelectorScene";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
            targetSceneName = targetSceneAsset.name;
    }
#endif

    /// <summary>
    /// Start Game 按鈕呼叫。前往關卡選擇畫面。
    /// </summary>
    public void StartGame()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("UIClick");

        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] SceneLoadManager 不存在！請確認場景中有 SceneLoadManager 物件。");
            return;
        }

        SceneLoadManager.Instance.LoadSceneWithLoading(targetSceneName);
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
