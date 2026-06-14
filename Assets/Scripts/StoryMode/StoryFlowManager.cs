using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 線性故事模式流程管理器（Singleton, DontDestroyOnLoad）。
/// 管理 TitleMenu → OpeningAnimation → Level01~LevelN → Ending 的完整流程。
/// 
/// 設計重點：
///   - 關卡列表在 Inspector 中以 string[] 設定，新增關卡只要加場景名即可
///   - 與現有 GameFlowManager/SceneLoadManager 系統完全獨立，不互相干擾
///   - 支援 Victory / Fail 兩種結局
/// 
/// 設定方式：
///   1. 在 TitleMenu 場景中建立空 GameObject，命名為 "StoryFlowManager"
///   2. 掛上此腳本
///   3. 在 Inspector 中填入場景名稱：
///      - titleMenuScene: "TitleMenu"
///      - openingAnimationScene: "OpeningAnimation"
///      - levelScenes: ["Level01", "Level02", "Level03"] ← 可自由增減
///      - endingScene: "Ending"
///   4. 確保所有場景已加入 Build Settings
/// 
/// 公開方法（供其他腳本呼叫）：
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

    // ========== Inspector 設定 ==========

    [Header("場景名稱設定")]
    [Tooltip("標題選單場景名稱")]
    [SerializeField] private string titleMenuScene = "TitleMenu";

    [Tooltip("開場動畫場景名稱")]
    [SerializeField] private string openingAnimationScene = "OpeningAnimation";

    [Tooltip("關卡場景名稱列表（按順序排列，可自由增減）")]
    [SerializeField] private string[] levelScenes = { "Game0", "Game1", "Game2" };

    [Tooltip("結局場景名稱")]
    [SerializeField] private string endingScene = "Ending";

    // ========== 狀態 ==========

    /// <summary>遊戲結果：Victory 或 Fail。</summary>
    public enum GameResult { None, Victory, Fail }

    /// <summary>當前遊戲結果（供 EndingUI 讀取）。</summary>
    public GameResult CurrentResult { get; private set; } = GameResult.None;

    /// <summary>當前關卡索引（0-based）。</summary>
    public int CurrentLevelIndex { get; private set; } = 0;

    /// <summary>總關卡數量。</summary>
    public int TotalLevelCount => levelScenes != null ? levelScenes.Length : 0;

    /// <summary>當前關卡場景名稱。</summary>
    public string CurrentLevelSceneName
    {
        get
        {
            if (levelScenes != null && CurrentLevelIndex >= 0 && CurrentLevelIndex < levelScenes.Length)
                return levelScenes[CurrentLevelIndex];
            return "";
        }
    }

    // ========== Unity 生命週期 ==========

    private void Awake()
    {
        // Singleton 模式：確保只有一個實例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 公開方法 ==========

    /// <summary>
    /// 從 TitleMenu 進入 OpeningAnimation。
    /// 由 TitleMenuUI 的 Start 按鈕呼叫。
    /// </summary>
    public void StartOpeningAnimation()
    {
        CurrentResult = GameResult.None;
        CurrentLevelIndex = 0;
        LoadScene(openingAnimationScene);
    }

    /// <summary>
    /// 從 OpeningAnimation 進入第一個關卡。
    /// 由 OpeningAnimationController.OnOpeningAnimationFinished() 呼叫。
    /// </summary>
    public void StartGameLoop()
    {
        CurrentLevelIndex = 0;
        LoadCurrentLevel();
    }

    /// <summary>
    /// 當前關卡通關，前往下一關或 Victory Ending。
    /// 由 LevelManager.OnLevelCleared() 呼叫。
    /// </summary>
    public void CompleteLevel()
    {
        CurrentLevelIndex++;

        // 還有下一關 → 載入下一關
        if (CurrentLevelIndex < TotalLevelCount)
        {
            LoadCurrentLevel();
        }
        // 全部通關 → Victory Ending
        else
        {
            CurrentResult = GameResult.Victory;
            LoadScene(endingScene);
        }
    }

    /// <summary>
    /// 玩家在任何關卡失敗，前往 Fail Ending。
    /// 由 LevelManager.OnLevelFailed() 呼叫。
    /// </summary>
    public void FailLevel()
    {
        CurrentResult = GameResult.Fail;
        LoadScene(endingScene);
    }

    /// <summary>
    /// 從 Ending 重新開始（從第一關）。
    /// 由 EndingUI 的 Retry 按鈕呼叫。
    /// </summary>
    public void RetryGame()
    {
        CurrentResult = GameResult.None;
        CurrentLevelIndex = 0;
        LoadCurrentLevel();
    }

    /// <summary>
    /// 返回標題選單。
    /// 由 EndingUI 的 Back to Title 按鈕呼叫。
    /// </summary>
    public void BackToTitle()
    {
        CurrentResult = GameResult.None;
        CurrentLevelIndex = 0;
        LoadScene(titleMenuScene);
    }

    /// <summary>
    /// 退出遊戲。
    /// 由 EndingUI 的 Quit 按鈕呼叫。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[StoryFlowManager] 退出遊戲");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ========== 內部方法 ==========

    /// <summary>
    /// 載入當前索引的關卡場景。
    /// </summary>
    private void LoadCurrentLevel()
    {
        if (levelScenes == null || levelScenes.Length == 0)
        {
            Debug.LogError("[StoryFlowManager] levelScenes 為空！請在 Inspector 中設定關卡場景名稱。");
            return;
        }

        if (CurrentLevelIndex < 0 || CurrentLevelIndex >= levelScenes.Length)
        {
            Debug.LogError($"[StoryFlowManager] CurrentLevelIndex ({CurrentLevelIndex}) 超出範圍！");
            return;
        }

        string sceneName = levelScenes[CurrentLevelIndex];

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[StoryFlowManager] levelScenes[{CurrentLevelIndex}] 為空字串！");
            return;
        }

        Debug.Log($"[StoryFlowManager] 載入關卡 {CurrentLevelIndex + 1}/{TotalLevelCount}: {sceneName}");
        LoadScene(sceneName);
    }

    /// <summary>
    /// 統一的場景載入方法。
    /// 如果 SceneLoadManager 存在就用 Loading 過渡，否則直接載入。
    /// </summary>
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[StoryFlowManager] LoadScene: sceneName 為空！");
            return;
        }

        // 優先使用現有的 SceneLoadManager（帶 Loading 過渡）
        if (SceneLoadManager.Instance != null)
        {
            SceneLoadManager.Instance.LoadSceneWithLoading(sceneName);
        }
        else
        {
            // 直接載入（無 Loading 過渡）
            SceneManager.LoadScene(sceneName);
        }
    }
}
