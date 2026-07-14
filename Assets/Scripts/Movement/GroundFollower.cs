using UnityEngine;

/// <summary>
/// Keeps a 3D character root aligned with uneven terrain by raycasting downward.
/// Put this on the movement/collider root; keep 2D sprites as visual children with their own local height offset.
/// </summary>
public sealed class GroundFollower : MonoBehaviour
{
    [Header("Ground Probe")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private Transform groundProbe;
    [SerializeField] private float raycastHeight = 3f;
    [SerializeField] private float raycastDistance = 8f;
    [SerializeField] private float groundOffset;
    [SerializeField] private bool useFootAnchors;
    [SerializeField] private Transform[] footAnchors;
    [SerializeField] private bool useRendererBoundsForFooting;
    [SerializeField] private Renderer[] footingRenderers;
    [SerializeField] private float footGroundPadding = 0.02f;
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
        ResolveGroundProbe();
        lastValidPosition = transform.position;
    }

    private void OnValidate()
    {
        ResolveGroundProbe();
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
        Vector3 probePosition = GetProbePosition();
        Vector3 origin = probePosition + Vector3.up * raycastHeight;
        float distance = raycastHeight + raycastDistance;

        if (drawDebugRay)
        {
            Debug.DrawRay(origin, Vector3.down * distance, Color.green);
        }

        if (!TryRaycastGround(origin, distance, out RaycastHit hit))
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
        position.y += hit.point.y + GetEffectiveGroundOffset() - GetLowestGroundReferenceY();

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

    private float GetEffectiveGroundOffset()
    {
        if (groundProbe != null)
        {
            return groundOffset;
        }

        return groundOffset + (useFootAnchors ? footGroundPadding : 0f);
    }

    private void ResolveGroundProbe()
    {
        if (groundProbe != null)
        {
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null && children[i].name == "Ground")
            {
                groundProbe = children[i];
                return;
            }
        }
    }

    private bool TryRaycastGround(Vector3 origin, float distance, out RaycastHit groundHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, groundMask, QueryTriggerInteraction.Ignore);
        groundHit = default;

        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                groundHit = hit;
            }
        }

        return !float.IsPositiveInfinity(closestDistance);
    }

    private Vector3 GetProbePosition()
    {
        return groundProbe != null ? groundProbe.position : transform.position;
    }

    private float GetLowestGroundReferenceY()
    {
        if (groundProbe != null)
        {
            return groundProbe.position.y;
        }

        return GetLowestFootY();
    }

    private float GetLowestFootY()
    {
        if (!useFootAnchors && !useRendererBoundsForFooting)
        {
            return transform.position.y;
        }

        float lowestY = float.PositiveInfinity;
        if (footAnchors != null)
        {
            for (int i = 0; i < footAnchors.Length; i++)
            {
                Transform foot = footAnchors[i];
                if (foot != null)
                {
                    lowestY = Mathf.Min(lowestY, foot.position.y);
                }
            }
        }

        if (useRendererBoundsForFooting && footingRenderers != null)
        {
            for (int i = 0; i < footingRenderers.Length; i++)
            {
                Renderer footingRenderer = footingRenderers[i];
                if (footingRenderer != null && footingRenderer.enabled)
                {
                    lowestY = Mathf.Min(lowestY, footingRenderer.bounds.min.y);
                }
            }
        }

        return float.IsPositiveInfinity(lowestY) ? transform.position.y : lowestY;
    }
}
