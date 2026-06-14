using UnityEngine;

/// <summary>
/// Core controller for the GameScene.
/// Reads the level info from GameFlowManager, instantiates the level Prefab,
/// and passes the LevelRunContext to components in the level that implement ILevelInitializable.
/// 
/// PauseMenuCanvas is created the first time a level is loaded and is never destroyed afterwards.
/// Retry only re-instantiates the level Prefab; it does not affect the PauseMenu.
/// 
/// Setup:
///   1. Create an empty GameObject in the GameScene named "GameSceneController"
///   2. Attach this script
///   3. Create an empty child object "LevelContainer" as the parent for the level Prefab
///   4. Drag LevelContainer into the levelContainer field in the Inspector
///   5. Drag the PauseMenuCanvas Prefab into the pauseMenuPrefab field
/// </summary>
public class GameSceneController : MonoBehaviour
{
    public static GameSceneController Instance { get; private set; }

    [Header("Level Container")]
    [Tooltip("The level Prefab will be instantiated under this Transform")]
    [SerializeField] private Transform levelContainer;

    [Header("Pause Menu")]
    [Tooltip("PauseMenuCanvas Prefab")]
    [SerializeField] private GameObject pauseMenuPrefab;

    /// <summary>The currently instantiated level GameObject.</summary>
    public GameObject CurrentLevelInstance { get; private set; }

    /// <summary>PauseMenu instance (not destroyed after being created).</summary>
    private GameObject pauseMenuInstance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Spawn the PauseMenu (created only once, never destroyed afterwards)
        SpawnPauseMenu();

        // Load the level
        LoadSelectedLevel();
    }

    /// <summary>
    /// Reads the currently selected level from GameFlowManager and instantiates it.
    /// </summary>
    public void LoadSelectedLevel()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("[GameSceneController] GameFlowManager does not exist! Returning to the level selector.");
            FallbackToLevelSelector();
            return;
        }

        LevelDefinition levelDef = GameFlowManager.Instance.CurrentLevelDefinition;

        if (levelDef == null)
        {
            Debug.LogWarning("[GameSceneController] No level selected! Returning to the level selector.");
            FallbackToLevelSelector();
            return;
        }

        if (levelDef.levelPrefab == null)
        {
            Debug.LogError($"[GameSceneController] The Prefab for level {levelDef.levelId} is null!");
            FallbackToLevelSelector();
            return;
        }

        if (levelContainer == null)
        {
            Debug.LogWarning("[GameSceneController] levelContainer is not set, using this Transform.");
            levelContainer = transform;
        }

        // Only clear the level, leave the PauseMenu alone
        ClearLevelOnly();

        // Instantiate the level Prefab
        CurrentLevelInstance = Instantiate(levelDef.levelPrefab, levelContainer);
        CurrentLevelInstance.name = $"[Level] {levelDef.displayName}";

        Debug.Log($"[GameSceneController] Loaded level: {levelDef.displayName} (ID: {levelDef.levelId})");

        // Pass the LevelRunContext
        InitializeLevelComponents();
    }

    /// <summary>
    /// Clears only the level instance, leaving the PauseMenu untouched.
    /// </summary>
    private void ClearLevelOnly()
    {
        if (CurrentLevelInstance != null)
        {
            Destroy(CurrentLevelInstance);
            CurrentLevelInstance = null;
        }
    }

    /// <summary>
    /// Clears the level (public, called on scene transitions).
    /// </summary>
    public void ClearCurrentLevel()
    {
        ClearLevelOnly();
    }

    /// <summary>
    /// Retries the current level.
    /// Only re-instantiates the same level Prefab; the PauseMenu is left untouched.
    /// </summary>
    public void RetryCurrentLevel()
    {
        Time.timeScale = 1f;

        if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentContext != null)
        {
            GameFlowManager.Instance.CurrentContext.replayIndex++;
        }

        // Reload the same level
        LoadSelectedLevel();
    }

    /// <summary>
    /// Returns to the level selector screen.
    /// </summary>
    public void ReturnToLevelSelector()
    {
        Time.timeScale = 1f;
        ClearLevelOnly();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GoToLevelSelector();
        }
        else if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading("LevelSelectorScene");
        }
        else
        {
            Debug.LogError("[GameSceneController] Unable to return to the level selector!");
        }
    }

    /// <summary>
    /// Returns to the main menu.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        ClearLevelOnly();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.GoToMainMenu();
        }
        else if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("[GameSceneController] Unable to return to the main menu!");
        }
    }

    /// <summary>
    /// Spawns the PauseMenu (created only once).
    /// </summary>
    private void SpawnPauseMenu()
    {
        if (pauseMenuInstance != null) return;

        // If not set in the Inspector, try loading it from Resources
        if (pauseMenuPrefab == null)
        {
            pauseMenuPrefab = Resources.Load<GameObject>("PauseMenuCanvas");
        }

        if (pauseMenuPrefab == null)
        {
            Debug.LogError("[GameSceneController] pauseMenuPrefab is not set! Drag the PauseMenuCanvas Prefab into the Inspector, or place a copy in the Assets/Resources/ folder.");
            return;
        }

        pauseMenuInstance = Instantiate(pauseMenuPrefab);
        pauseMenuInstance.name = "PauseMenuCanvas";
        Debug.Log("[GameSceneController] PauseMenuCanvas has been spawned.");
    }

    private void InitializeLevelComponents()
    {
        if (CurrentLevelInstance == null) return;

        LevelRunContext context = GameFlowManager.Instance?.CurrentContext;
        if (context == null)
        {
            Debug.LogWarning("[GameSceneController] LevelRunContext is null, skipping initialization.");
            return;
        }

        ILevelInitializable[] initializables = CurrentLevelInstance.GetComponentsInChildren<ILevelInitializable>(true);

        if (initializables.Length > 0)
        {
            Debug.Log($"[GameSceneController] Found {initializables.Length} ILevelInitializable components, starting initialization...");
            foreach (var initializable in initializables)
            {
                initializable.Initialize(context);
            }
        }
    }

    private void FallbackToLevelSelector()
    {
        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading("LevelSelectorScene");
        }
        else
        {
            Debug.LogError("[GameSceneController] Unable to fall back! SceneLoadManager does not exist.");
        }
    }
}
