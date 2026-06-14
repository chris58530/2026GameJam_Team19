using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 開場動畫：場景載入後播放影片，播完跳到下一關。
/// 
/// 設定：
///   1. 建立空物件掛此腳本
///   2. Inspector 拖入 mp4
///   3. 完成
/// </summary>
public class OpeningAnimationController : MonoBehaviour
{
    [Header("影片")]
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
        if (videoClip == null)
        {
            yield return new WaitForSeconds(3f);
            GoNext();
            yield break;
        }

        // 等一帧，確保場景完全載入
        yield return null;

        // 建立 RenderTexture
        rt = new RenderTexture(1920, 1080, 0);
        rt.Create();

        // 建立 VideoPlayer
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.source = VideoSource.VideoClip;
        vp.clip = videoClip;
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.skipOnDrop = true;
        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;

        // 建立全螢幕 Canvas + RawImage
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

        // 先準備影片
        vp.Prepare();

        // 等待準備完成
        while (!vp.isPrepared)
            yield return null;

        // 註冊播放完畢事件
        vp.loopPointReached += _ => GoNext();

        // 播放
        vp.Play();
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
    }
}
