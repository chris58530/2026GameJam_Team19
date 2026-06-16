using UnityEngine;

/// <summary>
/// Teleport-down corpse (portal).
/// - This corpse's collider is set to a Trigger and fires when the player passes through (enters the range).
/// - It casts a ray straight down from the corpse position to find the first "platform" (ground layer),
///   and teleports the player onto the top of that platform.
/// - It has a short built-in cooldown to avoid repeated teleports while the landing spot is still inside the range.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_TeleportDown : MonoBehaviour
{
    [Tooltip("Layer to detect platforms below (ground)")]
    public LayerMask groundMask = ~0;

    [Tooltip("Maximum distance to detect downward")]
    public float maxCastDistance = 50f;

    [Tooltip("Height offset of the player relative to the platform top after teleporting (about half the player's height)")]
    public float landingOffset = 0.6f;

    [Tooltip("Cooldown in seconds after teleporting")]
    public float cooldown = 0.5f;

    [Tooltip("Player Tag")]
    public string playerTag = "Player";

    private float _cooldownTimer;

    public void Configure(LayerMask ground)
    {
        groundMask = ground;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryTeleport(other);
    private void OnTriggerStay2D(Collider2D other) => TryTeleport(other);

    private void TryTeleport(Collider2D other)
    {
        if (_cooldownTimer > 0f) return;
        if (!other.CompareTag(playerTag)) return;

        Vector2 origin = transform.position;

        // Start the ray slightly below the corpse to avoid hitting itself (extra safety even though it's a Trigger)
        float skin = 0.05f;
        RaycastHit2D hit = Physics2D.Raycast(
            origin + Vector2.down * skin, Vector2.down, maxCastDistance, groundMask);

        if (hit.collider == null) return;
        if (hit.collider.gameObject == gameObject) return;

        Vector3 dest = new Vector3(hit.point.x, hit.point.y + landingOffset, other.transform.position.z);

        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.position = dest;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            other.transform.position = dest;
        }

        _cooldownTimer = cooldown;
    }
}
