using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 結局場景：播放 Victory/Fail 影片，播完顯示按鈕。
/// 
/// 設定：
///   1. 建立 Canvas
///   2. Canvas 底下建立 ButtonsPanel（Retry/Back/Quit），預設 SetActive(false)
///   3. 掛此腳本，拖入影片和按鈕
///   4. 不需要建 RawImage，腳本自動處理
/// </summary>
public class EndingUI : MonoBehaviour
{
    public static EndingUI Instance { get; private set; }

    [Header("影片")]
    [Tooltip("勝利影片")]
    [SerializeField] private VideoClip victoryClip;

    [Tooltip("失敗影片")]
    [SerializeField] private VideoClip failClip;

    [Header("按鈕面板")]
    [Tooltip("包含所有按鈕的面板（預設隱藏）")]
    [SerializeField] private GameObject buttonsPanel;

    [Header("按鈕")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button backToTitleButton;
    [SerializeField] private Button quitButton;

    [Header("無影片時")]
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

        // 建立 RenderTexture
        rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        // 建立 VideoPlayer
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.clip = clip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;

        // 自動建立 RawImage
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
