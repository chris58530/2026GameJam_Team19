using UnityEngine;

/// <summary>
/// Level helper script. Place one in each level scene.
/// Provides OnLevelCleared() and OnLevelFailed() methods so that any mechanic in the level
/// (such as reaching the goal, a timer running out, or hitting a hazard) can trigger flow progression.
/// 
/// Setup:
///   1. In each level scene (Level01, Level02, Level03...), create an empty GameObject
///   2. Name it "LevelManager"
///   3. Attach this script
/// 
/// Usage example (called from other scripts):
///   // When the player reaches the goal
///   LevelManager levelMgr = FindAnyObjectByType&lt;LevelManager&gt;();
///   levelMgr.OnLevelCleared();
/// 
///   // Or use the static shortcut (if this script exists in the scene)
///   LevelManager.Instance.OnLevelCleared();
///   LevelManager.Instance.OnLevelFailed();
/// 
/// Integrating with the existing LoopManager:
///   On victory in LoopManager.TryExit(), call LevelManager.Instance.OnLevelCleared();
///   On failure in LoopManager.FailLevel(), call LevelManager.Instance.OnLevelFailed();
///   (or detect LoopManager.Won == true outside of LoopManager and call it then)
/// </summary>
public class LevelManager : MonoBehaviour
{
    /// <summary>The LevelManager instance in the scene (one per scene).</summary>
    public static LevelManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("How many seconds to delay before switching scenes after clearing (used to play a clear animation/sound; 2~3 seconds is recommended so the player can see the CLEAR text)")]
    [SerializeField] private float clearDelay = 2.5f;

    [Tooltip("How many seconds to delay before switching scenes after failing (used to play a fail animation/sound)")]
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
    /// Level cleared. After calling this method, it automatically advances to the next level or the ending.
    /// 
    /// Usage:
    ///   - Call directly: LevelManager.Instance.OnLevelCleared()
    ///   - Or connect a button/event to this method in the Inspector
    /// </summary>
    public void OnLevelCleared()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[LevelManager] Level cleared! Currently on level {GetCurrentLevelDisplay()}.");

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
    /// Level failed. After calling this method, it automatically goes to the Fail Ending.
    /// 
    /// Usage:
    ///   - Call directly: LevelManager.Instance.OnLevelFailed()
    ///   - Or connect a button/event to this method in the Inspector
    /// </summary>
    public void OnLevelFailed()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        Debug.Log($"[LevelManager] Level failed! Currently on level {GetCurrentLevelDisplay()}.");

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
    /// Reset the trigger state (used for retrying within a level without switching scenes).
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    // ========== Internal Methods ==========

    private void ExecuteCompleteLevel()
    {
        if (StoryFlowManager.Instance != null)
        {
            StoryFlowManager.Instance.CompleteLevel();
        }
        else
        {
            Debug.LogError("[LevelManager] StoryFlowManager does not exist! Cannot advance the flow.");
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
            Debug.LogError("[LevelManager] StoryFlowManager does not exist! Cannot advance the flow.");
        }
    }

    private string GetCurrentLevelDisplay()
    {
        if (StoryFlowManager.Instance != null)
            return $"{StoryFlowManager.Instance.CurrentLevelIndex + 1}/{StoryFlowManager.Instance.TotalLevelCount}";
        return "?/?";
    }
}
