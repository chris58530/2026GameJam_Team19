using UnityEngine;

/// <summary>
/// 暫停選單控制器。做成 Prefab 後可以直接拖入任何遊戲場景使用。
/// 不會修改任何遊戲邏輯，僅控制暫停/恢復與場景切換。
/// 
/// 使用方式：
///   1. 將 PauseMenuCanvas prefab 拖入遊戲場景（例如 Platformer2D）
///   2. 確認場景中有 EventSystem
///   3. 完成！按 ESC 即可暫停
/// 
/// 按鈕連接（在 PauseMenuCanvas Prefab 中設定）：
///   - Resume 按鈕     → ResumeGame()
///   - Retry 按鈕      → RetryGame()
///   - Main Menu 按鈕  → ReturnToMainMenu()
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
        // 確保暫停面板一開始是隱藏的
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
        // 按 ESC 切換暫停狀態
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

        // 儲存游標狀態並顯示游標
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

        // 恢復游標狀態
        if (showCursorWhenPaused)
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }
    }

    /// <summary>
    /// 重試（重新載入當前場景）。Retry 按鈕呼叫此方法。
    /// 動態取得當前場景名稱，不寫死任何場景。
    /// </summary>
    public void RetryGame()
    {
        // 恢復時間（SceneLoadManager 也會做，但這裡先恢復避免問題）
        Time.timeScale = 1f;
        isPaused = false;

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.ReloadCurrentSceneWithLoading();
        }
        else
        {
            Debug.LogError("[PauseMenuController] SceneLoadManager 不存在！無法重試。");
        }
    }

    /// <summary>
    /// 返回主選單。Main Menu 按鈕呼叫此方法。
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadMainMenu();
        }
        else
        {
            Debug.LogError("[PauseMenuController] SceneLoadManager 不存在！無法返回主選單。");
        }
    }
}
