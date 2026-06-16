using UnityEngine;

/// <summary>
/// Effect type of the mechanism.
/// Gate          = Gate: slides open when triggered + disables collision (passable) + changes color; closes again to block when released.
/// MovingPlatform = Moving platform/lift: moves to the target position when triggered, collision stays on throughout (can be stood on); moves back when released.
/// </summary>
public enum MechanismMode
{
    Gate,
    MovingPlatform
}

/// <summary>Movement direction when triggered. Uses customOffset for Custom.</summary>
public enum OpenDirection
{
    Up,
    Down,
    Left,
    Right,
    Custom
}

/// <summary>
/// Generic mechanism: triggered by a set of buttons, produces different effects depending on mode. Reusable.
/// Drag the buttons that control it into triggers, pick a mode, and set openOffset (where it moves to after triggering).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Mechanism : MonoBehaviour
{
    [Header("Trigger Conditions")]
    [Tooltip("The buttons that control this mechanism (i.e. the condition array)")]
    public PressButton[] triggers;

    [Tooltip("true = trigger only when all are pressed; false = trigger when any one is pressed")]
    public bool requireAll = true;

    [Header("Effect Type")]
    [Tooltip("Gate = gate (passable when open); MovingPlatform = moving platform (always stand-able)")]
    public MechanismMode mode = MechanismMode.Gate;

    [Header("Trigger Action")]
    [Tooltip("Movement direction when triggered (uses the custom offset below for Custom)")]
    public OpenDirection direction = OpenDirection.Up;

    [Tooltip("Movement distance (world units)")]
    public float distance = 2.7f;

    [Tooltip("Custom offset used when direction is Custom")]
    public Vector2 customOffset = new Vector2(0f, 3f);

    [Tooltip("Movement speed")]
    public float moveSpeed = 14f;

    [Header("Gate-only Visuals")]
    public Color closedColor = new Color(0.7f, 0.25f, 0.25f);
    public Color openColor = new Color(0.3f, 0.75f, 0.4f);

    /// <summary>Whether the mechanism is currently triggered (open).</summary>
    public bool IsActive { get; private set; }

    private Vector3 _closedPos;
    private Vector3 _openPos;
    private Collider2D _col;
    private SpriteRenderer _sr;
    private bool _prevActive;

    private void Awake()
    {
        _closedPos = transform.position;
        _openPos = _closedPos + (Vector3)GetOffset();
        _col = GetComponent<Collider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>Computes the offset after triggering, based on direction/distance (or Custom).</summary>
    public Vector2 GetOffset()
    {
        switch (direction)
        {
            case OpenDirection.Up: return Vector2.up * distance;
            case OpenDirection.Down: return Vector2.down * distance;
            case OpenDirection.Left: return Vector2.left * distance;
            case OpenDirection.Right: return Vector2.right * distance;
            default: return customOffset;
        }
    }

    private void Update()
    {
        IsActive = Evaluate();

        // Play a sound effect when the state changes
        if (IsActive && !_prevActive)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(mode == MechanismMode.Gate ? "GateOpen" : "PlatformMove");
        }
        else if (!IsActive && _prevActive)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX(mode == MechanismMode.Gate ? "GateClose" : "PlatformMove");
        }
        _prevActive = IsActive;

        Vector3 target = IsActive ? _openPos : _closedPos;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (mode == MechanismMode.Gate)
        {
            // Gate: passable when open + changes color
            if (_col != null) _col.enabled = !IsActive;
            if (_sr != null) _sr.color = IsActive ? openColor : closedColor;
        }
        else // MovingPlatform
        {
            // Moving platform: collision stays on so it can be stood on, no color change
            if (_col != null) _col.enabled = true;
        }
    }

    private bool Evaluate()
    {
        if (triggers == null || triggers.Length == 0) return false;

        if (requireAll)
        {
            foreach (var t in triggers)
                if (t == null || !t.IsPressed) return false;
            return true;
        }
        else
        {
            foreach (var t in triggers)
                if (t != null && t.IsPressed) return true;
            return false;
        }
    }
}
