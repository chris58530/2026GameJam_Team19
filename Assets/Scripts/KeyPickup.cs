using UnityEngine;

/// <summary>
/// Key: collected when the player touches it, then disables itself.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
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
        if (_manager != null) _manager.CollectKey();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX("KeyPickup");

        gameObject.SetActive(false);
    }
}
