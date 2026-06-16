using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main menu controller, attached to the Canvas in MainMenuScene.
/// 
/// Inspector setup (just drag in the .unity scene file, the name syncs automatically):
///   - targetSceneAsset: LevelSelectorScene.unity
/// 
/// Button connections:
///   - Start Game button -> StartGame()
///   - Quit Game button  -> QuitGame()
///   - Settings button   -> OpenSettings() (placeholder)
/// </summary>
public class MainMenuController : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Scene setup (drag in the .unity scene file, the name syncs automatically)")]
    [Tooltip("The scene to go to when Start Game is pressed")]
    [SerializeField] private SceneAsset targetSceneAsset;
#endif

    [Header("Scene name (auto-filled from the SceneAsset above)")]
    [SerializeField] private string targetSceneName = "LevelSelectorScene";

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetSceneAsset != null)
            targetSceneName = targetSceneAsset.name;
    }
#endif

    /// <summary>
    /// Called by the Start Game button. Goes to the level selection screen.
    /// </summary>
    public void StartGame()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("UIClick");

        if (SceneLoadManager.Instance == null)
        {
            Debug.LogError("[MainMenuController] SceneLoadManager does not exist! Please make sure the scene has a SceneLoadManager object.");
            return;
        }

        SceneLoadManager.Instance.LoadSceneWithLoading(targetSceneName);
    }

    /// <summary>
    /// Called by the Quit Game button.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[MainMenuController] Quitting game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Called by the Settings button (currently a placeholder).
    /// </summary>
    public void OpenSettings()
    {
        Debug.Log("[MainMenuController] Settings feature is not implemented yet.");
    }
}
