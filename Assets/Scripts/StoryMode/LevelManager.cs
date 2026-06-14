using UnityEngine;

/// <summary>
/// 關卡輔助腳本。每個關卡場景放一個。
/// 提供 OnLevelCleared() 和 OnLevelFailed() 方法，
/// 讓關卡中的任何機制（如碰到終點、時間到、碰到危險）都能觸發流程推進。
/// 
/// 設定方式：
///   1. 在每個關卡場景（Level01, Level02, Level03...）中建立空 GameObject
///   2. 命名為 "LevelManager"
///   3. 掛上此腳本
/// 
/// 使用範例（在其他腳本中呼叫）：
///   // 玩家到達終點時
///   LevelManager levelMgr = FindAnyObjectByType&lt;LevelManager&gt;();
///   levelMgr.OnLevelCleared();
/// 
///   // 或使用靜態捷徑（如果此腳本存在於場景中）
///   LevelManager.Instance.OnLevelCleared();
///   LevelManager.Instance.OnLevelFailed();
/// 
/// 整合現有 LoopManager：
///   在 LoopManager.TryExit() 中勝利時，呼叫 LevelManager.Instance.OnLevelCleared();
///   在 LoopManager.FailLevel() 中失敗時，呼叫 LevelManager.Instance.OnLevelFailed();
///   （或在 LoopManager 之外偵測 LoopManager.Won == true 時呼叫）
/// </summary>
public class LevelManager : MonoBehaviour
{
    /// <summary>場景中的 LevelManager 實例（每個場景一個）。</summary>
    public static LevelManager Instance { get; private set; }

    [Header("設定")]
    [Tooltip("通關後延遲幾秒再切換場景（用於播放通關動畫/音效，建議 2~3 秒讓玩家看到 CLEAR 文字）")]
    [SerializeField] private float clearDelay = 2.5f;

    [Tooltip("失敗後延遲幾秒再切換場景（用於播放失敗動畫/音效）")]
    [SerializeField] private float failDelay = 2f;

    private bool hasTriggered = false;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 關卡通關。呼叫此方法後會自動推進到下一關或結局。
    /// 
    /// 使用方式：
    ///   - 直接呼叫：LevelManager.Instance.OnLevelCleared()
    ///   - 或在 Inspector 中連接按鈕/事件到此方法
    /// </summary>
    public void OnLevelCleared()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[LevelManager] 關卡通關！目前第 {GetCurrentLevelDisplay()} 關。");

        if (clearDelay > 0f)
        {
            Invoke(nameof(ExecuteCompleteLevel), clearDelay);
        }
        else
        {
            ExecuteCompleteLevel();
        }
    }

    /// <summary>
    /// 關卡失敗。呼叫此方法後會自動前往 Fail Ending。
    /// 
    /// 使用方式：
    ///   - 直接呼叫：LevelManager.Instance.OnLevelFailed()
    ///   - 或在 Inspector 中連接按鈕/事件到此方法
    /// </summary>
    public void OnLevelFailed()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[LevelManager] 關卡失敗！目前第 {GetCurrentLevelDisplay()} 關。");

        if (failDelay > 0f)
        {
            Invoke(nameof(ExecuteFailLevel), failDelay);
        }
        else
        {
            ExecuteFailLevel();
        }
    }

    /// <summary>
    /// 重設觸發狀態（用於關卡內部重試，不切換場景的情況）。
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    // ========== 內部方法 ==========

    private void ExecuteCompleteLevel()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.CompleteLevel();
        }
        else
        {
            Debug.LogError("[LevelManager] StoryFlowManager 不存在！無法推進流程。");
        }
    }

    private void ExecuteFailLevel()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.FailLevel();
        }
        else
        {
            Debug.LogError("[LevelManager] StoryFlowManager 不存在！無法推進流程。");
        }
    }

    private string GetCurrentLevelDisplay()
    {
        if (StoryFlowManager.Instance != null)
            return $"{StoryFlowManager.Instance.CurrentLevelIndex + 1}/{StoryFlowManager.Instance.TotalLevelCount}";
        return "?/?";
    }
}
