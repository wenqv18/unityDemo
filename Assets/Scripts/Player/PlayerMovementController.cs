using UnityEngine;

/// <summary>
/// Simple 3D player controller for the prototype cube player.
/// Uses WASD on the XZ plane and Space for a Rigidbody jump.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerMovementController : MonoBehaviour
{
    private const string PlayerLayerName = "Player";

    [SerializeField] private float fallbackMoveSpeedMetersPerSecond = 2f;
    [SerializeField] private CharacterRuntimeStats runtimeStats;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckDistance = 0.9f;
    [SerializeField] private float groundCheckRadius = 0.28f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [Header("Seam Smoothing")]
    [SerializeField] private bool replaceBoxColliderWithCapsule = true;
    [SerializeField] private float capsuleRadiusScale = 0.45f;
    [SerializeField] private float minimumCapsuleRadius = 0.25f;
    [SerializeField] private float minimumCapsuleHeight = 1.2f;

    private Rigidbody rb;
    private PhysicMaterial smoothMovementMaterial;
    private Vector3 pendingMove;
    private bool jumpRequested;
    private float lastGroundedTime;

    private float MoveSpeed => runtimeStats != null ? runtimeStats.MoveSpeed : fallbackMoveSpeedMetersPerSecond;

    private void Awake()
    {
        ApplyPlayerLayer();
        rb = GetComponent<Rigidbody>();
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<CharacterRuntimeStats>();
        }

        // Keep the cube from rotating while physics handles gravity and jumping.
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        ConfigureSmoothMovementCollider();
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

    private void ConfigureSmoothMovementCollider()
    {
        smoothMovementMaterial = new PhysicMaterial("Player Smooth Movement")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine = PhysicMaterialCombine.Minimum
        };

        if (!replaceBoxColliderWithCapsule)
        {
            ApplySmoothMaterialToColliders();
            return;
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            ApplySmoothMaterialToColliders();
            return;
        }

        CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        }

        Vector3 boxSize = boxCollider.size;
        float horizontalSize = Mathf.Min(Mathf.Abs(boxSize.x), Mathf.Abs(boxSize.z));
        float boxHeight = Mathf.Abs(boxSize.y);
        float capsuleHeight = Mathf.Max(minimumCapsuleHeight, boxHeight);
        Vector3 capsuleCenter = boxCollider.center;
        capsuleCenter.y += Mathf.Max(0f, capsuleHeight - boxHeight) * 0.5f;

        capsuleCollider.center = capsuleCenter;
        capsuleCollider.radius = Mathf.Max(minimumCapsuleRadius, horizontalSize * capsuleRadiusScale);
        capsuleCollider.height = capsuleHeight;
        capsuleCollider.direction = 1;
        capsuleCollider.isTrigger = boxCollider.isTrigger;
        capsuleCollider.sharedMaterial = smoothMovementMaterial;

        boxCollider.enabled = false;
    }

    private void ApplySmoothMaterialToColliders()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && !colliders[i].isTrigger)
            {
                colliders[i].sharedMaterial = smoothMovementMaterial;
            }
        }
    }

    private void ApplyPlayerLayer()
    {
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        if (playerLayer < 0)
        {
            Debug.LogWarning("Player layer is missing. Movement will not treat Player as an obstacle.");
            return;
        }

        SetLayerRecursively(transform, playerLayer);
    }

    private void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
        {
            SetLayerRecursively(root.GetChild(i), layer);
        }
    }
}
