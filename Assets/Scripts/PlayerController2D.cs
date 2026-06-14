using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// 基礎 2D 平台角色控制:左右移動 + 跳躍。
/// 使用新版 Input System,輸入綁定在程式內自訂(自給自足,不需在 Inspector 連線)。
///
/// 手感強化:
/// - 加減速移動 (acceleration / deceleration),避免瞬間切速度的生硬感。
/// - Coyote time:離開地面後短暫時間內仍可跳。
/// - Jump buffer:落地前按跳會被記住,落地瞬間自動執行。
/// - 變動跳躍高度:短按小跳、長按高跳。
/// - 上升/下降不同重力,跳躍更俐落不飄。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("水平移動速度 (單位/秒)")]
    public float moveSpeed = 7f;

    [Tooltip("地面加速度 (越大越俐落,越小越滑)")]
    public float acceleration = 80f;

    [Tooltip("地面減速度 (放開方向鍵時的煞車力)")]
    public float deceleration = 100f;

    [Tooltip("空中操控倍率 (0~1,1 = 與地面相同)")]
    [Range(0f, 1f)]
    public float airControl = 0.65f;

    [Header("跳躍設定")]
    [Tooltip("跳躍力道")]
    public float jumpForce = 14f;

    [Tooltip("下降時的重力倍率 (越大下墜越快、越不飄)")]
    public float fallGravityMultiplier = 1.8f;

    [Tooltip("上升中放開跳躍鍵的重力倍率 (做出短跳)")]
    public float lowJumpMultiplier = 3f;

    [Tooltip("離開地面後仍可跳的寬限時間 (秒)")]
    public float coyoteTime = 0.1f;

    [Tooltip("落地前提早按跳的緩衝時間 (秒)")]
    public float jumpBufferTime = 0.1f;

    [Header("地面偵測")]
    [Tooltip("地面偵測的圓心相對玩家的偏移")]
    public Vector2 groundCheckOffset = new Vector2(0f, -0.55f);

    [Tooltip("地面偵測半徑")]
    public float groundCheckRadius = 0.2f;

    [Tooltip("哪些圖層算地面")]
    public LayerMask groundLayer = ~0;

    [Header("動畫")]
    [Tooltip("角色 Animator (留空會自動在自身或子物件尋找)")]
    public Animator animator;

    [Tooltip("Idle 動畫狀態名稱")]
    public string idleState = "idle";

    [Tooltip("移動動畫狀態名稱")]
    public string moveState = "moving";

    [Tooltip("跳躍 (上升) 動畫狀態名稱")]
    public string jumpState = "jump";

    [Tooltip("死亡 / 變屍體動畫狀態名稱")]
    public string fallState = "fall";

    [Tooltip("判定為移動的最小水平速度")]
    public float moveAnimThreshold = 0.1f;

    [Tooltip("依移動方向左右翻轉角色圖片")]
    public bool flipSpriteByDirection = true;

    [Header("回饋感 (Juice,純視覺,不影響碰撞/邏輯)")]
    [Tooltip("是否啟用跳躍/落地的縮放彈性、塵土粒子與輕微螢幕震動")]
    public bool enableJuice = true;

    [Tooltip("起跳時的縱向拉伸 (x 變窄, y 變高)")]
    public Vector2 jumpSquash = new Vector2(0.8f, 1.25f);

    [Tooltip("落地時的壓扁 (x 變寬, y 變矮)")]
    public Vector2 landSquash = new Vector2(1.28f, 0.72f);

    [Tooltip("判定為「重摔落地」的最小下墜速度 (越大越難觸發強回饋)")]
    public float hardLandSpeed = 12f;

    private Rigidbody2D _rb;
    private SpriteRenderer _spriteRenderer;
    private Transform _visual;          // 承載 squash 的視覺子物件 (與 Collider 分離)
    private Vector3 _visualBaseScale = Vector3.one;
    private float _fallSpeed;           // 落地前的下墜速度 (取絕對值),用來決定回饋強度
    private int _currentStateHash;
    private bool _isDead;
    private InputAction _moveAction;
    private InputAction _jumpAction;

    private float _moveInput;
    private bool _isGrounded;
    private Collider2D _groundCollider;

    private float _baseGravityScale;
    private float _coyoteTimer;
    private float _jumpBufferTimer;

    // 速度加速 (來自加速屍體):純粹影響水平移動倍率
    private float _speedMul = 1f;
    private float _boostDecay = 2f;
    private float _pendingBoost = 1f;
    private bool _hasPendingBoost;

    /// <summary>由加速屍體呼叫:接觸期間每物理幀刷新,維持滿倍率(僅影響水平移動)。</summary>
    public void RefreshSpeedBoost(float multiplier, float decayPerSecond)
    {
        _pendingBoost = multiplier;
        _boostDecay = decayPerSecond;
        _hasPendingBoost = true;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.freezeRotation = true;
        _baseGravityScale = _rb.gravityScale;

        // 動畫元件:未指定就自動尋找 (含子物件)
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // squash/stretch 作用在 SpriteRenderer 所在的視覺子物件 (Player/2D),
        // 與根物件上的 Collider 分離,縮放它不會影響碰撞。
        if (_spriteRenderer != null && _spriteRenderer.transform != transform)
        {
            _visual = _spriteRenderer.transform;
            _visualBaseScale = _visual.localScale;
        }

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
        // 重新啟用時清除殘留的跳躍緩衝 (例如選卡暫停期間誤觸的跳)
        _jumpBufferTimer = 0f;
        _coyoteTimer = 0f;
        // 重生 / 恢復控制:解除死亡鎖定,並從 idle 動畫開始
        _isDead = false;
        // 視覺縮放歸位 (避免重生時卡在 squash 中途)
        if (_visual != null)
        {
            _visual.DOKill();
            _visual.localScale = _visualBaseScale;
        }
        PlayState(idleState);
    }

    private void OnDisable()
    {
        _moveAction.Disable();
        _jumpAction.Disable();
        // 停用 (死亡 / 選卡) 時收掉縮放動畫並歸位,避免殘留
        if (_visual != null)
        {
            _visual.DOKill();
            _visual.localScale = _visualBaseScale;
        }
    }

    private void OnDestroy()
    {
        _jumpAction.performed -= OnJumpPerformed;
        _moveAction.Dispose();
        _jumpAction.Dispose();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        // 記下按跳:在緩衝時間內落地就會自動跳 (jump buffer)
        _jumpBufferTimer = jumpBufferTime;
    }

    private void Update()
    {
        _moveInput = _moveAction.ReadValue<float>();

        // 緩衝計時放在 Update 以反映真實按鍵時間
        if (_jumpBufferTimer > 0f)
            _jumpBufferTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 記錄進入這個物理幀時的速度:落地瞬間即為下墜速度,用來決定落地回饋強度
        float incomingVelY = _rb.linearVelocity.y;

        // 地面偵測 (記錄踩到的 collider)
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        _groundCollider = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);
        bool wasGrounded = _isGrounded;
        _isGrounded = _groundCollider != null;

        // 落地音效
        if (!wasGrounded && _isGrounded)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("Land");

            // 落地回饋:壓扁 + 塵土 + 依下墜速度的螢幕震動 (純視覺,不影響碰撞)
            if (enableJuice)
            {
                _fallSpeed = Mathf.Max(0f, -incomingVelY);
                float power = Mathf.Clamp01(_fallSpeed / Mathf.Max(0.01f, hardLandSpeed));

                JuiceFX.Squash(_visual, _visualBaseScale, landSquash.x, landSquash.y, 0.22f);
                JuiceFX.Dust((Vector2)transform.position + groundCheckOffset,
                    count: 6 + Mathf.RoundToInt(power * 10f),
                    strength: 0.8f + power * 0.7f);

                if (power > 0.15f)
                    JuiceFX.Shake(0.08f + power * 0.16f, 0.12f + power * 0.10f);
            }
        }

        // Coyote time:在地面時補滿,離地後逐漸遞減
        if (_isGrounded)
            _coyoteTimer = coyoteTime;
        else if (_coyoteTimer > 0f)
            _coyoteTimer -= dt;

        // 速度加速:接觸加速屍體期間維持滿倍率,離開後遞減回 1(僅水平方向)
        if (_hasPendingBoost)
        {
            _speedMul = _pendingBoost;
            _hasPendingBoost = false;
        }
        else
        {
            _speedMul = Mathf.MoveTowards(_speedMul, 1f, _boostDecay * dt);
        }

        Vector2 velocity = _rb.linearVelocity;

        // 水平移動:用加減速逼近目標速度,避免瞬間切速度的生硬/打滑感
        float targetSpeed = _moveInput * moveSpeed * _speedMul;
        bool accelerating = !Mathf.Approximately(_moveInput, 0f);
        float rate = accelerating ? acceleration : deceleration;
        if (!_isGrounded) rate *= airControl;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * dt);

        // 跳躍:coyote time + jump buffer 兩者皆滿足才執行
        if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
        {
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySFX("Jump");

            // 彈跳屍體:踩在帶有 CorpseSkill_Bounce 的平台上,跳躍力倍增
            float force = jumpForce;
            bool bounced = false;
            if (_groundCollider != null)
            {
                var bounce = _groundCollider.GetComponent<CorpseSkill_Bounce>();
                if (bounce != null) { force *= bounce.jumpMultiplier; bounced = true; }
            }

            velocity.y = force;

            // 起跳回饋:縱向拉伸 + 腳底塵土 + 輕微震動 (彈跳屍體給更強的回饋)
            if (enableJuice)
            {
                float stretchBoost = bounced ? 1.15f : 1f;
                JuiceFX.Squash(_visual, _visualBaseScale,
                    jumpSquash.x, jumpSquash.y * stretchBoost, 0.2f);
                JuiceFX.Dust((Vector2)transform.position + groundCheckOffset,
                    count: bounced ? 12 : 7,
                    strength: bounced ? 1.3f : 0.9f);
                if (bounced)
                    JuiceFX.Shake(0.18f, 0.14f);
            }
        }

        _rb.linearVelocity = velocity;

        // 變動重力:下降加重、上升中放開跳躍鍵也加重 (做出短跳),其餘維持基礎重力
        bool jumpHeld = _jumpAction.IsPressed();
        if (_rb.linearVelocity.y < 0f)
            _rb.gravityScale = _baseGravityScale * fallGravityMultiplier;
        else if (_rb.linearVelocity.y > 0f && !jumpHeld)
            _rb.gravityScale = _baseGravityScale * lowJumpMultiplier;
        else
            _rb.gravityScale = _baseGravityScale;

        // 動畫狀態
        UpdateAnimation();
    }

    /// <summary>
    /// 依目前移動/跳躍狀態,直接用 Animator.Play 切換到對應動畫。
    /// 只在狀態改變時呼叫 Play,避免每幀重播動畫。
    /// </summary>
    private void UpdateAnimation()
    {
        // 死亡中:鎖定在死亡動畫,不被移動狀態覆蓋
        if (_isDead)
            return;

        Vector2 v = _rb.linearVelocity;

        // 依方向翻轉圖片 (有輸入時才更新朝向)
        if (flipSpriteByDirection && _spriteRenderer != null && Mathf.Abs(_moveInput) > 0.01f)
            _spriteRenderer.flipX = _moveInput < 0f;

        // 決定狀態名稱:空中 (不論上升或下降) 都用跳躍動畫
        string state;
        if (!_isGrounded)
            state = jumpState;
        else
            state = Mathf.Abs(v.x) > moveAnimThreshold ? moveState : idleState;

        PlayState(state);
    }

    /// <summary>切換到指定動畫狀態 (只在改變時 Play,避免每幀重播)。</summary>
    private void PlayState(string state)
    {
        if (animator == null || string.IsNullOrEmpty(state))
            return;

        int hash = Animator.StringToHash(state);
        if (hash == _currentStateHash)
            return;

        _currentStateHash = hash;
        animator.Play(hash, 0, 0f);
    }

    /// <summary>
    /// 播放死亡 (變屍體) 動畫並鎖定動畫狀態。
    /// 由死亡流程呼叫 (例如 LoopManager 留下殘影、踏入 Hazard 失敗時)。
    /// 下次重生時會在 OnEnable 自動解除鎖定。
    /// </summary>
    /// <returns>死亡動畫長度 (秒),呼叫端可用來等待動畫播放完畢。找不到則回傳 0。</returns>
    public float PlayDeath()
    {
        _isDead = true;
        PlayState(fallState);
        var clip = GetClip(fallState);
        return clip != null ? clip.length : 0f;
    }

    /// <summary>目前角色圖片的左右翻轉狀態 (朝向),供屍體沿用。</summary>
    public bool SpriteFlipX => _spriteRenderer != null && _spriteRenderer.flipX;

    /// <summary>顯示 / 隱藏角色圖片 (死亡後隱藏主角,只留屍體在畫面上)。</summary>
    public void SetRendererVisible(bool visible)
    {
        if (_spriteRenderer != null)
            _spriteRenderer.enabled = visible;
    }

    /// <summary>
    /// 取樣死亡動畫的最後一幀,回傳該幀的 Sprite (供屍體當作圖片使用)。
    /// 直接取樣 clip 結尾,不受 Animator 是否 loop 或更新時序影響。
    /// </summary>
    public Sprite GetDeathLastFrameSprite()
    {
        var clip = GetClip(fallState);
        if (clip != null && _spriteRenderer != null)
            clip.SampleAnimation(_spriteRenderer.gameObject, clip.length);
        return _spriteRenderer != null ? _spriteRenderer.sprite : null;
    }

    /// <summary>依名稱在目前 Animator Controller 中尋找動畫片段。</summary>
    private AnimationClip GetClip(string clipName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            return null;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
            if (clip != null && clip.name == clipName)
                return clip;

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
