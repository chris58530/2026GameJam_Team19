using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Global scene load manager (Singleton, DontDestroyOnLoad).
/// All scene transitions go through LoadingScene to ensure a consistent loading experience.
/// 
/// Inspector setup (just drag in the scene .unity file, the name syncs automatically):
///   - loadingSceneAsset: LoadingScene.unity
///   - mainMenuSceneAsset: MainMenuScene.unity
/// 
/// Usage:
///   SceneLoadManager.Instance.LoadSceneWithLoading("GameScene");
///   SceneLoadManager.Instance.LoadSceneDirect("MainMenuScene");
///   SceneLoadManager.Instance.LoadMainMenu();
/// 
/// Note: Loading of the level Prefab is handled by GameSceneController, not SceneLoadManager.
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    /// <summary>
    /// LoadingScene reads this value to decide which scene to load.
    /// </summary>
    public string TargetSceneName { get; private set; }

#if UNITY_EDITOR
    [Header("Scene setup (drag in the .unity scene file, the name syncs automatically)")]
    [Tooltip("Loading scene")]
    [SerializeField] private SceneAsset loadingSceneAsset;

    [Tooltip("Main menu scene")]
    [SerializeField] private SceneAsset mainMenuSceneAsset;
#endif

    [Header("Scene names (auto-filled from the SceneAsset above)")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (loadingSceneAsset != null)
            loadingSceneName = loadingSceneAsset.name;

        if (mainMenuSceneAsset != null)
            mainMenuSceneName = mainMenuSceneAsset.name;
    }
#endif

    /// <summary>
    /// Load the specified scene through LoadingScene.
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoadManager] LoadSceneWithLoading: sceneName is empty!");
            return;
        }

        TargetSceneName = sceneName;
        Time.timeScale = 1f;
        SceneManager.LoadScene(loadingSceneName);
    }

    /// <summary>
    /// Load the specified scene directly (without going through LoadingScene).
    /// </summary>
    public void LoadSceneDirect(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoadManager] LoadSceneDirect: sceneName is empty!");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Reload the currently active scene (through LoadingScene).
    /// </summary>
    public void ReloadCurrentSceneWithLoading()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadSceneWithLoading(currentScene);
    }

    /// <summary>
    /// Return to the main menu.
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        TargetSceneName = mainMenuSceneName;
        SceneManager.LoadScene(loadingSceneName);
    }

    /// <summary>
    /// Get the target scene name (used by LoadingScreenController).
    /// </summary>
    public string GetTargetSceneName()
    {
        return TargetSceneName;
    }
}
