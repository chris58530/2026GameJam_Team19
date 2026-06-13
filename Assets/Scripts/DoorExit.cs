using UnityEngine;

/// <summary>
/// 大門:玩家進入時,若已取得鑰匙則通關,否則提示需要鑰匙。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorExit : MonoBehaviour
{
    public string playerTag = "Player";

    private DeadBodyManager _manager;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
        _manager = FindFirstObjectByType<DeadBodyManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (_manager != null) _manager.TryExit();
    }
}
