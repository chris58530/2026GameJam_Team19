using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A platform that moves back and forth vertically (or horizontally).
/// - Uses a Kinematic Rigidbody2D + BoxCollider2D, placed on the Ground layer, so it can be stood on by the player and pressed by ghosts.
/// - Carries the player and ghosts standing "on top of the platform" along with it (uses OverlapBox to detect riders, moving step by step).
/// - All movement parameters are exposed in the Inspector for tuning.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis { Vertical, Horizontal }

    [Header("Movement Settings")]
    [Tooltip("Movement axis: vertical (up/down) or horizontal (left/right)")]
    public MoveAxis axis = MoveAxis.Vertical;

    [Tooltip("One-way travel distance (world units)")]
    public float distance = 3f;

    [Tooltip("Movement speed (units / sec)")]
    public float speed = 2f;

    [Tooltip("Seconds to wait after reaching each end")]
    public float endWaitTime = 0.5f;

    [Tooltip("Delay in seconds before movement starts")]
    public float startDelay = 0f;

    [Tooltip("Starting phase (0~1). Lets multiple platforms stagger their rhythm, e.g. 0.5 means starting from the middle.")]
    [Range(0f, 1f)]
    public float startPhase = 0f;

    [Tooltip("Whether to move in the positive direction (up / right) first")]
    public bool startMovingPositive = true;

    [Header("Rider Detection")]
    [Tooltip("Thickness of the rider detection zone above the platform top")]
    public float riderCheckHeight = 0.2f;

    [Tooltip("Which Tags are treated as riders and carried along (the player). Ghosts are always carried.")]
    public string playerTag = "Player";

    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private Vector2 _startPos;
    private Vector2 _dir;
    private float _t;          // Progress between the two ends, 0~1
    private int _sign;         // Current movement direction +1 / -1
    private float _waitTimer;
    private float _delayTimer;

    private readonly HashSet<Transform> _movedThisStep = new HashSet<Transform>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.useFullKinematicContacts = true;
    }

    private void Start()
    {
        _startPos = _rb.position;
        _dir = axis == MoveAxis.Vertical ? Vector2.up : Vector2.right;
        _sign = startMovingPositive ? 1 : -1;
        _t = Mathf.Clamp01(startPhase);
        _delayTimer = Mathf.Max(0f, startDelay);
    }

    private void FixedUpdate()
    {
        if (distance <= 0f || speed <= 0f) return;

        if (_delayTimer > 0f) { _delayTimer -= Time.fixedDeltaTime; return; }
        if (_waitTimer > 0f) { _waitTimer -= Time.fixedDeltaTime; return; }

        // Convert "speed / distance" into normalized progress per frame
        float step = (speed / distance) * Time.fixedDeltaTime;
        _t += _sign * step;

        if (_t >= 1f) { _t = 1f; _sign = -1; _waitTimer = endWaitTime; }
        else if (_t <= 0f) { _t = 0f; _sign = 1; _waitTimer = endWaitTime; }

        Vector2 target = _startPos + _dir * (distance * _t);
        Vector2 delta = target - _rb.position;
        if (delta.sqrMagnitude > 0f)
        {
            CarryRiders(delta);
            _rb.MovePosition(target);
        }
    }

    /// <summary>Detects riders standing above the platform and moves them by the same offset to carry them along.</summary>
    private void CarryRiders(Vector2 delta)
    {
        _movedThisStep.Clear();

        Bounds b = _col.bounds;
        Vector2 boxCenter = new Vector2(b.center.x, b.max.y + riderCheckHeight * 0.5f);
        Vector2 boxSize = new Vector2(b.size.x * 0.95f, riderCheckHeight);

        var hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        foreach (var h in hits)
        {
            if (h == null || h.gameObject == gameObject) continue;

            bool isRider = h.CompareTag(playerTag) || h.GetComponent<Ghost>() != null;
            if (!isRider) continue;

            Transform t = h.attachedRigidbody != null ? h.attachedRigidbody.transform : h.transform;
            if (!_movedThisStep.Add(t)) continue;

            if (h.attachedRigidbody != null && h.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                h.attachedRigidbody.position += delta;
            else
                t.position += (Vector3)delta;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Endpoints and travel preview
        Vector3 dir = axis == MoveAxis.Vertical ? Vector3.up : Vector3.right;
        Vector3 a = Application.isPlaying ? (Vector3)_startPos : transform.position;
        Vector3 c = a + dir * distance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, c);
        Gizmos.DrawWireSphere(a, 0.12f);
        Gizmos.DrawWireSphere(c, 0.12f);
    }
}
