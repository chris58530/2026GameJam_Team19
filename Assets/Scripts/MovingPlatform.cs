using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 上下 (或左右) 來回移動的平台。
/// - 使用 Kinematic Rigidbody2D + BoxCollider2D,放在 Ground 圖層,可被玩家踩、被殘影壓。
/// - 會帶著站在「平台上方」的玩家與殘影一起移動 (用 OverlapBox 偵測乘客,逐格平移)。
/// - 所有移動參數都開放在 Inspector 調整。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum MoveAxis { Vertical, Horizontal }

    [Header("移動設定")]
    [Tooltip("移動軸向:上下 (Vertical) 或左右 (Horizontal)")]
    public MoveAxis axis = MoveAxis.Vertical;

    [Tooltip("單程移動距離 (世界單位)")]
    public float distance = 3f;

    [Tooltip("移動速度 (單位 / 秒)")]
    public float speed = 2f;

    [Tooltip("到達兩端後停留的秒數")]
    public float endWaitTime = 0.5f;

    [Tooltip("開場延遲幾秒才開始移動")]
    public float startDelay = 0f;

    [Tooltip("起始相位 (0~1)。讓多個平台錯開節奏,例如 0.5 代表從中間開始。")]
    [Range(0f, 1f)]
    public float startPhase = 0f;

    [Tooltip("是否先往正方向 (上 / 右) 移動")]
    public bool startMovingPositive = true;

    [Header("乘客偵測")]
    [Tooltip("平台頂端往上偵測乘客的厚度")]
    public float riderCheckHeight = 0.2f;

    [Tooltip("哪些 Tag 會被當成乘客一起帶動 (玩家)。殘影 (Ghost) 一律會被帶動。")]
    public string playerTag = "Player";

    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private Vector2 _startPos;
    private Vector2 _dir;
    private float _t;          // 在兩端之間的進度 0~1
    private int _sign;         // 目前移動方向 +1 / -1
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

        // 以「速度 / 距離」換算每幀的正規化進度
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

    /// <summary>偵測站在平台上方的乘客,平移相同位移帶著他們走。</summary>
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
        // 端點與行程預覽
        Vector3 dir = axis == MoveAxis.Vertical ? Vector3.up : Vector3.right;
        Vector3 a = Application.isPlaying ? (Vector3)_startPos : transform.position;
        Vector3 c = a + dir * distance;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(a, c);
        Gizmos.DrawWireSphere(a, 0.12f);
        Gizmos.DrawWireSphere(c, 0.12f);
    }
}
