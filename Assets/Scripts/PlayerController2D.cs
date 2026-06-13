using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 基礎 2D 平台角色控制:左右移動 + 跳躍。
/// 使用新版 Input System,輸入綁定在程式內自訂(自給自足,不需在 Inspector 連線)。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("水平移動速度 (單位/秒)")]
    public float moveSpeed = 7f;

    [Tooltip("跳躍力道")]
    public float jumpForce = 14f;

    [Header("地面偵測")]
    [Tooltip("地面偵測的圓心相對玩家的偏移")]
    public Vector2 groundCheckOffset = new Vector2(0f, -0.55f);

    [Tooltip("地面偵測半徑")]
    public float groundCheckRadius = 0.2f;

    [Tooltip("哪些圖層算地面")]
    public LayerMask groundLayer = ~0;

    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private InputAction _jumpAction;

    private float _moveInput;
    private bool _jumpRequested;
    private bool _isGrounded;
    private Collider2D _groundCollider;

    // 速度加速 (來自加速屍體):X = 水平移動倍率, Y = 跳躍力倍率
    private Vector2 _speedMul = Vector2.one;
    private float _boostDecay = 2f;
    private Vector2 _pendingBoost = Vector2.one;
    private bool _hasPendingBoost;

    /// <summary>由加速屍體呼叫:接觸期間每物理幀刷新,維持滿倍率。</summary>
    public void RefreshSpeedBoost(Vector2 multiplier, float decayPerSecond)
    {
        _pendingBoost = multiplier;
        _boostDecay = decayPerSecond;
        _hasPendingBoost = true;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;

        // 零摩擦力材質:避免貼牆時被摩擦力「黏」在牆上而緩慢下滑。
        // Box2D 摩擦力為 sqrt(a*b),玩家這邊設 0,對任何牆面組合後都會是 0。
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            var noFriction = new PhysicsMaterial2D("Player_NoFriction")
            {
                friction = 0f,
                bounciness = 0f
            };
            col.sharedMaterial = noFriction;
        }

        // 水平移動 (A/D 與 左右方向鍵)
        _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
        _moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        _moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        _moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/leftStick/left")
            .With("Positive", "<Gamepad>/leftStick/right");

        // 跳躍 (W / 空白鍵 / 手把 A 鍵)
        _jumpAction = new InputAction("Jump", InputActionType.Button);
        _jumpAction.AddBinding("<Keyboard>/w");
        _jumpAction.AddBinding("<Keyboard>/upArrow");
        _jumpAction.AddBinding("<Keyboard>/space");
        _jumpAction.AddBinding("<Gamepad>/buttonSouth");
        _jumpAction.performed += OnJumpPerformed;
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        // 重新啟用時清除殘留的跳躍請求 (例如選卡暫停期間誤觸的跳)
        _jumpRequested = false;
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
    }

    private void OnDestroy()
    {
        _jumpAction.performed -= OnJumpPerformed;
        _moveAction.Dispose();
        _jumpAction.Dispose();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        _jumpRequested = true;
    }

    private void Update()
    {
        _moveInput = _moveAction.ReadValue<float>();
    }

    private void FixedUpdate()
    {
        // 地面偵測 (記錄踩到的 collider)
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        _groundCollider = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);
        _isGrounded = _groundCollider != null;

        // 速度加速:接觸加速屍體期間維持滿倍率,離開後逐軸遞減回 1
        if (_hasPendingBoost)
        {
            _speedMul = _pendingBoost;
            _hasPendingBoost = false;
        }
        else
        {
            _speedMul.x = Mathf.MoveTowards(_speedMul.x, 1f, _boostDecay * Time.fixedDeltaTime);
            _speedMul.y = Mathf.MoveTowards(_speedMul.y, 1f, _boostDecay * Time.fixedDeltaTime);
        }

        // 水平移動:套用 X 倍率,保留重力造成的 Y 速度
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = _moveInput * moveSpeed * _speedMul.x;
        _rb.linearVelocity = velocity;

        // 跳躍:只在地面上才允許,跳躍力套用 Y 倍率
        if (_jumpRequested)
        {
            _jumpRequested = false;
            if (_isGrounded)
            {
                Vector2 v = _rb.linearVelocity;
                v.y = jumpForce * _speedMul.y;
                _rb.linearVelocity = v;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
