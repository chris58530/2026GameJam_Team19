using UnityEngine;

/// <summary>
/// Random-launch corpse (Budweiser).
/// As soon as the player's collider touches this corpse, the player is flung off at "a random angle in the upper half-circle".
/// The launch force reuses the player's own original jump force (jumpForce), so the distance/height match the original setting.
/// It has a short built-in cooldown to avoid repeated triggering within the same instant of contact.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_RandomLaunch : MonoBehaviour
{
    [Tooltip("Minimum random launch angle (degrees, measured counter-clockwise from +X axis; 90 = straight up)")]
    public float minAngle = 50f;

    [Tooltip("Maximum random launch angle (degrees)")]
    public float maxAngle = 130f;

    [Tooltip("Cooldown in seconds after triggering (avoids repeated launches within the same contact)")]
    public float cooldown = 0.4f;

    [Tooltip("Fallback force used when the player's original jump force can't be found")]
    public float fallbackForce = 14f;

    [Tooltip("Player Tag")]
    public string playerTag = "Player";

    private float _cooldownTimer;

    public void Configure(float min, float max, float cd)
    {
        minAngle = min;
        maxAngle = max;
        cooldown = cd;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D c) => TryLaunch(c.collider);
    private void OnCollisionStay2D(Collision2D c) => TryLaunch(c.collider);

    private void TryLaunch(Collider2D other)
    {
        if (_cooldownTimer > 0f) return;
        if (other == null || !other.CompareTag(playerTag)) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        // Force = player's original jump force (distance/height reuse the original setting)
        float force = fallbackForce;
        var pc = rb.GetComponent<PlayerController2D>();
        if (pc != null) force = pc.jumpForce;

        // Random direction in the upper half-circle
        float angleDeg = Random.Range(minAngle, maxAngle);
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        rb.linearVelocity = dir * force;
        _cooldownTimer = cooldown;
    }
}
