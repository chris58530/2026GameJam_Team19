using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 掛在 LoadingScene 中的控制器。
/// 讀取 SceneLoadManager.TargetSceneName，非同步載入目標場景。
/// 
/// 注意：
///   - LoadingScene 只負責載入「場景」（MainMenuScene, LevelSelectorScene, GameScene）
///   - 關卡 Prefab 的實例化由 GameSceneController 在 GameScene 載入後處理
/// 
/// Inspector 設定：
///   - progressBar: UI Slider（顯示進度）
///   - loadingText: TMP_Text（顯示 "Loading..." 或百分比）
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [Header("UI 元件（在 Inspector 中連接）")]
    [Tooltip("進度條 Slider")]
    [SerializeField] private Slider progressBar;

    [Tooltip("載入文字（TMP）")]
    [SerializeField] private TMP_Text loadingText;

    [Header("設定")]
    [Tooltip("是否顯示百分比數字")]
    [SerializeField] private bool showPercentage = true;

    [Tooltip("最短顯示時間（秒），避免 Loading 畫面一閃而過")]
    [SerializeField] private float minimumLoadTime = 0.5f;

    [Tooltip("如果目標場景為空，回退到此場景")]
    [SerializeField] private string fallbackSceneName = "MainMenuScene";

    private void Start()
    {
        StartCoroutine(LoadTargetSceneAsync());
    }

    private IEnumerator LoadTargetSceneAsync()
    {
        // 取得目標場景名稱
        string targetScene = null;

        if (SceneLoadManager.Instance != null)
        {
            targetScene = SceneLoadManager.Instance.GetTargetSceneName();
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[LoadingScreenController] 沒有設定目標場景，回退到：" + fallbackSceneName);
            targetScene = fallbackSceneName;
        }

        // 記錄開始時間
        float startTime = Time.realtimeSinceStartup;

        // 開始非同步載入
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

        if (asyncLoad == null)
        {
            Debug.LogError("[LoadingScreenController] 無法載入場景：" + targetScene + "，請確認已加入 Build Settings！");
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            // Unity 的 progress 在 allowSceneActivation=false 時最高到 0.9
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 更新進度條
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // 更新文字
            if (loadingText != null)
            {
                if (showPercentage)
                    loadingText.text = $"Loading... {(progress * 100f):0}%";
                else
                    loadingText.text = "Loading...";
            }

            // 載入完成，確保最短顯示時間
            if (asyncLoad.progress >= 0.9f)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed < minimumLoadTime)
                {
                    yield return new WaitForSecondsRealtime(minimumLoadTime - elapsed);
                }

                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
