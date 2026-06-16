using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Ending scene: plays the Victory/Fail video, then shows buttons when it finishes.
/// 
/// Setup:
///   1. Create a Canvas
///   2. Under the Canvas, create a ButtonsPanel (Retry/Back/Quit), default SetActive(false)
///   3. Attach this script and drag in the videos and buttons
///   4. No need to create a RawImage, the script handles it automatically
/// </summary>
public class EndingUI : MonoBehaviour
{
    public static EndingUI Instance { get; private set; }

    [Header("Videos")]
    [Tooltip("Victory video")]
    [SerializeField] private VideoClip victoryClip;

    [Tooltip("Fail video")]
    [SerializeField] private VideoClip failClip;

    [Header("Buttons Panel")]
    [Tooltip("Panel containing all buttons (hidden by default)")]
    [SerializeField] private GameObject buttonsPanel;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private Button quitButton;

    [Header("When No Video")]
    [SerializeField] private float noVideoDelay = 3f;

    private VideoPlayer videoPlayer;
    private RenderTexture rt;
    private bool buttonsShown = false;

    private void Awake() { Instance = this; }

    private void Start()
    {
        if (buttonsPanel != null) buttonsPanel.SetActive(false);

        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        if (backToTitleButton != null) backToTitleButton.onClick.AddListener(OnBackToTitle);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

        PlayVideo();
    }

    private void PlayVideo()
    {
        bool isVictory = StoryFlowManager.Instance != null
            && StoryFlowManager.Instance.CurrentResult == StoryFlowManager.GameResult.Victory;

        VideoClip clip = isVictory ? victoryClip : failClip;

        if (clip == null)
        {
            Invoke(nameof(ShowButtons), noVideoDelay);
            return;
        }

        // Create the RenderTexture
        rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        // Create the VideoPlayer
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.clip = clip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;

        // Automatically create a RawImage
        var rawImageObj = new GameObject("VideoImage");
        rawImageObj.transform.SetParent(transform, false);
        rawImageObj.transform.SetAsFirstSibling();

        var rectTransform = rawImageObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var rawImage = rawImageObj.AddComponent<RawImage>();
        rawImage.texture = rt;

        videoPlayer.loopPointReached += _ => ShowButtons();
        videoPlayer.Play();
    }

    public void ShowButtons()
    {
        if (buttonsShown) return;
        buttonsShown = true;
        if (buttonsPanel != null) buttonsPanel.SetActive(true);
    }

    private void OnRetry()
    {
        Cleanup();
        if (StoryFlowManager.Instance != null) StoryFlowManager.Instance.RetryGame();
    }

    private void OnBackToTitle()
    {
        Cleanup();
        if (StoryFlowManager.Instance != null) StoryFlowManager.Instance.BackToTitle();
    }

    private void OnQuit()
    {
        if (StoryFlowManager.Instance != null)
            StoryFlowManager.Instance.QuitGame();
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void Cleanup()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (rt != null) { rt.Release(); Destroy(rt); }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Cleanup();
    }
}
