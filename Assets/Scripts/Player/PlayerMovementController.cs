using UnityEngine;

/// <summary>
/// Simple 3D player controller for the prototype cube player.
/// Uses WASD on the XZ plane and Space for a Rigidbody jump.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float fallbackMoveSpeedMetersPerSecond = 2f;
    [SerializeField] private CharacterRuntimeStats runtimeStats;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckDistance = 0.9f;
    [SerializeField] private float groundCheckRadius = 0.28f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private LayerMask groundLayers = ~0;

    private Rigidbody rb;
    private Vector3 pendingMove;
    private bool jumpRequested;
    private float lastGroundedTime;

    private float MoveSpeed => runtimeStats != null ? runtimeStats.MoveSpeed : fallbackMoveSpeedMetersPerSecond;

private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<CharacterRuntimeStats>();
        }

        // Keep the cube from rotating while physics handles gravity and jumping.
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        pendingMove = new Vector3(horizontal, 0f, vertical).normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpRequested = true;
        }
    }

private void FixedUpdate()
    {
        Vector3 velocity = rb.velocity;
        Vector3 targetHorizontalVelocity = pendingMove * MoveSpeed;
        rb.velocity = new Vector3(targetHorizontalVelocity.x, velocity.y, targetHorizontalVelocity.z);

        if (IsGrounded())
        {
            lastGroundedTime = Time.time;
        }

        if (jumpRequested && Time.time - lastGroundedTime <= coyoteTime)
        {
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastGroundedTime = float.NegativeInfinity;
        }

        rb.MoveRotation(Quaternion.identity);
        jumpRequested = false;
    }

private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        return Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore);
    }
}
