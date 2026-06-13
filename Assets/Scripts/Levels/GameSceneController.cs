using UnityEngine;

/// <summary>
/// GameScene 的核心控制器。
/// 負責讀取 GameFlowManager 中的關卡資訊，實例化關卡 Prefab，
/// 並將 LevelRunContext 傳遞給關卡中實作 ILevelInitializable 的元件。
/// 
/// PauseMenuCanvas 在第一次載入關卡時生成，之後不再銷毀。
/// Retry 只會重新實例化關卡 Prefab，不影響 PauseMenu。
/// 
/// 設定方式：
///   1. 在 GameScene 中建立空 GameObject，命名為 "GameSceneController"
///   2. 掛上此腳本
///   3. 建立空子物件 "LevelContainer" 作為關卡 Prefab 的父物件
///   4. 將 LevelContainer 拖入 Inspector 的 levelContainer 欄位
///   5. 將 PauseMenuCanvas Prefab 拖入 pauseMenuPrefab 欄位
/// </summary>
public class GameSceneController : MonoBehaviour
{
    public static GameSceneController Instance { get; private set; }

    [Header("關卡容器")]
    [Tooltip("關卡 Prefab 會實例化在此 Transform 底下")]
    [SerializeField] private Transform levelContainer;

    [Header("暫停選單")]
    [Tooltip("PauseMenuCanvas Prefab")]
    [SerializeField] private GameObject pauseMenuPrefab;

    /// <summary>當前實例化的關卡 GameObject。</summary>
    public GameObject CurrentLevelInstance { get; private set; }

    /// <summary>PauseMenu 實例（生成後不銷毀）。</summary>
    private GameObject pauseMenuInstance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 生成 PauseMenu（只生成一次，之後不銷毀）
        SpawnPauseMenu();

        // 載入關卡
        LoadSelectedLevel();
    }

    /// <summary>
    /// 從 GameFlowManager 讀取當前選定的關卡並實例化。
    /// </summary>
    public void LoadSelectedLevel()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError("[GameSceneController] GameFlowManager 不存在！返回選關畫面。");
            FallbackToLevelSelector();
            return;
        }

        LevelDefinition levelDef = GameFlowManager.Instance.CurrentLevelDefinition;

        if (levelDef == null)
        {
            Debug.LogWarning("[GameSceneController] 沒有選定的關卡！返回選關畫面。");
            FallbackToLevelSelector();
            return;
        }

        if (levelDef.levelPrefab == null)
        {
            Debug.LogError($"[GameSceneController] 關卡 {levelDef.levelId} 的 Prefab 為 null！");
            FallbackToLevelSelector();
            return;
        }

        if (levelContainer == null)
        {
            Debug.LogWarning("[GameSceneController] levelContainer 未設定，使用自身 Transform。");
            levelContainer = transform;
        }

        // 只清除關卡，不動 PauseMenu
        ClearLevelOnly();

        // 實例化關卡 Prefab
        CurrentLevelInstance = Instantiate(levelDef.levelPrefab, levelContainer);
        CurrentLevelInstance.name = $"[Level] {levelDef.displayName}";

        Debug.Log($"[GameSceneController] 已載入關卡: {levelDef.displayName} (ID: {levelDef.levelId})");

        // 傳遞 LevelRunContext
        InitializeLevelComponents();
    }

    /// <summary>
    /// 只清除關卡實例，不影響 PauseMenu。
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
    /// 清除關卡（對外用，場景切換時呼叫）。
    /// </summary>
    public void ClearCurrentLevel()
    {
        ClearLevelOnly();
    }

    /// <summary>
    /// 重試當前關卡。
    /// 只重新實例化同一個關卡 Prefab，PauseMenu 不動。
    /// </summary>
    public void RetryCurrentLevel()
    {
        Time.timeScale = 1f;

        if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentContext != null)
        {
            GameFlowManager.Instance.CurrentContext.replayIndex++;
        }

        // 重新載入同一個關卡
        LoadSelectedLevel();
    }

    /// <summary>
    /// 返回關卡選擇畫面。
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
            Debug.LogError("[GameSceneController] 無法返回選關畫面！");
        }
    }

    /// <summary>
    /// 返回主選單。
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
            Debug.LogError("[GameSceneController] 無法返回主選單！");
        }
    }

    /// <summary>
    /// 生成 PauseMenu（只生成一次）。
    /// </summary>
    private void SpawnPauseMenu()
    {
        if (pauseMenuInstance != null) return;

        // 如果 Inspector 沒設定，嘗試從 Resources 載入
        if (pauseMenuPrefab == null)
        {
            pauseMenuPrefab = Resources.Load<GameObject>("PauseMenuCanvas");
        }

        if (pauseMenuPrefab == null)
        {
            Debug.LogError("[GameSceneController] pauseMenuPrefab 未設定！請在 Inspector 拖入 PauseMenuCanvas Prefab，或放一份到 Assets/Resources/ 資料夾。");
            return;
        }

        pauseMenuInstance = Instantiate(pauseMenuPrefab);
        pauseMenuInstance.name = "PauseMenuCanvas";
        Debug.Log("[GameSceneController] PauseMenuCanvas 已生成。");
    }

    private void InitializeLevelComponents()
    {
        if (CurrentLevelInstance == null) return;

        LevelRunContext context = GameFlowManager.Instance?.CurrentContext;
        if (context == null)
        {
            Debug.LogWarning("[GameSceneController] LevelRunContext 為 null，跳過初始化。");
            return;
        }

        ILevelInitializable[] initializables = CurrentLevelInstance.GetComponentsInChildren<ILevelInitializable>(true);

        if (initializables.Length > 0)
        {
            Debug.Log($"[GameSceneController] 找到 {initializables.Length} 個 ILevelInitializable 元件，開始初始化...");
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
            Debug.LogError("[GameSceneController] 無法回退！SceneLoadManager 不存在。");
        }
    }
}
