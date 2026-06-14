using UnityEngine;

/// <summary>
/// SoundManager 測試腳本。
/// 掛到場景中的任意 GameObject 上，用鍵盤快速測試所有音效。
/// 
/// 使用方式：
///   1. 在任意場景建立空 GameObject，命名 "SoundTester"
///   2. 掛上此腳本
///   3. 確保場景中有 SoundManager
///   4. Play 後按對應按鍵即可測試
/// 
/// 按鍵對照（顯示在 Game 視窗左上角）：
///   Alpha1~9, 0  = 測試 SFX
///   F1~F5        = 測試 BGM
///   -/+          = 調整音量
/// </summary>
public class SoundManagerTester : MonoBehaviour
{
    [Header("SFX 測試對照表（依序對應數字鍵 1~0）")]
    [Tooltip("最多 10 個 SFX 名稱，對應鍵盤 1234567890")]
    [SerializeField] private string[] sfxTestNames = new string[]
    {
        "Jump",
        "Land",
        "Die",
        "Fail",
        "KeyPickup",
        "LevelClear",
        "ButtonPress",
        "GateOpen",
        "UIClick",
        "DoorLocked"
    };

    [Header("BGM 測試對照表（依序對應 F1~F5）")]
    [Tooltip("最多 5 個 BGM 名稱，對應 F1~F5")]
    [SerializeField] private string[] bgmTestNames = new string[]
    {
        "TitleBGM",
        "GameBGM",
        "BossBGM",
        "",
        ""
    };

    [Header("音量調整步進")]
    [SerializeField] private float volumeStep = 0.1f;

    private string _lastPlayed = "";
    private float _lastPlayedTimer;

    private readonly KeyCode[] numberKeys = new KeyCode[]
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        KeyCode.Alpha0
    };

    private readonly KeyCode[] fKeys = new KeyCode[]
    {
        KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5
    };

    private void Update()
    {
        if (SoundManager.Instance == null) return;

        // SFX 測試：數字鍵 1~0
        for (int i = 0; i < numberKeys.Length && i < sfxTestNames.Length; i++)
        {
            if (Input.GetKeyDown(numberKeys[i]) && !string.IsNullOrEmpty(sfxTestNames[i]))
            {
                SoundManager.Instance.PlaySFX(sfxTestNames[i]);
                SetLastPlayed($"SFX: {sfxTestNames[i]}");
            }
        }

        // BGM 測試：F1~F5
        for (int i = 0; i < fKeys.Length && i < bgmTestNames.Length; i++)
        {
            if (Input.GetKeyDown(fKeys[i]) && !string.IsNullOrEmpty(bgmTestNames[i]))
            {
                SoundManager.Instance.PlayBGM(bgmTestNames[i]);
                SetLastPlayed($"BGM: {bgmTestNames[i]}");
            }
        }

        // F6 = 停止 BGM
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SoundManager.Instance.StopBGM();
            SetLastPlayed("BGM 停止（淡出）");
        }

        // F7 = 立即停止 BGM
        if (Input.GetKeyDown(KeyCode.F7))
        {
            SoundManager.Instance.StopBGMImmediate();
            SetLastPlayed("BGM 立即停止");
        }

        // - / = 調整 BGM 音量
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            float vol = SoundManager.Instance.GetBGMVolume() - volumeStep;
            SoundManager.Instance.SetBGMVolume(vol);
            SetLastPlayed($"BGM 音量: {SoundManager.Instance.GetBGMVolume():F1}");
        }
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            float vol = SoundManager.Instance.GetBGMVolume() + volumeStep;
            SoundManager.Instance.SetBGMVolume(vol);
            SetLastPlayed($"BGM 音量: {SoundManager.Instance.GetBGMVolume():F1}");
        }

        // [ / ] 調整 SFX 音量
        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            float vol = SoundManager.Instance.GetSFXVolume() - volumeStep;
            SoundManager.Instance.SetSFXVolume(vol);
            SetLastPlayed($"SFX 音量: {SoundManager.Instance.GetSFXVolume():F1}");
        }
        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            float vol = SoundManager.Instance.GetSFXVolume() + volumeStep;
            SoundManager.Instance.SetSFXVolume(vol);
            SetLastPlayed($"SFX 音量: {SoundManager.Instance.GetSFXVolume():F1}");
        }

        // 計時器
        if (_lastPlayedTimer > 0f)
            _lastPlayedTimer -= Time.unscaledDeltaTime;
    }

    private void SetLastPlayed(string msg)
    {
        _lastPlayed = msg;
        _lastPlayedTimer = 3f;
    }

    private void OnGUI()
    {
        if (SoundManager.Instance == null)
        {
            GUI.Label(new Rect(10, 10, 400, 30), "⚠ SoundManager 不存在！請確認場景中有 SoundManager。");
            return;
        }

        float y = 10f;
        float lineHeight = 22f;

        // 標題
        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(10, y, 400, 25), "🔊 SoundManager Tester", titleStyle);
        y += 30f;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        style.normal.textColor = Color.white;

        // SFX
        GUI.Label(new Rect(10, y, 400, lineHeight), "── SFX（數字鍵 1~0）──", style);
        y += lineHeight;
        for (int i = 0; i < sfxTestNames.Length && i < 10; i++)
        {
            if (!string.IsNullOrEmpty(sfxTestNames[i]))
            {
                string key = i == 9 ? "0" : (i + 1).ToString();
                GUI.Label(new Rect(10, y, 400, lineHeight), $"  [{key}] {sfxTestNames[i]}", style);
                y += lineHeight;
            }
        }

        y += 5f;

        // BGM
        GUI.Label(new Rect(10, y, 400, lineHeight), "── BGM（F1~F5）──", style);
        y += lineHeight;
        for (int i = 0; i < bgmTestNames.Length && i < 5; i++)
        {
            if (!string.IsNullOrEmpty(bgmTestNames[i]))
            {
                GUI.Label(new Rect(10, y, 400, lineHeight), $"  [F{i + 1}] {bgmTestNames[i]}", style);
                y += lineHeight;
            }
        }
        GUI.Label(new Rect(10, y, 400, lineHeight), "  [F6] 停止 BGM（淡出）", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), "  [F7] 立即停止 BGM", style);
        y += lineHeight + 5f;

        // 音量控制
        GUI.Label(new Rect(10, y, 400, lineHeight), "── 音量 ──", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight),
            $"  [-/+] BGM 音量: {SoundManager.Instance.GetBGMVolume():F1}", style);
        y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight),
            $"  [ / ] SFX 音量: {SoundManager.Instance.GetSFXVolume():F1}", style);
        y += lineHeight + 10f;

        // 最後播放
        if (_lastPlayedTimer > 0f)
        {
            var playedStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            playedStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, y, 500, 25), $"▶ {_lastPlayed}", playedStyle);
        }
    }
}
