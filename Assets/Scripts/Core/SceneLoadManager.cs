using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全域場景載入管理器（Singleton, DontDestroyOnLoad）。
/// 所有場景切換都經過 LoadingScene，確保一致的載入體驗。
/// 
/// Inspector 設定（拖入場景 .unity 檔案即可，名稱自動同步）：
///   - loadingSceneAsset: LoadingScene.unity
///   - mainMenuSceneAsset: MainMenuScene.unity
/// 
/// 使用方式：
///   SceneLoadManager.Instance.LoadSceneWithLoading("GameScene");
///   SceneLoadManager.Instance.LoadSceneDirect("MainMenuScene");
///   SceneLoadManager.Instance.LoadMainMenu();
/// 
/// 注意：關卡 Prefab 的載入由 GameSceneController 處理，不是 SceneLoadManager。
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    /// <summary>
    /// LoadingScene 讀取此值來決定要載入哪個場景。
    /// </summary>
    public string TargetSceneName { get; private set; }

#if UNITY_EDITOR
    [Header("場景設定（拖入 .unity 場景檔案，名稱自動同步）")]
    [Tooltip("Loading 場景")]
    [SerializeField] private SceneAsset loadingSceneAsset;

    [Tooltip("主選單場景")]
    [SerializeField] private SceneAsset mainMenuSceneAsset;
#endif

    [Header("場景名稱（由上方 SceneAsset 自動填入）")]
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
    /// 透過 LoadingScene 載入指定場景。
    /// </summary>
    public void LoadSceneWithLoading(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoadManager] LoadSceneWithLoading: sceneName 為空！");
            return;
        }

        TargetSceneName = sceneName;
        Time.timeScale = 1f;
        SceneManager.LoadScene(loadingSceneName);
    }

    /// <summary>
    /// 直接載入指定場景（不經過 LoadingScene）。
    /// </summary>
    public void LoadSceneDirect(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneLoadManager] LoadSceneDirect: sceneName 為空！");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 重新載入當前活動場景（透過 LoadingScene）。
    /// </summary>
    public void ReloadCurrentSceneWithLoading()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadSceneWithLoading(currentScene);
    }

    /// <summary>
    /// 返回主選單。
    /// </summary>
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        TargetSceneName = mainMenuSceneName;
        SceneManager.LoadScene(loadingSceneName);
    }

    /// <summary>
    /// 取得目標場景名稱（LoadingScreenController 使用）。
    /// </summary>
    public string GetTargetSceneName()
    {
        return TargetSceneName;
    }
}
