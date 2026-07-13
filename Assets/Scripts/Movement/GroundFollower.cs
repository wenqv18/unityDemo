using UnityEngine;

/// <summary>
/// Keeps a 3D character root aligned with uneven terrain by raycasting downward.
/// Put this on the movement/collider root; keep 2D sprites as visual children with their own local height offset.
/// </summary>
public sealed class GroundFollower : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float raycastHeight = 3f;
    [SerializeField] private float raycastDistance = 8f;
    [SerializeField] private float groundOffset;
    [SerializeField] private float maxSlopeAngle = 50f;
    [SerializeField] private bool snapToGround = true;
    [SerializeField] private bool blockTooSteepSlope = true;
    [SerializeField] private bool drawDebugRay;

    private Vector3 lastValidPosition;
    private bool hasValidGround;

    public bool IsGrounded { get; private set; }
    public bool IsOnTooSteepSlope { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;
    public float GroundSlopeAngle { get; private set; }

    private void Awake()
    {
        lastValidPosition = transform.position;
    }

    private void LateUpdate()
    {
        FollowGround();
    }

    /// <summary>
    /// Runs the ground check immediately. Call this after manual teleports or spawn placement.
    /// </summary>
    public bool FollowGround()
    {
        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        float distance = raycastHeight + raycastDistance;

        if (drawDebugRay)
        {
            Debug.DrawRay(origin, Vector3.down * distance, Color.green);
        }

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            IsGrounded = false;
            IsOnTooSteepSlope = false;
            return false;
        }

        IsGrounded = true;
        GroundNormal = hit.normal;
        GroundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        IsOnTooSteepSlope = GroundSlopeAngle > maxSlopeAngle;

        if (IsOnTooSteepSlope && blockTooSteepSlope && hasValidGround)
        {
            transform.position = lastValidPosition;
            return false;
        }

        Vector3 position = transform.position;
        position.y = hit.point.y + groundOffset;

        if (snapToGround)
        {
            transform.position = position;
        }

        lastValidPosition = transform.position;
        hasValidGround = true;
        return true;
    }

    /// <summary>
    /// Changes the desired vertical offset above the detected ground at runtime.
    /// </summary>
    public void SetGroundOffset(float offset)
    {
        groundOffset = offset;
    }
}
