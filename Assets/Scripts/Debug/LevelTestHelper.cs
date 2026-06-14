using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 關卡測試輔助腳本 — 用鍵盤快捷鍵快速測試通關流程。
/// 
/// 快捷鍵：
///   P        = 直接過關 / 跳到下一關
///
/// 使用方式：
///   1. 在 Game0 (或任何關卡) 場景中建立空 GameObject
///   2. 命名為 "LevelTestHelper"
///   3. 掛上此腳本
///   4. Play 模式中按 P 即可過關
///
/// 注意：此腳本僅供開發測試，正式版本請移除或停用。
/// </summary>
public class LevelTestHelper : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("是否啟用此測試腳本 (關閉後快捷鍵無效)")]
    [SerializeField] private bool enableTestKeys = true;

    private LoopManager _loopManager;
    private DeadBodyManager _deadBodyManager;

    private void Start()
    {
        _loopManager = FindAnyObjectByType<LoopManager>();
        _deadBodyManager = FindAnyObjectByType<DeadBodyManager>();
    }

    private void Update()
    {
        if (!enableTestKeys) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // P = 直接過關 / 跳到下一關
        if (kb.pKey.wasPressedThisFrame)
        {
            ForceVictory();
        }
    }

    /// <summary>直接觸發勝利 / 過關。</summary>
    private void ForceVictory()
    {
        // 優先嘗試 LoopManager (Game0 使用)
        if (_loopManager != null)
        {
            _loopManager.TryExit();

            // 如果門沒開,TryExit 不會觸發勝利,改用 LevelManager
            if (!_loopManager.Won)
            {
                Debug.Log("[TestHelper] LoopManager 門未開,直接呼叫 LevelManager.OnLevelCleared()");
                TriggerLevelComplete();
            }
            return;
        }

        // 其餘情況直接觸發關卡完成
        TriggerLevelComplete();
    }

    /// <summary>觸發關卡完成 (通用)。</summary>
    private void TriggerLevelComplete()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.OnLevelCleared();
        }
        else if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.CompleteLevel();
        }
        else
        {
            Debug.LogWarning("[TestHelper] 找不到 LevelManager 或 StoryFlowManager！");
        }
    }
}
