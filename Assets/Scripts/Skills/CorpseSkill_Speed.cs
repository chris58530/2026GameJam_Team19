using UnityEngine;

/// <summary>
/// Speed-boost corpse (only affects horizontal movement speed).
/// When the player's collider touches this corpse, it calls the player controller to apply a horizontal speed multiplier.
/// The full multiplier is held while in contact, and after leaving the player side decays it back to normal speed over time.
/// The corpse is otherwise a solid platform the player can stand on.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_Speed : MonoBehaviour
{
    [Tooltip("Horizontal movement speed multiplier")]
    public float speedMultiplier = 2f;

    [Tooltip("Amount the multiplier decays per second after leaving (e.g. 2 = goes from 2x back to 1x in 0.5 seconds)")]
    public float decayPerSecond = 2f;

    [Tooltip("Player Tag")]
    public string playerTag = "Player";

    public void Configure(float multiplier, float decay)
    {
        speedMultiplier = multiplier;
        decayPerSecond = decay;
    }

    private void OnCollisionEnter2D(Collision2D c) => TryBoost(c.collider);
    private void OnCollisionStay2D(Collision2D c) => TryBoost(c.collider);

    private void TryBoost(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag)) return;

        var pc = other.GetComponent<PlayerController2D>();
        if (pc == null && other.attachedRigidbody != null)
            pc = other.attachedRigidbody.GetComponent<PlayerController2D>();

        if (pc != null) pc.RefreshSpeedBoost(speedMultiplier, decayPerSecond);
    }
}
