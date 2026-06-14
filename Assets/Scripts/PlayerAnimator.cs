using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家動畫控制器：直接偵測按鍵輸入來驅動 Animator 參數。
/// 需搭配 PlayerController2D 使用,掛在同一個 GameObject 上。
/// 
/// Animator 參數設定:
///   - Speed (float): 水平輸入絕對值 (0 或 1)
///   - IsJumping (bool): 跳躍鍵按下且不在地面 (上升中)
///   - IsFalling (bool): 不在地面且正在下墜
///   - IsGrounded (bool): 是否在地面
///   - yVelocity (float): 垂直速度 (用於 Jump→Fall 過渡)
///   - Die (trigger): 按下 K 鍵自殺時觸發
/// 
/// 建議的 Animator 狀態機:
///   Idle → Run (Speed > 0.01)
///   Run → Idle (Speed < 0.01)
///   Any State → Jump (IsJumping == true)
///   Any State → Fall (IsFalling == true)
///   Fall → Idle (IsGrounded == true)
///   Jump → Fall (yVelocity < 0)
///   Any State → Die (Die trigger)
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    // Animator 參數 Hash
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimYVelocity = Animator.StringToHash("yVelocity");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int AnimIsFalling = Animator.StringToHash("IsFalling");
    private static readonly int AnimDie = Animator.StringToHash("Die");

    [Header("地面偵測 (與 PlayerController2D 保持一致)")]
    [Tooltip("地面偵測的圓心相對玩家的偏移")]
    public Vector2 groundCheckOffset = new Vector2(0f, -0.55f);

    [Tooltip("地面偵測半徑")]
    public float groundCheckRadius = 0.2f;

    [Tooltip("哪些圖層算地面")]
    public LayerMask groundLayer = ~0;

    [Header("翻轉設定")]
    [Tooltip("是否根據移動方向自動翻轉角色")]
    public bool autoFlip = true;

    // 輸入 Actions
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dieAction;

    private bool _isGrounded;
    private bool _jumpPressed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 移動輸入 (A/D, 方向鍵)
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

        // 跳躍輸入 (W, Space, 上方向鍵)
        _jumpAction = new InputAction("Jump", InputActionType.Button);
        _jumpAction.AddBinding("<Keyboard>/w");
        _jumpAction.AddBinding("<Keyboard>/upArrow");
        _jumpAction.AddBinding("<Keyboard>/space");
        _jumpAction.AddBinding("<Gamepad>/buttonSouth");
        _jumpAction.performed += _ => _jumpPressed = true;

        // 自殺輸入 (K)
        _dieAction = new InputAction("Die", InputActionType.Button);
        _dieAction.AddBinding("<Keyboard>/k");
        _dieAction.performed += _ => OnDiePressed();
    }

    private void OnEnable()
    {
        _moveAction.Enable();
        _jumpAction.Enable();
        _dieAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        _dieAction.Disable();
    }

    private void OnDestroy()
    {
        _moveAction?.Dispose();
        _jumpAction?.Dispose();
        _dieAction?.Dispose();
    }

    private void Update()
    {
        if (_animator == null || _rb == null) return;

        // 地面偵測
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        _isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer) != null;

        // 讀取移動輸入
        float moveInput = _moveAction.ReadValue<float>();
        float speed = Mathf.Abs(moveInput);

        float yVelocity = _rb.linearVelocity.y;

        // 跳躍狀態：按了跳躍鍵 + 不在地面 + 上升中
        bool isJumping = !_isGrounded && yVelocity > 0.1f;

        // 下墜狀態：不在地面 + 下降中
        bool isFalling = !_isGrounded && yVelocity < -0.1f;

        // 設定 Animator 參數
        _animator.SetFloat(AnimSpeed, speed);
        _animator.SetFloat(AnimYVelocity, yVelocity);
        _animator.SetBool(AnimIsGrounded, _isGrounded);
        _animator.SetBool(AnimIsJumping, isJumping);
        _animator.SetBool(AnimIsFalling, isFalling);

        // 角色翻轉 (根據輸入方向)
        if (autoFlip && _spriteRenderer != null)
        {
            if (moveInput > 0.01f)
                _spriteRenderer.flipX = false;
            else if (moveInput < -0.01f)
                _spriteRenderer.flipX = true;
        }

        // 重置跳躍按鍵狀態
        _jumpPressed = false;
    }

    private void OnDiePressed()
    {
        if (_animator != null)
            _animator.SetTrigger(AnimDie);
    }
}
