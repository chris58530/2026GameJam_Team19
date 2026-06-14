using UnityEngine;

/// <summary>
/// Example level-complete trigger.
/// When the player reaches the goal (for example, touches this Collider), the level is completed.
/// 
/// Usage:
///   1. Create an empty GameObject in the level Prefab (e.g. "Goal")
///   2. Add a BoxCollider2D (Is Trigger = true) or BoxCollider (Is Trigger = true)
///   3. Attach this script
///   4. Set playerTag (defaults to "Player")
/// 
/// Note: this example also implements ILevelInitializable so it can receive a LevelRunContext.
/// </summary>
public class LevelCompleteTrigger : MonoBehaviour, ILevelInitializable
{
    [Header("Settings")]
    [Tooltip("The Tag of the player object")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Behavior after completion")]
    [SerializeField] private CompleteBehavior completeBehavior = CompleteBehavior.ReturnToLevelSelector;

    private LevelRunContext levelContext;
    private bool triggered = false;

    public enum CompleteBehavior
    {
        ReturnToLevelSelector,
        ReturnToMainMenu
    }

    /// <summary>
    /// Receives the level runtime data (called by GameSceneController).
    /// </summary>
    public void Initialize(LevelRunContext context)
    {
        levelContext = context;
        Debug.Log($"[LevelCompleteTrigger] Initialized. Level: {context.selectedLevelName}, Difficulty: {context.difficulty}");
    }

    // --- 2D Trigger ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            TriggerLevelComplete();
        }
    }

    // --- 3D Trigger ---
    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag(playerTag))
        {
            TriggerLevelComplete();
        }
    }

    /// <summary>
    /// Manually triggers level completion (can be called by other scripts).
    /// </summary>
    public void TriggerLevelComplete()
    {
        if (triggered) return;
        triggered = true;

        Debug.Log($"[LevelCompleteTrigger] Level complete! {levelContext?.selectedLevelName ?? "Unknown"}");

        // TODO: Add level-complete animation, sound effects, saving, and other logic here

        switch (completeBehavior)
        {
            case CompleteBehavior.ReturnToLevelSelector:
                if (GameSceneController.Instance != null)
                    GameSceneController.Instance.ReturnToLevelSelector();
                else if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.GoToLevelSelector();
                break;

            case CompleteBehavior.ReturnToMainMenu:
                if (GameSceneController.Instance != null)
                    GameSceneController.Instance.ReturnToMainMenu();
                else if (GameFlowManager.Instance != null)
                    GameFlowManager.Instance.GoToMainMenu();
                break;
        }
    }
}
