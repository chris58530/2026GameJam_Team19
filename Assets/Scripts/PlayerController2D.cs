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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;

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

        // 跳躍 (空白鍵 / 手把 A 鍵)
        _jumpAction = new InputAction("Jump", InputActionType.Button);
        _jumpAction.AddBinding("<Keyboard>/space");
        _jumpAction.AddBinding("<Gamepad>/buttonSouth");
        _jumpAction.performed += OnJumpPerformed;
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
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
        // 地面偵測
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        _isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        // 水平移動:直接設定 X 速度,保留重力造成的 Y 速度
        Vector2 velocity = _rb.linearVelocity;
        velocity.x = _moveInput * moveSpeed;
        _rb.linearVelocity = velocity;

        // 跳躍:只在地面上才允許
        if (_jumpRequested)
        {
            _jumpRequested = false;
            if (_isGrounded)
            {
                Vector2 v = _rb.linearVelocity;
                v.y = jumpForce;
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
