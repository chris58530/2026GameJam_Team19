using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // 啟用重力，鎖定 Z 軸移動和 X/Y 旋轉（2D 風格）
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // 地面偵測
        CheckGrounded();

        // 水平移動
        float moveX = Input.GetAxis("Horizontal");
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveX * moveSpeed;
        rb.linearVelocity = velocity;

        // 跳躍
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
        }
    }

    void CheckGrounded()
    {
        // 從膠囊體底部發射射線檢測地面
        float radius = capsuleCollider.radius;
        Vector3 origin = transform.position + Vector3.down * (capsuleCollider.height / 2f - radius);

        isGrounded = Physics.SphereCast(origin, radius * 0.9f, Vector3.down, out RaycastHit hit, groundCheckDistance);
    }
}
