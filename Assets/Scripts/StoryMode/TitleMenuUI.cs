using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 標題選單：背景影片 + Start 按鈕。
/// 
/// 設定：
///   1. 建立 Canvas + Start 按鈕
///   2. 掛此腳本，拖入按鈕和影片（影片可選）
///   3. 不需要建 RawImage，腳本自動處理
/// </summary>
public class TitleMenuUI : MonoBehaviour
{
    [Header("按鈕")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("背景影片（可選）")]
    [Tooltip("標題背景影片，循環播放")]
    [SerializeField] private VideoClip backgroundClip;

    private VideoPlayer videoPlayer;
    private RenderTexture rt;

    private void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStart);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

        if (backgroundClip != null)
            PlayBackground();
    }

    private void PlayBackground()
    {
        rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.clip = backgroundClip;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;

        // 自動建立顯示用的 RawImage（放在 Canvas 最底層）
        var rawImageObj = new GameObject("BgVideoImage");
        rawImageObj.transform.SetParent(transform, false);
        rawImageObj.transform.SetAsFirstSibling(); // 最底層，按鈕在上面

        var rectTransform = rawImageObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        var rawImage = rawImageObj.AddComponent<RawImage>();
        rawImage.texture = rt;

        videoPlayer.Play();
    }

    public void OnStart()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (rt != null) { rt.Release(); Destroy(rt); }

        if (StoryFlowManager.Instance != null)
            StoryFlowManager.Instance.StartOpeningAnimation();
    }

    public void OnQuit()
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
}
