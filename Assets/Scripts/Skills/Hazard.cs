using UnityEngine;

/// <summary>
/// Damage area (spikes / lava, etc.).
/// When the player touches it, this is treated as "death not caused by the player's own mechanics" -> it notifies the manager through ILevelFailHandler,
/// declaring the level failed and restarting. Attach it to an object with a Trigger Collider2D.
/// It does not depend directly on any specific manager.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [Tooltip("Player Tag")]
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
        _handler?.FailLevel("Stepped into hazard");
    }
}
