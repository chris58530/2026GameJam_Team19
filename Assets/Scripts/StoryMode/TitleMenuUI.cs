using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Title menu: background video + Start button.
/// 
/// Setup:
///   1. Create a Canvas + Start button
///   2. Attach this script and drag in the buttons and video (video is optional)
///   3. No need to create a RawImage, the script handles it automatically
/// </summary>
public class TitleMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Background Video (optional)")]
    [Tooltip("Title background video, looped (for Editor / PC)")]
    [SerializeField] private VideoClip backgroundClip;

    [Tooltip("For WebGL: relative path inside the StreamingAssets folder, e.g. Videos/intro.mp4. WebGL cannot play a VideoClip directly, so this file path must be used instead.")]
    [SerializeField] private string webglVideoFileName = "Videos/intro.mp4";

    private VideoPlayer videoPlayer;
    private RenderTexture rt;

    private void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStart);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuit);

        // WebGL requires a file name to play; other platforms just need a clip
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!string.IsNullOrEmpty(webglVideoFileName))
            PlayBackground();
#else
        if (backgroundClip != null || !string.IsNullOrEmpty(webglVideoFileName))
            PlayBackground();
#endif
    }

    private void PlayBackground()
    {
        rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        videoPlayer = gameObject.AddComponent<VideoPlayer>();

        // WebGL does not support VideoClip, use the StreamingAssets URL stream instead
#if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, webglVideoFileName);
#else
        if (backgroundClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = backgroundClip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, webglVideoFileName);
        }
#endif
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;

        // WebGL: browsers block videos "with sound" from auto-playing before any user interaction.
        // The background video plays as soon as the page loads, so it must be muted to avoid being blocked.
#if UNITY_WEBGL && !UNITY_EDITOR
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
#else
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
#endif

        // Automatically create the RawImage used for display (placed at the bottom layer of the Canvas)
        var rawImageObj = new GameObject("BgVideoImage");
        rawImageObj.transform.SetParent(transform, false);
        rawImageObj.transform.SetAsFirstSibling(); // bottom layer, buttons sit on top

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
