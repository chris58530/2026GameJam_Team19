using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 遊戲流程管理器（Singleton, DontDestroyOnLoad）。
/// 儲存當前選擇的關卡與執行時上下文，提供全域場景切換方法。
/// 
/// Inspector 設定（拖入 .unity 場景檔案即可，名稱自動同步）：
///   - mainMenuSceneAsset: MainMenuScene.unity
///   - levelSelectorSceneAsset: LevelSelectorScene.unity
///   - gameSceneAsset: GameScene.unity
/// 
/// 設定方式：
///   1. 在 MainMenuScene 中建立空 GameObject，命名為 "GameFlowManager"
///   2. 掛上此腳本
///   3. 在 Inspector 中拖入三個場景檔案
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    /// <summary>當前選擇的關卡定義。</summary>
    public LevelDefinition CurrentLevelDefinition { get; private set; }

    /// <summary>當前的關卡執行時上下文。</summary>
    public LevelRunContext CurrentContext { get; private set; }

#if UNITY_EDITOR
    [Header("場景設定（拖入 .unity 場景檔案，名稱自動同步）")]
    [Tooltip("主選單場景")]
    [SerializeField] private SceneAsset mainMenuSceneAsset;

    [Tooltip("關卡選擇場景")]
    [SerializeField] private SceneAsset levelSelectorSceneAsset;

    [Tooltip("遊戲場景（shell）")]
    [SerializeField] private SceneAsset gameSceneAsset;
#endif

    [Header("場景名稱（由上方 SceneAsset 自動填入）")]
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
    /// 返回主選單。
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
            Debug.LogError("[GameFlowManager] SceneLoadManager 不存在！");
        }
    }

    /// <summary>
    /// 前往關卡選擇畫面。
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
            Debug.LogError("[GameFlowManager] SceneLoadManager 不存在！");
        }
    }

    /// <summary>
    /// 開始指定關卡。由 LevelSelectorController 呼叫。
    /// </summary>
    public void StartLevel(LevelDefinition levelDefinition, LevelRunContext context)
    {
        if (levelDefinition == null)
        {
            Debug.LogError("[GameFlowManager] StartLevel: levelDefinition 為 null！");
            return;
        }

        if (levelDefinition.levelPrefab == null)
        {
            Debug.LogError($"[GameFlowManager] StartLevel: {levelDefinition.levelId} 的 levelPrefab 為 null！");
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
            Debug.LogError("[GameFlowManager] SceneLoadManager 不存在！");
        }
    }

    /// <summary>
    /// 重試當前關卡。保持相同的 LevelDefinition 和 Context。
    /// </summary>
    public void RetryCurrentLevel()
    {
        if (CurrentLevelDefinition == null)
        {
            Debug.LogWarning("[GameFlowManager] RetryCurrentLevel: 沒有當前關卡，返回選關畫面。");
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
            Debug.LogError("[GameFlowManager] SceneLoadManager 不存在！");
        }
    }

    /// <summary>
    /// 清除當前關卡資料。
    /// </summary>
    public void ClearCurrentLevelData()
    {
        CurrentLevelDefinition = null;
        CurrentContext = null;
    }
}
