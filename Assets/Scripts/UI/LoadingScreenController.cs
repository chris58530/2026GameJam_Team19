using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller attached to LoadingScene.
/// Reads SceneLoadManager.TargetSceneName and asynchronously loads the target scene.
/// 
/// Note:
///   - LoadingScene is only responsible for loading "scenes" (MainMenuScene, LevelSelectorScene, GameScene)
///   - Instantiation of the level Prefab is handled by GameSceneController after GameScene loads
/// 
/// Inspector setup:
///   - progressBar: UI Slider (shows progress)
///   - loadingText: TMP_Text (shows "Loading..." or a percentage)
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    [Header("UI components (connect in the Inspector)")]
    [Tooltip("Progress bar Slider")]
    [SerializeField] private Slider progressBar;

    [Tooltip("Loading text (TMP)")]
    [SerializeField] private TMP_Text loadingText;

    [Header("Settings")]
    [Tooltip("Whether to show the percentage number")]
    [SerializeField] private bool showPercentage = true;

    [Tooltip("Minimum display time (seconds), to avoid the loading screen flashing by")]
    [SerializeField] private float minimumLoadTime = 0.5f;

    [Tooltip("If the target scene is empty, fall back to this scene")]
    [SerializeField] private string fallbackSceneName = "MainMenuScene";

    private void Start()
    {
        StartCoroutine(LoadTargetSceneAsync());
    }

    private IEnumerator LoadTargetSceneAsync()
    {
        // Get the target scene name
        string targetScene = null;

        if (SceneLoadManager.Instance != null)
        {
            targetScene = SceneLoadManager.Instance.GetTargetSceneName();
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[LoadingScreenController] No target scene set, falling back to: " + fallbackSceneName);
            targetScene = fallbackSceneName;
        }

        // Record the start time
        float startTime = Time.realtimeSinceStartup;

        // Begin asynchronous loading
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

        if (asyncLoad == null)
        {
            Debug.LogError("[LoadingScreenController] Cannot load scene: " + targetScene + ", please make sure it is added to Build Settings!");
            yield break;
        }

        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            // Unity's progress caps at 0.9 when allowSceneActivation=false
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Update the progress bar
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Update the text
            if (loadingText != null)
            {
                if (showPercentage)
                    loadingText.text = $"Loading... {(progress * 100f):0}%";
                else
                    loadingText.text = "Loading...";
            }

            // Loading complete; ensure the minimum display time
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
