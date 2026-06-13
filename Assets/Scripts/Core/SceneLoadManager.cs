using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 全域場景載入管理器（Singleton）。
/// 所有場景切換都經過 LoadingScene，確保一致的載入體驗。
/// 
/// Inspector 設定：
///   - Loading Scene: 拖入 LoadingScene 場景檔案
///   - Main Menu Scene: 拖入 MainMenuScene 場景檔案
/// 
/// 使用方式：
///   SceneLoadManager.Instance.LoadSceneWithLoading("Platformer2D");
///   SceneLoadManager.Instance.ReloadCurrentSceneWithLoading();
///   SceneLoadManager.Instance.LoadMainMenu();
/// </summary>
public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    /// <summary>
    /// LoadingScene 讀取此值來決定要載入哪個場景。
    /// </summary>
    public string TargetSceneName { get; private set; }

    [Header("場景設定（拖入場景檔案）")]
#if UNITY_EDITOR
    [Tooltip("Loading 場景（拖入 Assets/Scenes/LoadingScene.unity）")]
    [SerializeField] private SceneAsset loadingSceneAsset;

    [Tooltip("主選單場景（拖入 Assets/Scenes/MainMenuScene.unity）")]
    [SerializeField] private SceneAsset mainMenuSceneAsset;
#endif

    [Header("場景名稱（自動填入，勿手動修改）")]
    [SerializeField] private string loadingSceneName = "LoadingScene";
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private void Awake()
    {
        // Singleton：場景切換時不銷毀
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor 中修改 SceneAsset 時，自動同步場景名稱字串。
    /// </summary>
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
    /// <param name="sceneName">目標場景名稱（需加入 Build Settings）</param>
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
    /// 重新載入當前活動場景（透過 LoadingScene）。
    /// Retry 按鈕使用此方法，不會寫死場景名稱。
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
}
