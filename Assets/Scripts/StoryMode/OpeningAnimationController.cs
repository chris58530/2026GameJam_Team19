using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 開場動畫：播放影片，播完跳到下一關。支援 WebGL！
/// 
/// === WebGL 重要說明 ===
/// WebGL 不支援 VideoClip（拖入的 mp4），必須用 URL 從 StreamingAssets 讀取。
/// 因此請：
///   1. 在 Assets/ 底下建立資料夾 "StreamingAssets"
///   2. 把你的 mp4 放進去，例如 Assets/StreamingAssets/Opening.mp4
///   3. 在此腳本的 videoFileName 欄位填入檔名 "Opening.mp4"
/// 
/// 這樣 Editor、PC、WebGL 都能正常播放。
/// 
/// （如果只在 PC/Editor 玩，也可以改用 videoClip 欄位拖 mp4，
///   但 WebGL 一定要用 videoFileName。）
/// </summary>
public class OpeningAnimationController : MonoBehaviour
{
    [Header("影片來源（WebGL 必須用這個）")]
    [Tooltip("放在 Assets/StreamingAssets/ 裡的影片檔名，例如 Opening.mp4")]
    [SerializeField] private string videoFileName = "Opening.mp4";

    [Header("影片來源（僅 PC/Editor，WebGL 無效）")]
    [Tooltip("直接拖 mp4。WebGL 不支援，留空即可")]
    [SerializeField] private VideoClip videoClip;

    [Header("跳過設定")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipMinWait = 2f;

    private VideoPlayer vp;
    private RenderTexture rt;
    private bool finished = false;
    private float elapsed = 0f;

    private IEnumerator Start()
    {
        // 等一帧確保場景就緒
        yield return null;

        bool hasSource = SetupVideoPlayer();
        if (!hasSource)
        {
            Debug.LogWarning("[Opening] 沒有設定影片，3 秒後跳過。");
            yield return new WaitForSeconds(3f);
            GoNext();
            yield break;
        }

        // 準備影片
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
            Debug.LogError("[Opening] 影片準備逾時！檢查檔案路徑與格式。直接跳過。");
            GoNext();
            yield break;
        }

        vp.loopPointReached += _ => GoNext();
        vp.Play();
        Debug.Log("[Opening] 影片開始播放。");
    }

    /// <summary>
    /// 設定 VideoPlayer，回傳是否有有效的影片來源。
    /// 平台自動 fallback：
    ///   - WebGL：強制用 URL（StreamingAssets），因為不支援 VideoClip
    ///   - 其他平台：優先用 VideoClip，沒有才用 URL
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
        // WebGL：只能用 URL（StreamingAssets）
        if (!string.IsNullOrEmpty(videoFileName))
        {
            string url = Path.Combine(Application.streamingAssetsPath, videoFileName);
            vp.source = VideoSource.Url;
            vp.url = url;
            Debug.Log("[Opening] (WebGL) 使用 URL 播放: " + url);
            CreateDisplay();
            return true;
        }
        Debug.LogError("[Opening] WebGL 需要設定 videoFileName（StreamingAssets 內的影片）！");
        return false;
#else
        // Editor / PC：優先用 VideoClip
        if (videoClip != null)
        {
            vp.source = VideoSource.VideoClip;
            vp.clip = videoClip;
            Debug.Log("[Opening] 使用 VideoClip 播放。");
            CreateDisplay();
            return true;
        }

        // Fallback：用 URL
        if (!string.IsNullOrEmpty(videoFileName))
        {
            string url = Path.Combine(Application.streamingAssetsPath, videoFileName);
            vp.source = VideoSource.Url;
            vp.url = url;
            Debug.Log("[Opening] (Fallback) 使用 URL 播放: " + url);
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
            Debug.LogError("[Opening] StoryFlowManager 不存在！");
    }
}
