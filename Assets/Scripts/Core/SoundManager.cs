using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全域音效管理器（Singleton, DontDestroyOnLoad）。
/// 支援 BGM 播放（含淡入淡出切換）及 SFX 多聲道播放。
/// 支援場景自動切換 BGM（透過 SceneBGM 映射設定）。
///
/// Inspector 設定：
///   - bgmClips: 所有 BGM 音效（以名稱索引）
///   - sfxClips: 所有 SFX 音效（以名稱索引）
///   - sfxChannelCount: SFX 同時播放聲道數量
///   - bgmVolume / sfxVolume: 音量控制
///   - bgmFadeDuration: BGM 切換淡入淡出時間
///   - sceneBGMBindings: 場景名稱 → BGM 自動對應
///
/// 使用方式：
///   SoundManager.Instance.PlayBGM("BattleTheme");
///   SoundManager.Instance.StopBGM();
///   SoundManager.Instance.PlaySFX("Jump");
///   SoundManager.Instance.PlaySFX("Hit", 0.8f);
///
/// 場景自動 BGM：
///   在 Inspector 的「場景 BGM 綁定」區塊設定場景名稱對應的 BGM 名稱，
///   切換場景時會自動播放對應的 BGM。
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // ─── Inspector 設定 ───────────────────────────────────────

    [Header("BGM 設定")]
    [Tooltip("BGM 音量 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    [Tooltip("BGM 切換淡入淡出時間（秒）")]
    [SerializeField] private float bgmFadeDuration = 1f;

    [Tooltip("所有 BGM 音效片段")]
    [SerializeField] private SoundClip[] bgmClips;

    [Header("場景 BGM 自動綁定")]
    [Tooltip("設定每個場景自動播放的 BGM（場景名稱 → BGM 名稱）")]
    [SerializeField] private SceneBGMBinding[] sceneBGMBindings;

    [Header("SFX 設定")]
    [Tooltip("SFX 音量 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Tooltip("SFX 同時播放聲道數量")]
    [SerializeField] private int sfxChannelCount = 8;

    [Tooltip("所有 SFX 音效片段")]
    [SerializeField] private SoundClip[] sfxClips;

    // ─── 內部狀態 ───────────────────────────────────────────

    private AudioSource bgmSource;
    private AudioSource[] sfxSources;
    private int currentSfxIndex = 0;

    private Dictionary<string, AudioClip> bgmDict;
    private Dictionary<string, AudioClip> sfxDict;

    private Coroutine bgmFadeCoroutine;
    private string currentBgmName;

    private Dictionary<string, string> sceneBgmMap;

    // ─── Lifecycle ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitAudioSources();
        BuildDictionaries();
        BuildSceneBGMMap();

        // 監聽場景切換事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitAudioSources()
    {
        // BGM AudioSource
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        // SFX AudioSources (多聲道)
        sfxSources = new AudioSource[sfxChannelCount];
        for (int i = 0; i < sfxChannelCount; i++)
        {
            sfxSources[i] = gameObject.AddComponent<AudioSource>();
            sfxSources[i].loop = false;
            sfxSources[i].playOnAwake = false;
            sfxSources[i].volume = sfxVolume;
        }
    }

    private void BuildDictionaries()
    {
        bgmDict = new Dictionary<string, AudioClip>();
        if (bgmClips != null)
        {
            foreach (var clip in bgmClips)
            {
                if (clip != null && !string.IsNullOrEmpty(clip.name) && clip.clip != null)
                {
                    if (!bgmDict.ContainsKey(clip.name))
                        bgmDict.Add(clip.name, clip.clip);
                    else
                        Debug.LogWarning($"[SoundManager] BGM 名稱重複: {clip.name}");
                }
            }
        }

        sfxDict = new Dictionary<string, AudioClip>();
        if (sfxClips != null)
        {
            foreach (var clip in sfxClips)
            {
                if (clip != null && !string.IsNullOrEmpty(clip.name) && clip.clip != null)
                {
                    if (!sfxDict.ContainsKey(clip.name))
                        sfxDict.Add(clip.name, clip.clip);
                    else
                        Debug.LogWarning($"[SoundManager] SFX 名稱重複: {clip.name}");
                }
            }
        }
    }

    private void BuildSceneBGMMap()
    {
        sceneBgmMap = new Dictionary<string, string>();
        if (sceneBGMBindings != null)
        {
            foreach (var binding in sceneBGMBindings)
            {
                if (binding != null && !string.IsNullOrEmpty(binding.sceneName) && !string.IsNullOrEmpty(binding.bgmName))
                {
                    sceneBgmMap[binding.sceneName] = binding.bgmName;
                }
            }
        }
    }

    /// <summary>
    /// 場景載入完成時自動切換 BGM。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (sceneBgmMap != null && sceneBgmMap.TryGetValue(scene.name, out string bgmName))
        {
            PlayBGM(bgmName);
        }
    }

    // ─── BGM API ────────────────────────────────────────────

    /// <summary>
    /// 播放指定名稱的 BGM（若已在播放相同 BGM 則忽略）。
    /// 若當前有 BGM 正在播放，會淡出後淡入新 BGM。
    /// </summary>
    public void PlayBGM(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("[SoundManager] PlayBGM: clipName 為空！");
            return;
        }

        if (clipName == currentBgmName && bgmSource.isPlaying)
            return;

        if (!bgmDict.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"[SoundManager] 找不到 BGM: {clipName}");
            return;
        }

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(FadeBGM(clip, clipName));
    }

    /// <summary>
    /// 停止播放 BGM（含淡出效果）。
    /// </summary>
    public void StopBGM()
    {
        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
    }

    /// <summary>
    /// 立即停止 BGM（無淡出）。
    /// </summary>
    public void StopBGMImmediate()
    {
        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmSource.Stop();
        currentBgmName = null;
    }

    /// <summary>
    /// 設定 BGM 音量。
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        bgmSource.volume = bgmVolume;
    }

    /// <summary>
    /// 取得當前 BGM 音量。
    /// </summary>
    public float GetBGMVolume() => bgmVolume;

    // ─── SFX API ────────────────────────────────────────────

    /// <summary>
    /// 播放指定名稱的 SFX（多聲道輪播）。
    /// </summary>
    public void PlaySFX(string clipName, float volumeScale = 1f)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("[SoundManager] PlaySFX: clipName 為空！");
            return;
        }

        if (!sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"[SoundManager] 找不到 SFX: {clipName}");
            return;
        }

        AudioSource source = sfxSources[currentSfxIndex];
        source.clip = clip;
        source.volume = sfxVolume * Mathf.Clamp01(volumeScale);
        source.Play();

        currentSfxIndex = (currentSfxIndex + 1) % sfxChannelCount;
    }

    /// <summary>
    /// 以 PlayOneShot 方式播放 SFX（不中斷其他正在播放的音效）。
    /// </summary>
    public void PlaySFXOneShot(string clipName, float volumeScale = 1f)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("[SoundManager] PlaySFXOneShot: clipName 為空！");
            return;
        }

        if (!sfxDict.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"[SoundManager] 找不到 SFX: {clipName}");
            return;
        }

        // 找一個目前沒在播放的 channel
        AudioSource source = GetAvailableSfxSource();
        source.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// 設定 SFX 音量。
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        foreach (var source in sfxSources)
        {
            source.volume = sfxVolume;
        }
    }

    /// <summary>
    /// 取得當前 SFX 音量。
    /// </summary>
    public float GetSFXVolume() => sfxVolume;

    // ─── 內部方法 ───────────────────────────────────────────

    private AudioSource GetAvailableSfxSource()
    {
        // 先找沒在播放的
        foreach (var source in sfxSources)
        {
            if (!source.isPlaying)
                return source;
        }

        // 都在播放就用輪播
        AudioSource fallback = sfxSources[currentSfxIndex];
        currentSfxIndex = (currentSfxIndex + 1) % sfxChannelCount;
        return fallback;
    }

    private IEnumerator FadeBGM(AudioClip newClip, string newName)
    {
        // 淡出
        if (bgmSource.isPlaying)
        {
            float startVol = bgmSource.volume;
            float elapsed = 0f;
            float halfDuration = bgmFadeDuration * 0.5f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                yield return null;
            }

            bgmSource.Stop();
        }

        // 切換並淡入
        bgmSource.clip = newClip;
        bgmSource.volume = 0f;
        bgmSource.Play();
        currentBgmName = newName;

        float fadeInElapsed = 0f;
        float fadeInDuration = bgmFadeDuration * 0.5f;

        while (fadeInElapsed < fadeInDuration)
        {
            fadeInElapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(0f, bgmVolume, fadeInElapsed / fadeInDuration);
            yield return null;
        }

        bgmSource.volume = bgmVolume;
        bgmFadeCoroutine = null;
    }

    private IEnumerator FadeOutBGM()
    {
        if (!bgmSource.isPlaying)
        {
            currentBgmName = null;
            yield break;
        }

        float startVol = bgmSource.volume;
        float elapsed = 0f;

        while (elapsed < bgmFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / bgmFadeDuration);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = bgmVolume;
        currentBgmName = null;
        bgmFadeCoroutine = null;
    }
}

/// <summary>
/// 音效片段資料結構，用於 Inspector 中設定名稱與對應的 AudioClip。
/// </summary>
[Serializable]
public class SoundClip
{
    [Tooltip("音效識別名稱（用於程式碼呼叫）")]
    public string name;

    [Tooltip("對應的 AudioClip")]
    public AudioClip clip;
}

/// <summary>
/// 場景 BGM 綁定資料結構。設定場景名稱對應的 BGM 名稱，
/// 切換到該場景時自動播放對應 BGM。
/// </summary>
[Serializable]
public class SceneBGMBinding
{
    [Tooltip("場景名稱（必須與 Build Settings 中的場景名稱一致）")]
    public string sceneName;

    [Tooltip("對應的 BGM 名稱（必須與 BGM 音效清單中的名稱一致）")]
    public string bgmName;
}
