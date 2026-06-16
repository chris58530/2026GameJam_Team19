using UnityEngine;

/// <summary>
/// Pause menu controller. Attach to the PauseMenuCanvas in GameScene.
/// Supports pause/resume, retry level, return to level selector, and return to main menu.
/// 
/// New architecture behavior:
///   - Retry no longer reloads the whole scene; instead it re-instantiates the level Prefab inside GameScene
///   - Added a Level Selector button to return to the level selection screen
///   - All scene transitions go through GameFlowManager
/// 
/// Setup:
///   1. Place the PauseMenuCanvas Prefab into GameScene
///   2. Make sure the scene has an EventSystem
///   3. Done! Press ESC to pause
/// 
/// Button connections (configured in the PauseMenuCanvas Prefab):
///   - Resume button         -> ResumeGame()
///   - Retry button          -> RetryGame()
///   - Level Selector button -> ReturnToLevelSelector()
///   - Main Menu button      -> ReturnToMainMenu()
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI references (connect in the Inspector)")]
    [Tooltip("Pause menu panel GameObject")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Settings")]
    [Tooltip("Whether to show the mouse cursor while paused")]
    [SerializeField] private bool showCursorWhenPaused = true;

    private bool isPaused = false;
    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

    private void Start()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PauseMenuController] pauseMenuPanel is not set! Please connect it in the Inspector.");
        }

        isPaused = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Toggle between paused and resumed states.
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Pause the game.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Pause");

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);

        if (showCursorWhenPaused)
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Resume the game. Called by the Resume button.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("Resume");

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (showCursorWhenPaused)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    /// <summary>
    /// Retry the current level. Called by the Retry button.
    /// Prefers re-instantiating the level Prefab inside GameScene (without reloading the whole scene).
    /// </summary>
    public void RetryGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // Prefer GameSceneController's retry (faster, no scene reload)
        if (GameSceneController.Instance != null)
        {
            GameSceneController.Instance.RetryCurrentLevel();
        }
        // Fallback: reload GameScene through GameFlowManager
        else if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RetryCurrentLevel();
        }
        // Last resort: reload the current scene
        else if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.ReloadCurrentSceneWithLoading();
        }
        else
        {
            Debug.LogError("[PauseMenuController] Cannot retry! No Manager found.");
        }
    }

    /// <summary>
    /// Return to the level selection screen. Called by the Level Selector button.
    /// </summary>
    public void ReturnToLevelSelector()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

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
            Debug.LogError("[PauseMenuController] Cannot return to level selector! Manager does not exist.");
        }
    }

    /// <summary>
    /// Return to the main menu. Called by the Main Menu button.
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

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
            Debug.LogError("[PauseMenuController] Cannot return to main menu! Manager does not exist.");
        }
    }
}
