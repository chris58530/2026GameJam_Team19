using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 關卡測試輔助腳本 — 用鍵盤快捷鍵快速測試按鈕觸發與通關流程。
/// 
/// 快捷鍵：
///   T        = 強制觸發所有按鈕 (模擬全部被壓下)
///   Y        = 直接觸發勝利 / 過關 (呼叫 LevelManager 或 LoopManager)
///   U        = 觸發失敗
///   N        = 跳到下一關 (透過 StoryFlowManager.CompleteLevel)
///   P        = 印出當前關卡狀態
///
/// 使用方式：
///   1. 在 Game0 (或任何關卡) 場景中建立空 GameObject
///   2. 命名為 "LevelTestHelper"
///   3. 掛上此腳本
///   4. Play 模式中按快捷鍵即可測試
///
/// 注意：此腳本僅供開發測試，正式版本請移除或停用。
/// </summary>
public class LevelTestHelper : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("是否在遊戲畫面顯示快捷鍵提示")]
    [SerializeField] private bool showHints = true;

    [Tooltip("是否啟用此測試腳本 (關閉後所有快捷鍵無效)")]
    [SerializeField] private bool enableTestKeys = true;

    private LoopManager _loopManager;
    private DeadBodyManager _deadBodyManager;
    private PressButton[] _allButtons;

    private string _statusMessage = "";
    private float _statusTimer;

    private void Start()
    {
        _loopManager = FindAnyObjectByType<LoopManager>();
        _deadBodyManager = FindAnyObjectByType<DeadBodyManager>();
        _allButtons = FindObjectsByType<PressButton>(FindObjectsSortMode.None);

        ShowStatus($"[TestHelper] 已啟動。找到 {_allButtons.Length} 個按鈕。");
    }

    private void Update()
    {
        if (!enableTestKeys) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // T = 強制觸發所有按鈕
        if (kb.tKey.wasPressedThisFrame)
        {
            ForceAllButtonsPressed();
        }

        // Y = 直接觸發勝利 / 過關
        if (kb.yKey.wasPressedThisFrame)
        {
            ForceVictory();
        }

        // U = 觸發失敗
        if (kb.uKey.wasPressedThisFrame)
        {
            ForceFail();
        }

        // N = 跳到下一關 (Story Mode)
        if (kb.nKey.wasPressedThisFrame)
        {
            SkipToNextLevel();
        }

        // P = 印出當前狀態
        if (kb.pKey.wasPressedThisFrame)
        {
            PrintStatus();
        }

        if (_statusTimer > 0f) _statusTimer -= Time.unscaledDeltaTime;
    }

    /// <summary>強制讓所有按鈕被視為「被壓下」。</summary>
    private void ForceAllButtonsPressed()
    {
        if (_allButtons == null || _allButtons.Length == 0)
        {
            _allButtons = FindObjectsByType<PressButton>(FindObjectsSortMode.None);
        }

        // 直接把玩家移到按鈕上方來觸發
        // 改用更直接的方式: 直接觸發通關 (因為按鈕是物理偵測,無法在程式中假裝壓下)
        ShowStatus($"[TestHelper] 場景有 {_allButtons.Length} 個按鈕。按鈕為物理偵測,建議直接按 Y 過關。");

        // 列出按鈕狀態
        foreach (var btn in _allButtons)
        {
            if (btn != null)
                Debug.Log($"  Button [{btn.id}] IsPressed={btn.IsPressed} pos={btn.transform.position}");
        }
    }

    /// <summary>直接觸發勝利。</summary>
    private void ForceVictory()
    {
        // 優先嘗試 LoopManager (Game0 使用)
        if (_loopManager != null)
        {
            // 強制設定門為開 + 呼叫 TryExit
            // 由於 DoorOpen 是唯讀的,直接模擬通關
            _loopManager.TryExit();

            // 如果門沒開,TryExit 不會觸發勝利,改用 LevelManager
            if (!_loopManager.Won)
            {
                Debug.Log("[TestHelper] LoopManager 門未開,直接呼叫 LevelManager.OnLevelCleared()");
                TriggerLevelComplete();
            }
            else
            {
                ShowStatus("[TestHelper] ✓ 已觸發勝利 (LoopManager.TryExit)");
            }
            return;
        }

        // 嘗試 DeadBodyManager
        if (_deadBodyManager != null)
        {
            TriggerLevelComplete();
            ShowStatus("[TestHelper] ✓ 已觸發勝利 (LevelManager)");
            return;
        }

        // 直接呼叫 LevelManager
        TriggerLevelComplete();
    }

    /// <summary>觸發關卡完成 (通用)。</summary>
    private void TriggerLevelComplete()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelCleared();
            ShowStatus("[TestHelper] ✓ LevelManager.OnLevelCleared() 已呼叫");
        }
        else if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.CompleteLevel();
            ShowStatus("[TestHelper] ✓ StoryFlowManager.CompleteLevel() 已呼叫");
        }
        else
        {
            ShowStatus("[TestHelper] ✗ 找不到 LevelManager 或 StoryFlowManager！");
            Debug.LogWarning("[TestHelper] 找不到 LevelManager 或 StoryFlowManager！");
        }
    }

    /// <summary>觸發失敗。</summary>
    private void ForceFail()
    {
        if (_loopManager != null)
        {
            _loopManager.FailLevel("[TestHelper] 強制失敗");
            ShowStatus("[TestHelper] ✓ 已觸發失敗 (LoopManager)");
        }
        else if (_deadBodyManager != null)
        {
            _deadBodyManager.FailLevel("[TestHelper] 強制失敗");
            ShowStatus("[TestHelper] ✓ 已觸發失敗 (DeadBodyManager)");
        }
        else if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelFailed();
            ShowStatus("[TestHelper] ✓ 已觸發失敗 (LevelManager)");
        }
        else
        {
            ShowStatus("[TestHelper] ✗ 找不到任何關卡管理器！");
        }
    }

    /// <summary>跳到下一關 (Story Mode)。</summary>
    private void SkipToNextLevel()
    {
        if (StoryFlowManager.Instance != null)
        {
            int current = StoryFlowManager.Instance.CurrentLevelIndex;
            StoryFlowManager.Instance.CompleteLevel();
            ShowStatus($"[TestHelper] ✓ 跳過關卡 {current + 1} → 下一關");
        }
        else if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelCleared();
            ShowStatus("[TestHelper] ✓ 已呼叫 OnLevelCleared (跳關)");
        }
        else
        {
            ShowStatus("[TestHelper] ✗ 找不到 StoryFlowManager！");
        }
    }

    /// <summary>印出當前關卡狀態。</summary>
    private void PrintStatus()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string info = $"場景={scene}";

        if (StoryFlowManager.Instance != null)
            info += $" | 關卡索引={StoryFlowManager.Instance.CurrentLevelIndex}/{StoryFlowManager.Instance.TotalLevelCount}";

        if (_loopManager != null)
            info += $" | Loop={_loopManager.LoopCount} Won={_loopManager.Won} DoorOpen={_loopManager.DoorOpen} Pressed={_loopManager.PressedCount}";

        if (_deadBodyManager != null)
            info += $" | Deaths={_deadBodyManager.Deaths} Won={_deadBodyManager.Won} HasKey={_deadBodyManager.HasKey}";

        if (_allButtons != null)
        {
            int pressedCount = 0;
            foreach (var b in _allButtons)
                if (b != null && b.IsPressed) pressedCount++;
            info += $" | 按鈕={pressedCount}/{_allButtons.Length}";
        }

        Debug.Log($"[TestHelper Status] {info}");
        ShowStatus(info);
    }

    private void ShowStatus(string msg)
    {
        _statusMessage = msg;
        _statusTimer = 4f;
        Debug.Log(msg);
    }

    private void OnGUI()
    {
        if (!showHints) return;

        float y = Screen.height - 180f;
        var bgStyle = new GUIStyle(GUI.skin.box);

        // 快捷鍵提示區
        GUI.Box(new Rect(8, y, 320, 170), "");

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        titleStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(14, y + 4, 300, 22), "🛠 Test Keys (LevelTestHelper)", titleStyle);

        var style = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        style.normal.textColor = Color.white;

        float ly = y + 28;
        float lh = 20f;
        GUI.Label(new Rect(14, ly, 300, lh), "[T] 查看按鈕狀態", style); ly += lh;
        GUI.Label(new Rect(14, ly, 300, lh), "[Y] 強制勝利 / 過關", style); ly += lh;
        GUI.Label(new Rect(14, ly, 300, lh), "[U] 強制失敗", style); ly += lh;
        GUI.Label(new Rect(14, ly, 300, lh), "[N] 跳到下一關 (Story Mode)", style); ly += lh;
        GUI.Label(new Rect(14, ly, 300, lh), "[P] 印出當前狀態", style); ly += lh;
        GUI.Label(new Rect(14, ly, 300, lh), "[K] 死亡/留屍  [R] 重置關卡", style); ly += lh;

        // 狀態訊息
        if (_statusTimer > 0f)
        {
            var msgStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            msgStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(14, ly + 8, 600, 22), _statusMessage, msgStyle);
        }
    }
}
