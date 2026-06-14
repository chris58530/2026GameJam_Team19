using UnityEngine;

/// <summary>
/// 玩家動畫控制器：根據 Rigidbody2D 狀態驅動 Animator 參數。
/// 需搭配 PlayerController2D 使用,掛在同一個 GameObject 上。
/// 
/// Animator 參數設定:
///   - Speed (float): 水平速度絕對值
///   - yVelocity (float): 垂直速度
///   - IsGrounded (bool): 是否在地面
///   - IsJumping (bool): 是否正在跳躍上升
///   - IsFalling (bool): 是否正在下墜
/// 
/// 建議的 Animator 狀態機:
///   Idle → Run (Speed > 0.01)
///   Run → Idle (Speed < 0.01)
///   Any State → Jump (IsJumping == true)
///   Any State → Fall (IsFalling == true)
///   Fall → Idle (IsGrounded == true)
///   Jump → Fall (yVelocity < 0)
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;

    // Animator 參數的 Hash (效能優化,避免每幀字串比對)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimYVelocity = Animator.StringToHash("yVelocity");
    private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");
    private static readonly int AnimIsFalling = Animator.StringToHash("IsFalling");

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

    private bool _isGrounded;
    private bool _wasGrounded;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (_animator == null || _rb == null) return;

        // 地面偵測
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        _wasGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer) != null;

        float horizontalSpeed = Mathf.Abs(_rb.linearVelocity.x);
        float yVelocity = _rb.linearVelocity.y;

        // 判斷跳躍與下墜狀態
        bool isJumping = !_isGrounded && yVelocity > 0.1f;
        bool isFalling = !_isGrounded && yVelocity < -0.1f;

        // 設定 Animator 參數
        _animator.SetFloat(AnimSpeed, horizontalSpeed);
        _animator.SetFloat(AnimYVelocity, yVelocity);
        _animator.SetBool(AnimIsGrounded, _isGrounded);
        _animator.SetBool(AnimIsJumping, isJumping);
        _animator.SetBool(AnimIsFalling, isFalling);

        // 角色翻轉
        if (autoFlip && _spriteRenderer != null)
        {
            if (_rb.linearVelocity.x > 0.01f)
                _spriteRenderer.flipX = false;
            else if (_rb.linearVelocity.x < -0.01f)
                _spriteRenderer.flipX = true;
        }
    }
}
