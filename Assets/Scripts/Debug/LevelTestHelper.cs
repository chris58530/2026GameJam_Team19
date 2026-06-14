using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Level testing helper script - use keyboard shortcuts to quickly test the clear flow.
/// 
/// Shortcuts:
///   P        = Clear the level directly / jump to the next level
///
/// Usage:
///   1. Create an empty GameObject in the Game0 (or any level) scene
///   2. Name it "LevelTestHelper"
///   3. Attach this script
///   4. Press P in Play mode to clear the level
///
/// Note: this script is for development testing only; remove or disable it in the release build.
/// </summary>
public class LevelTestHelper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Whether to enable this test script (shortcuts are disabled when off)")]
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

        // P = Clear the level directly / jump to the next level
        if (kb.pKey.wasPressedThisFrame)
        {
            ForceVictory();
        }
    }

    /// <summary>Directly triggers victory / level clear.</summary>
    private void ForceVictory()
    {
        // Try LoopManager first (used by Game0)
        if (_loopManager != null)
        {
            _loopManager.TryExit();

            // If the door isn't open, TryExit won't trigger victory; fall back to LevelManager
            if (!_loopManager.Won)
            {
                Debug.Log("[TestHelper] LoopManager door not open, calling LevelManager.OnLevelCleared() directly");
                TriggerLevelComplete();
            }
            return;
        }

        // Otherwise trigger level completion directly
        TriggerLevelComplete();
    }

    /// <summary>Triggers level completion (generic).</summary>
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
            Debug.LogWarning("[TestHelper] Could not find LevelManager or StoryFlowManager!");
        }
    }
}
