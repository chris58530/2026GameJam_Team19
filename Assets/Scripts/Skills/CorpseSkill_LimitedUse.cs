using UnityEngine;

/// <summary>
/// Limited-use corpse (frozen).
/// The corpse is normally a solid platform the player can stand on, but "each time it is stepped on / touched" counts as one use.
/// After maxUses uses are spent, the corpse disappears. The remaining uses are hinted via transparency:
/// each use spent drops the sprite's alpha by one step, and it is destroyed after the last use is spent.
/// A new contact is only counted after the player leaves and touches it again (staying in contact does not keep draining uses).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CorpseSkill_LimitedUse : MonoBehaviour
{
    [Tooltip("Total number of available uses")]
    [Min(1)]
    public int maxUses = 3;

    [Tooltip("Destroy delay after uses run out (seconds, 0 = immediate)")]
    public float destroyDelay = 0f;

    [Tooltip("Player Tag")]
    public string playerTag = "Player";

    private SpriteRenderer _sr;
    private int _usesLeft;
    private bool _playerInContact;
    private bool _consumed;

    public void Configure(int uses)
    {
        maxUses = Mathf.Max(1, uses);
    }

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        _usesLeft = Mathf.Max(1, maxUses);
        ApplyAlpha();
    }

    private void OnCollisionEnter2D(Collision2D c) => HandleEnter(c.collider);
    private void OnCollisionExit2D(Collision2D c) => HandleExit(c.collider);
    private void OnTriggerEnter2D(Collider2D other) => HandleEnter(other);
    private void OnTriggerExit2D(Collider2D other) => HandleExit(other);

    private void HandleEnter(Collider2D other)
    {
        if (_consumed) return;
        if (other == null || !other.CompareTag(playerTag)) return;
        if (_playerInContact) return; // a single contact only counts once

        _playerInContact = true;
        ConsumeOne();
    }

    private void HandleExit(Collider2D other)
    {
        if (other == null || !other.CompareTag(playerTag)) return;
        _playerInContact = false;
    }

    private void ConsumeOne()
    {
        _usesLeft--;
        if (_usesLeft <= 0)
        {
            _consumed = true;
            if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
            else Destroy(gameObject);
            return;
        }
        ApplyAlpha();
    }

    /// <summary>Sets transparency based on remaining uses: the fewer left, the more transparent (uses left = maxUses means fully opaque).</summary>
    private void ApplyAlpha()
    {
        if (_sr == null) return;
        float a = Mathf.Clamp01((float)_usesLeft / Mathf.Max(1, maxUses));
        Color c = _sr.color;
        c.a = a;
        _sr.color = c;
    }
}
