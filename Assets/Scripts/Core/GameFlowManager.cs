using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Game flow manager (Singleton, DontDestroyOnLoad).
/// Stores the currently selected level and runtime context, and provides global scene transition methods.
/// 
/// Inspector setup (just drag in the .unity scene file, the name syncs automatically):
///   - mainMenuSceneAsset: MainMenuScene.unity
///   - levelSelectorSceneAsset: LevelSelectorScene.unity
///   - gameSceneAsset: GameScene.unity
/// 
/// Setup:
///   1. Create an empty GameObject in MainMenuScene, name it "GameFlowManager"
///   2. Attach this script
///   3. Drag the three scene files into the Inspector
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    /// <summary>The currently selected level definition.</summary>
    public LevelDefinition CurrentLevelDefinition { get; private set; }

    /// <summary>The current level runtime context.</summary>
    public LevelRunContext CurrentContext { get; private set; }

#if UNITY_EDITOR
    [Header("Scene setup (drag in the .unity scene file, the name syncs automatically)")]
    [Tooltip("Main menu scene")]
    [SerializeField] private SceneAsset mainMenuSceneAsset;

    [Tooltip("Level selection scene")]
    [SerializeField] private SceneAsset levelSelectorSceneAsset;

    [Tooltip("Game scene (shell)")]
    [SerializeField] private SceneAsset gameSceneAsset;
#endif

    [Header("Scene names (auto-filled from the SceneAsset above)")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private string levelSelectorSceneName = "LevelSelectorScene";
    [SerializeField] private string gameSceneName = "GameScene";

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
        if (mainMenuSceneAsset != null)
            mainMenuSceneName = mainMenuSceneAsset.name;

        if (levelSelectorSceneAsset != null)
            levelSelectorSceneName = levelSelectorSceneAsset.name;

        if (gameSceneAsset != null)
            gameSceneName = gameSceneAsset.name;
    }
#endif

    /// <summary>
    /// Return to the main menu.
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        ClearCurrentLevelData();

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading(mainMenuSceneName);
        }
        else
        {
            Debug.LogError("[GameFlowManager] SceneLoadManager does not exist!");
        }
    }

    /// <summary>
    /// Go to the level selection screen.
    /// </summary>
    public void GoToLevelSelector()
    {
        Time.timeScale = 1f;
        ClearCurrentLevelData();

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading(levelSelectorSceneName);
        }
        else
        {
            Debug.LogError("[GameFlowManager] SceneLoadManager does not exist!");
        }
    }

    /// <summary>
    /// Start the specified level. Called by LevelSelectorController.
    /// </summary>
    public void StartLevel(LevelDefinition levelDefinition, LevelRunContext context)
    {
        if (levelDefinition == null)
        {
            Debug.LogError("[GameFlowManager] StartLevel: levelDefinition is null!");
            return;
        }

        if (levelDefinition.levelPrefab == null)
        {
            Debug.LogError($"[GameFlowManager] StartLevel: levelPrefab of {levelDefinition.levelId} is null!");
            return;
        }

        CurrentLevelDefinition = levelDefinition;
        CurrentContext = context ?? new LevelRunContext();

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading(gameSceneName);
        }
        else
        {
            Debug.LogError("[GameFlowManager] SceneLoadManager does not exist!");
        }
    }

    /// <summary>
    /// Retry the current level. Keeps the same LevelDefinition and Context.
    /// </summary>
    public void RetryCurrentLevel()
    {
        if (CurrentLevelDefinition == null)
        {
            Debug.LogWarning("[GameFlowManager] RetryCurrentLevel: no current level, returning to the level selector.");
            GoToLevelSelector();
            return;
        }

        Time.timeScale = 1f;

        if (CurrentContext != null)
        {
            CurrentContext.replayIndex++;
        }

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading(gameSceneName);
        }
        else
        {
            Debug.LogError("[GameFlowManager] SceneLoadManager does not exist!");
        }
    }

    /// <summary>
    /// Clear the current level data.
    /// </summary>
    public void ClearCurrentLevelData()
    {
        CurrentLevelDefinition = null;
        CurrentContext = null;
    }
}
