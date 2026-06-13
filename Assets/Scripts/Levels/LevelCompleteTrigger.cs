using UnityEngine;

/// <summary>
/// 關卡完成觸發器範例。
/// 當玩家到達目標（例如碰到這個 Collider）時，觸發關卡完成。
/// 
/// 使用方式：
///   1. 在關卡 Prefab 中建立空 GameObject（例如 "Goal"）
///   2. 加上 BoxCollider2D（Is Trigger = true）或 BoxCollider（Is Trigger = true）
///   3. 掛上此腳本
///   4. 設定 playerTag（預設為 "Player"）
/// 
/// 注意：此範例同時實作 ILevelInitializable，可接收 LevelRunContext。
/// </summary>
public class LevelCompleteTrigger : MonoBehaviour, ILevelInitializable
{
    [Header("設定")]
    [Tooltip("玩家物件的 Tag")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("完成後的行為")]
    [SerializeField] private CompleteBehavior completeBehavior = CompleteBehavior.ReturnToLevelSelector;

    private LevelRunContext levelContext;
    private bool triggered = false;

    public enum CompleteBehavior
    {
        ReturnToLevelSelector,
        ReturnToMainMenu
    }

    /// <summary>
    /// 接收關卡執行時資料（由 GameSceneController 呼叫）。
    /// </summary>
    public void Initialize(LevelRunContext context)
    {
        levelContext = context;
        Debug.Log($"[LevelCompleteTrigger] 已初始化。關卡: {context.selectedLevelName}, 難度: {context.difficulty}");
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
    /// 手動觸發關卡完成（可由其他腳本呼叫）。
    /// </summary>
    public void TriggerLevelComplete()
    {
        if (triggered) return;
        triggered = true;

        Debug.Log($"[LevelCompleteTrigger] 關卡完成！{levelContext?.selectedLevelName ?? "Unknown"}");

        // TODO: 這裡可以加入關卡完成動畫、音效、存檔等邏輯

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
