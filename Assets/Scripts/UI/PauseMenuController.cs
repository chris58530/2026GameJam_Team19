using UnityEngine;

/// <summary>
/// 暫停選單控制器。掛在 GameScene 的 PauseMenuCanvas 上。
/// 支援暫停/恢復、重試關卡、返回選關畫面、返回主選單。
/// 
/// 新架構行為：
///   - Retry 不再重載整個場景，而是在 GameScene 內重新實例化關卡 Prefab
///   - 新增 Level Selector 按鈕，返回選關畫面
///   - 所有場景切換都透過 GameFlowManager
/// 
/// 設定方式：
///   1. 將 PauseMenuCanvas Prefab 放入 GameScene
///   2. 確認場景中有 EventSystem
///   3. 完成！按 ESC 即可暫停
/// 
/// 按鈕連接（在 PauseMenuCanvas Prefab 中設定）：
///   - Resume 按鈕        → ResumeGame()
///   - Retry 按鈕         → RetryGame()
///   - Level Selector 按鈕 → ReturnToLevelSelector()
///   - Main Menu 按鈕     → ReturnToMainMenu()
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("UI 參考（在 Inspector 中連接）")]
    [Tooltip("暫停選單面板 GameObject")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("設定")]
    [Tooltip("暫停時是否顯示滑鼠游標")]
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
            Debug.LogWarning("[PauseMenuController] pauseMenuPanel 未設定！請在 Inspector 中連接。");
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
    /// 切換暫停/恢復狀態。
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// 暫停遊戲。
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

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
    /// 恢復遊戲。Resume 按鈕呼叫此方法。
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (showCursorWhenPaused)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    /// <summary>
    /// 重試當前關卡。Retry 按鈕呼叫此方法。
    /// 優先在 GameScene 內重新實例化關卡 Prefab（不重載整個場景）。
    /// </summary>
    public void RetryGame()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        // 優先使用 GameSceneController 的重試（不重載場景，更快）
        if (GameSceneController.Instance != null)
        {
            GameSceneController.Instance.RetryCurrentLevel();
        }
        // 備用：透過 GameFlowManager 重載 GameScene
        else if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RetryCurrentLevel();
        }
        // 最終回退：重載當前場景
        else if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.ReloadCurrentSceneWithLoading();
        }
        else
        {
            Debug.LogError("[PauseMenuController] 無法重試！沒有找到任何 Manager。");
        }
    }

    /// <summary>
    /// 返回關卡選擇畫面。Level Selector 按鈕呼叫此方法。
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
            Debug.LogError("[PauseMenuController] 無法返回選關畫面！Manager 不存在。");
        }
    }

    /// <summary>
    /// 返回主選單。Main Menu 按鈕呼叫此方法。
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
            Debug.LogError("[PauseMenuController] 無法返回主選單！Manager 不存在。");
        }
    }
}
