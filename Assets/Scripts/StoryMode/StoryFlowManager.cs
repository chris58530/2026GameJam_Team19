using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Linear story mode flow manager (Singleton, DontDestroyOnLoad).
/// Manages the full flow: TitleMenu -> OpeningAnimation -> Level01~LevelN -> Ending.
/// 
/// Design highlights:
///   - The level list is configured as a string[] in the Inspector; adding a level only requires adding a scene name
///   - Fully independent from the existing GameFlowManager/SceneLoadManager systems; they do not interfere with each other
///   - Supports both Victory and Fail endings
/// 
/// Setup:
///   1. In the TitleMenu scene, create an empty GameObject named "StoryFlowManager"
///   2. Attach this script
///   3. Fill in the scene names in the Inspector:
///      - titleMenuScene: "TitleMenu"
///      - openingAnimationScene: "OpeningAnimation"
///      - levelScenes: ["Level01", "Level02", "Level03"] <- freely add or remove
///      - endingScene: "Ending"
///   4. Make sure all scenes are added to Build Settings
/// 
/// Public methods (called by other scripts):
///   StoryFlowManager.Instance.StartOpeningAnimation()
///   StoryFlowManager.Instance.StartGameLoop()
///   StoryFlowManager.Instance.CompleteLevel()
///   StoryFlowManager.Instance.FailLevel()
///   StoryFlowManager.Instance.RetryGame()
///   StoryFlowManager.Instance.BackToTitle()
///   StoryFlowManager.Instance.QuitGame()
/// </summary>
public class StoryFlowManager : MonoBehaviour
{
    // ========== Singleton ==========
    public static StoryFlowManager Instance { get; private set; }

    // ========== Inspector Settings ==========

    [Header("Scene Name Settings")]
    [Tooltip("Title menu scene name")]
    [SerializeField] private string titleMenuScene = "TitleMenu";

    [Tooltip("Opening animation scene name")]
    [SerializeField] private string openingAnimationScene = "OpeningAnimation";

    [Tooltip("List of level scene names (in order, freely add or remove)")]
    [SerializeField] private string[] levelScenes = { "Game0", "Game1", "Game2" };

    [Tooltip("Ending scene name")]
    [SerializeField] private string endingScene = "Ending";

    // ========== State ==========

    /// <summary>Game result: Victory or Fail.</summary>
    public enum GameResult { None, Victory, Fail }

    /// <summary>Current game result (read by EndingUI).</summary>
    public GameResult CurrentResult { get; private set; } = GameResult.None;

    /// <summary>Current level index (0-based).</summary>
    public int CurrentLevelIndex { get; private set; } = 0;

    /// <summary>Total number of levels.</summary>
    public int TotalLevelCount => levelScenes != null ? levelScenes.Length : 0;

    /// <summary>Current level scene name.</summary>
    public string CurrentLevelSceneName
    {
        get
        {
            if (levelScenes != null && CurrentLevelIndex >= 0 && CurrentLevelIndex < levelScenes.Length)
                return levelScenes[CurrentLevelIndex];
            return "";
        }
    }

    // ========== Unity Lifecycle ==========

    private void Awake()
    {
        // Singleton pattern: ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== Public Methods ==========

    /// <summary>
    /// Go from TitleMenu into OpeningAnimation.
    /// Called by the Start button in TitleMenuUI.
    /// </summary>
    public void StartOpeningAnimation()
    {
        CurrentResult = GameResult.None;
        CurrentLevelIndex = 0;
        LoadScene(openingAnimationScene);
    }

    /// <summary>
    /// Go from OpeningAnimation into the first level.
    /// Called by OpeningAnimationController.OnOpeningAnimationFinished().
    /// </summary>
    public void StartGameLoop()
    {
        CurrentLevelIndex = 0;
        LoadCurrentLevel();
    }

    /// <summary>
    /// Current level cleared; advance to the next level or the Victory Ending.
    /// Called by LevelManager.OnLevelCleared().
    /// </summary>
    public void CompleteLevel()
    {
        CurrentLevelIndex++;

        // There is a next level -> load the next level
        if (CurrentLevelIndex < TotalLevelCount)
        {
            LoadCurrentLevel();
        }
        // All levels cleared -> Victory Ending
        else
        {
            CurrentResult = GameResult.Victory;
            LoadScene(endingScene);
        }
    }

    /// <summary>
    /// The player failed on any level; go to the Fail Ending.
    /// Called by LevelManager.OnLevelFailed().
    /// </summary>
    public void FailLevel()
    {
        CurrentResult = GameResult.Fail;
        LoadScene(endingScene);
    }

    /// <summary>
    /// Restart from the Ending (from the first level).
    /// Called by the Retry button in EndingUI.
    /// </summary>
    public void RetryGame()
    {
        CurrentResult = GameResult.None;
        CurrentLevelIndex = 0;
        LoadCurrentLevel();
    }

    /// <summary>
    /// Return to the title menu.
    /// Called by the Back to Title button in EndingUI.
    /// </summary>
    public void BackToTitle()
    {
        CurrentResult = GameResult.None;
        CurrentLevelIndex = 0;
        LoadScene(titleMenuScene);
    }

    /// <summary>
    /// Quit the game.
    /// Called by the Quit button in EndingUI.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[StoryFlowManager] Quitting game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== Internal Methods ==========

    /// <summary>
    /// Load the level scene at the current index.
    /// </summary>
    private void LoadCurrentLevel()
    {
        if (levelScenes == null || levelScenes.Length == 0)
        {
            Debug.LogError("[StoryFlowManager] levelScenes is empty! Please set the level scene names in the Inspector.");
            return;
        }

        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelScenes.Length)
        {
            Debug.LogError($"[StoryFlowManager] CurrentLevelIndex ({CurrentLevelIndex}) is out of range!");
            return;
        }

        string sceneName = levelScenes[CurrentLevelIndex];

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[StoryFlowManager] levelScenes[{CurrentLevelIndex}] is an empty string!");
            return;
        }

        Debug.Log($"[StoryFlowManager] Loading level {CurrentLevelIndex + 1}/{TotalLevelCount}: {sceneName}");
        LoadScene(sceneName);
    }

    /// <summary>
    /// Unified scene loading method.
    /// Uses the Loading transition if SceneLoadManager exists, otherwise loads directly.
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[StoryFlowManager] LoadScene: sceneName is empty!");
            return;
        }

        // Prefer the existing SceneLoadManager (with Loading transition)
        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading(sceneName);
        }
        else
        {
            // Load directly (no Loading transition)
            SceneManager.LoadScene(sceneName);
        }
    }
}
