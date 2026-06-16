using UnityEngine;

/// <summary>
/// Button: detects whether a player or ghost is pressing on it from above (Physics2D.OverlapBox).
/// Does not need a solid collider; acts purely as a detection zone plus visual.
/// </summary>
public class PressButton : MonoBehaviour
{
    [Tooltip("Button ID (A/B/C)")]
    public string id = "A";

    [Tooltip("Tag used to identify the player")]
    public string playerTag = "Player";

    [Tooltip("Offset of the detection zone relative to the button center")]
    public Vector2 checkOffset = new Vector2(0f, 0.5f);

    [Tooltip("Detection zone size")]
    public Vector2 checkSize = new Vector2(1.0f, 1.0f);

    public bool IsPressed { get; private set; }

    private SpriteRenderer _sr;
    private Color _baseColor;
    private Vector3 _baseScale;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
        _baseScale = transform.localScale;
    }

    private void FixedUpdate()
    {
        Vector2 center = (Vector2)transform.position + checkOffset;
        var hits = Physics2D.OverlapBoxAll(center, checkSize, 0f);
        bool pressed = false;
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (h.CompareTag(playerTag) || h.GetComponent<Ghost>() != null)
            {
                pressed = true;
                break;
            }
        }

        // Play a sound effect when the state changes
        if (pressed && !IsPressed)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("ButtonPress");
        }
        else if (!pressed && IsPressed)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("ButtonRelease");
        }

        IsPressed = pressed;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (_sr != null)
            _sr.color = IsPressed ? Color.Lerp(_baseColor, Color.white, 0.5f) : _baseColor;

        // Squash down when pressed
        transform.localScale = IsPressed
            ? new Vector3(_baseScale.x, _baseScale.y * 0.5f, _baseScale.z)
            : _baseScale;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((Vector2)transform.position + checkOffset, checkSize);
    }
}
