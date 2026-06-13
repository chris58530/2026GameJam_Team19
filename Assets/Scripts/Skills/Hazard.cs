using UnityEngine;

/// <summary>
/// 傷害區域 (尖刺 / 岩漿 等)。
/// 玩家碰到時,視為「非自身機制導致的死亡」→ 透過 ILevelFailHandler 通知管理器,
/// 宣告關卡失敗並重來。掛在帶有 Trigger Collider2D 的物件上。
/// 不直接依賴任何特定管理器。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private ILevelFailHandler _handler;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        _handler = FindFailHandler();
    }

    private static ILevelFailHandler FindFailHandler()
    {
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
            if (mb is ILevelFailHandler h) return h;
        return null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_handler == null) _handler = FindFailHandler();
        _handler?.FailLevel("踏入傷害區域");
    }
}
