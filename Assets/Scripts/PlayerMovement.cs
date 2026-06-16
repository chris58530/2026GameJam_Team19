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

        // Enable gravity, lock Z-axis movement and X/Y rotation (2D style)
        rb.useGravity = true;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        // Ground check
        CheckGrounded();

        // Horizontal movement
        float moveX = Input.GetAxis("Horizontal");
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveX * moveSpeed;
        rb.linearVelocity = velocity;

        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
        }
    }

    void CheckGrounded()
    {
        // Cast a ray from the bottom of the capsule to detect the ground
        float radius = capsuleCollider.radius;
        Vector3 origin = transform.position + Vector3.down * (capsuleCollider.height / 2f - radius);

        isGrounded = Physics.SphereCast(origin, radius * 0.9f, Vector3.down, out RaycastHit hit, groundCheckDistance);
    }
}
