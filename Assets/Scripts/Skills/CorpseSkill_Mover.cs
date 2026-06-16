using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A corpse that moves (horizontal sway / vertical sway).
/// - Uses a Kinematic Rigidbody2D and moves back and forth along the given axis.
/// - Reverses when it reaches the configured one-way distance endpoint; it also reverses early if it "hits an obstacle" along the way.
/// - Carries the player standing on top along with it (same logic as MovingPlatform).
/// The movement range is fixed: it is set when the skill is applied and never changes afterwards.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CorpseSkill_Mover : MonoBehaviour
{
    public enum MoveAxis { Horizontal, Vertical }

    [Tooltip("Movement axis")]
    public MoveAxis axis = MoveAxis.Horizontal;

    [Tooltip("One-way movement distance (world units)")]
    public float distance = 3f;

    [Tooltip("Movement speed (units / second)")]
    public float speed = 2f;

    [Tooltip("Which layers count as obstacles (hitting one reverses early)")]
    public LayerMask obstacleMask = ~0;

    [Tooltip("Player Tag used for carrying riders")]
    public string playerTag = "Player";

    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private Vector2 _startPos;
    private Vector2 _dir;
    private int _sign = 1;

    private readonly List<RaycastHit2D> _castHits = new List<RaycastHit2D>();
    private ContactFilter2D _filter;

    /// <summary>Called by the manager after spawning the corpse to set the movement parameters.</summary>
    public void Configure(MoveAxis moveAxis, float moveDistance, float moveSpeed, LayerMask obstacles)
    {
        axis = moveAxis;
        distance = moveDistance;
        speed = moveSpeed;
        obstacleMask = obstacles;
    }

    private void Awake()
    {
        _col = GetComponent<BoxCollider2D>();
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.useFullKinematicContacts = true;
    }

    private void Start()
    {
        _startPos = _rb.position;
        _dir = axis == MoveAxis.Horizontal ? Vector2.right : Vector2.up;

        _filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = obstacleMask,
            useTriggers = false
        };
    }

    private void FixedUpdate()
    {
        if (distance <= 0f || speed <= 0f) return;

        float step = speed * Time.fixedDeltaTime;
        Vector2 moveVec = _dir * (_sign * step);

        // Obstacle detection: cast our own collider along the movement direction; reverse early if it hits something (no movement this frame)
        int count = _col.Cast(_dir * _sign, _filter, _castHits, step + 0.02f);
        if (count > 0)
        {
            _sign = -_sign;
            return;
        }

        Vector2 target = _rb.position + moveVec;

        // Endpoint reversal: if it exceeds the one-way distance, clamp and reverse
        float traveled = Vector2.Dot(target - _startPos, _dir);
        if (traveled > distance)
        {
            target = _startPos + _dir * distance;
            _sign = -1;
        }
        else if (traveled < 0f)
        {
            target = _startPos;
            _sign = 1;
        }

        Vector2 delta = target - _rb.position;
        if (delta.sqrMagnitude > 0f)
        {
            CarryRiders(delta);
            _rb.MovePosition(target);
        }
    }

    /// <summary>Carries the player standing on top along with the platform.</summary>
    private void CarryRiders(Vector2 delta)
    {
        Bounds b = _col.bounds;
        Vector2 boxCenter = new Vector2(b.center.x, b.max.y + 0.1f);
        Vector2 boxSize = new Vector2(b.size.x * 0.95f, 0.2f);

        var hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        foreach (var h in hits)
        {
            if (h == null || h.gameObject == gameObject) continue;
            if (!h.CompareTag(playerTag)) continue;

            if (h.attachedRigidbody != null && h.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                h.attachedRigidbody.position += delta;
            else
                h.transform.position += (Vector3)delta;
        }
    }
}
