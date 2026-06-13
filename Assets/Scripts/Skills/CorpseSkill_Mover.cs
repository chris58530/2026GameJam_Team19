using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 會移動的屍體 (左右橫擺 / 上下搖擺)。
/// - 使用 Kinematic Rigidbody2D,沿指定軸來回移動。
/// - 到達設定的單程距離端點時折返;若中途「碰到障礙物」也會提前折返。
/// - 會帶著站在上方的玩家一起移動 (與 MovingPlatform 同邏輯)。
/// 移動範圍固定,於套用技能時設定,之後不再變動。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CorpseSkill_Mover : MonoBehaviour
{
    public enum MoveAxis { Horizontal, Vertical }

    [Tooltip("移動軸向")]
    public MoveAxis axis = MoveAxis.Horizontal;

    [Tooltip("單程移動距離 (世界單位)")]
    public float distance = 3f;

    [Tooltip("移動速度 (單位 / 秒)")]
    public float speed = 2f;

    [Tooltip("哪些圖層算障礙物 (碰到會提前折返)")]
    public LayerMask obstacleMask = ~0;

    [Tooltip("帶動乘客用的玩家 Tag")]
    public string playerTag = "Player";

    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private Vector2 _startPos;
    private Vector2 _dir;
    private int _sign = 1;

    private readonly List<RaycastHit2D> _castHits = new List<RaycastHit2D>();
    private ContactFilter2D _filter;

    /// <summary>由管理器在生成屍體後呼叫,設定移動參數。</summary>
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

        // 障礙物偵測:沿移動方向投射自身 collider,碰到就提前折返 (本幀不移動)
        int count = _col.Cast(_dir * _sign, _filter, _castHits, step + 0.02f);
        if (count > 0)
        {
            _sign = -_sign;
            return;
        }

        Vector2 target = _rb.position + moveVec;

        // 端點折返:超過單程距離就夾住並反向
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

    /// <summary>帶著站在上方的玩家一起移動。</summary>
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
