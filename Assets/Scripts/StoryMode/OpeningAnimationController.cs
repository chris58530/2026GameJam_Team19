using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Opening animation: plays a video, then jumps to the next level when it finishes. WebGL supported!
/// 
/// === Important WebGL notes ===
/// WebGL does not support VideoClip (a dragged-in mp4); it must read from StreamingAssets via URL.
/// So please:
///   1. Create a folder named "StreamingAssets" under Assets/
///   2. Put your mp4 in it, e.g. Assets/StreamingAssets/Opening.mp4
///   3. Fill in the file name "Opening.mp4" in this script's videoFileName field
/// 
/// This way it plays correctly in the Editor, on PC, and on WebGL.
/// 
/// (If you only play on PC/Editor, you can also drag an mp4 into the videoClip field instead,
///   but WebGL must use videoFileName.)
/// </summary>
public class OpeningAnimationController : MonoBehaviour
{
    [Header("Video Source (WebGL must use this)")]
    [Tooltip("The video file name placed in Assets/StreamingAssets/, e.g. Opening.mp4")]
    [SerializeField] private string videoFileName = "Opening.mp4";

    [Header("Video Source (PC/Editor only, ignored on WebGL)")]
    [Tooltip("Drag an mp4 directly. Not supported on WebGL, leave empty if so")]
    [SerializeField] private VideoClip videoClip;

    [Header("Skip Settings")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipMinWait = 2f;

    private VideoPlayer vp;
    private RenderTexture rt;
    private bool finished = false;
    private float elapsed = 0f;

    private IEnumerator Start()
    {
        // Wait one frame to make sure the scene is ready
        yield return null;

        bool hasSource = SetupVideoPlayer();
        if (!hasSource)
        {
            Debug.LogWarning("[Opening] No video set, skipping after 3 seconds.");
            yield return new WaitForSeconds(3f);
            GoNext();
            yield break;
        }

        // Prepare the video
        vp.Prepare();

        float timeout = 10f;
        float t = 0f;
        while (!vp.isPrepared && t < timeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            Debug.LogError("[Opening] Video preparation timed out! Check the file path and format. Skipping.");
            GoNext();
            yield break;
        }

        vp.loopPointReached += _ => GoNext();
        vp.Play();
        Debug.Log("[Opening] Video started playing.");
    }

    /// <summary>
    /// Sets up the VideoPlayer and returns whether there is a valid video source.
    /// Platform auto-fallback:
    ///   - WebGL: forced to use URL (StreamingAssets) because VideoClip is not supported
    ///   - Other platforms: prefer VideoClip, fall back to URL only if there is none
    /// </summary>
    private bool SetupVideoPlayer()
    {
        rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.skipOnDrop = true;
        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: can only use URL (StreamingAssets)
        if (!string.IsNullOrEmpty(videoFileName))
        {
            string url = Path.Combine(Application.streamingAssetsPath, videoFileName);
            vp.source = VideoSource.Url;
            vp.url = url;
            Debug.Log("[Opening] (WebGL) Playing via URL: " + url);
            CreateDisplay();
            return true;
        }
        Debug.LogError("[Opening] WebGL requires videoFileName to be set (the video inside StreamingAssets)!");
        return false;
#else
        // Editor / PC: prefer VideoClip
        if (videoClip != null)
        {
            vp.source = VideoSource.VideoClip;
            vp.clip = videoClip;
            Debug.Log("[Opening] Playing via VideoClip.");
            CreateDisplay();
            return true;
        }

        // Fallback: use URL
        if (!string.IsNullOrEmpty(videoFileName))
        {
            string url = Path.Combine(Application.streamingAssetsPath, videoFileName);
            vp.source = VideoSource.Url;
            vp.url = url;
            Debug.Log("[Opening] (Fallback) Playing via URL: " + url);
            CreateDisplay();
            return true;
        }

        return false;
#endif
    }

    private void CreateDisplay()
    {
        var canvasObj = new GameObject("_VideoCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var imgObj = new GameObject("_VideoImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        var rect = imgObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var rawImage = imgObj.AddComponent<RawImage>();
        rawImage.texture = rt;
    }

    private void Update()
    {
        if (finished) return;
        elapsed += Time.deltaTime;

        if (allowSkip && elapsed >= skipMinWait && Input.anyKeyDown)
            GoNext();
    }

    private void GoNext()
    {
        if (finished) return;
        finished = true;

        if (vp != null) vp.Stop();
        if (rt != null) { rt.Release(); Destroy(rt); }

        if (StoryFlowManager.Instance != null)
            StoryFlowManager.Instance.StartGameLoop();
        else
            Debug.LogError("[Opening] StoryFlowManager does not exist!");
    }
}
