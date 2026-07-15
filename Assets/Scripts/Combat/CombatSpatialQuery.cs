using UnityEngine;

/// <summary>
/// Centralized spatial rules for combat units: valid ground, wall visibility, and unit occupancy.
/// Keep summon placement, target detection, and movement checks using the same layer policy.
/// </summary>
public static class CombatSpatialQuery
{
    private const string GroundLayerName = "Ground";
    private const string WallLayerName = "Wall";
    private const string PlayerLayerName = "Player";
    private const float DefaultWallCheckHeight = 0.9f;
    private const float MovementTargetIgnoreDistance = 0.75f;

    public static int GroundMask => GetLayerMask(GroundLayerName, ~0);
    public static int WallMask => GetLayerMask(WallLayerName, 0);
    public static int PlayerMask => GetLayerMask(PlayerLayerName, 0);
    public static int MovementObstacleMask => WallMask | PlayerMask;

    public static bool IsGroundLayer(int layer)
    {
        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        return groundLayer >= 0 && layer == groundLayer;
    }

    public static bool IsWallLayer(int layer)
    {
        int wallLayer = LayerMask.NameToLayer(WallLayerName);
        return wallLayer >= 0 && layer == wallLayer;
    }

    public static bool IsPlayerLayer(int layer)
    {
        int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        return playerLayer >= 0 && layer == playerLayer;
    }

    public static bool TryFindGroundPoint(Vector3 candidate, float raycastHeight, float raycastDistance, out Vector3 groundPoint)
    {
        groundPoint = candidate;
        Vector3 origin = candidate + Vector3.up * Mathf.Max(0f, raycastHeight);
        float distance = Mathf.Max(0.1f, raycastHeight + raycastDistance);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, GroundMask, QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        bool foundGround = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || !IsGroundLayer(hit.collider.gameObject.layer))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                groundPoint = hit.point;
                foundGround = true;
            }
        }

        return foundGround;
    }

    public static bool HasWallBetween(Vector3 from, Vector3 to)
    {
        return HasWallBetween(from, to, DefaultWallCheckHeight, null, null);
    }

    public static bool HasWallBetween(Vector3 from, Vector3 to, float height, Transform ignoredFrom, Transform ignoredTo)
    {
        return HasObstacleBetween(from, to, height, ignoredFrom, ignoredTo, WallMask, false);
    }

    public static bool HasMovementObstacleBetween(Vector3 from, Vector3 to, float height, Transform ignoredFrom, Transform ignoredTo)
    {
        return HasMovementObstacleBetween(from, to, height, 0f, ignoredFrom, ignoredTo);
    }

    public static bool HasMovementObstacleBetween(Vector3 from, Vector3 to, float height, float clearanceRadius, Transform ignoredFrom, Transform ignoredTo)
    {
        if (clearanceRadius <= 0f)
        {
            return HasObstacleBetween(from, to, height, ignoredFrom, ignoredTo, MovementObstacleMask, true);
        }

        return HasObstacleVolumeBetween(from, to, height, clearanceRadius, ignoredFrom, ignoredTo, MovementObstacleMask, true);
    }

    private static bool HasObstacleBetween(
        Vector3 from,
        Vector3 to,
        float height,
        Transform ignoredFrom,
        Transform ignoredTo,
        int layerMask,
        bool ignorePlayerAtDestination)
    {
        if (layerMask == 0)
        {
            return false;
        }

        Vector3 origin = new Vector3(from.x, from.y + height, from.z);
        Vector3 destination = new Vector3(to.x, to.y + height, to.z);
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance <= 0.0001f)
        {
            return false;
        }

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, layerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null
                || (ignoredFrom != null && hitCollider.transform.IsChildOf(ignoredFrom))
                || (ignoredTo != null && hitCollider.transform.IsChildOf(ignoredTo)))
            {
                continue;
            }

            int hitLayer = hitCollider.gameObject.layer;
            if (IsWallLayer(hitLayer))
            {
                return true;
            }

            if (IsPlayerLayer(hitLayer))
            {
                if (ignorePlayerAtDestination && Vector3.Distance(hits[i].point, destination) <= MovementTargetIgnoreDistance)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    private static bool HasObstacleVolumeBetween(
        Vector3 from,
        Vector3 to,
        float height,
        float clearanceRadius,
        Transform ignoredFrom,
        Transform ignoredTo,
        int layerMask,
        bool ignorePlayerAtDestination)
    {
        if (layerMask == 0)
        {
            return false;
        }

        Vector3 origin = new Vector3(from.x, from.y + height, from.z);
        Vector3 destination = new Vector3(to.x, to.y + height, to.z);
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance <= 0.0001f)
        {
            return HasBlockingOverlap(origin, clearanceRadius, ignoredFrom, ignoredTo, ignorePlayerAtDestination, destination, layerMask);
        }

        float safeRadius = Mathf.Max(0.01f, clearanceRadius);
        if (HasBlockingOverlap(origin, safeRadius, ignoredFrom, ignoredTo, ignorePlayerAtDestination, destination, layerMask))
        {
            return true;
        }

        RaycastHit[] hits = Physics.SphereCastAll(origin, safeRadius, direction.normalized, distance, layerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsBlockingMovementHit(hits[i].collider, hits[i].point, ignoredFrom, ignoredTo, ignorePlayerAtDestination, destination))
            {
                return true;
            }
        }

        return HasBlockingOverlap(destination, safeRadius, ignoredFrom, ignoredTo, ignorePlayerAtDestination, destination, layerMask);
    }

    private static bool HasBlockingOverlap(
        Vector3 center,
        float radius,
        Transform ignoredFrom,
        Transform ignoredTo,
        bool ignorePlayerAtDestination,
        Vector3 destination,
        int layerMask)
    {
        Collider[] overlaps = Physics.OverlapSphere(center, radius, layerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            if (IsBlockingMovementHit(overlaps[i], center, ignoredFrom, ignoredTo, ignorePlayerAtDestination, destination))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlockingMovementHit(
        Collider hitCollider,
        Vector3 hitPoint,
        Transform ignoredFrom,
        Transform ignoredTo,
        bool ignorePlayerAtDestination,
        Vector3 destination)
    {
        if (hitCollider == null
            || (ignoredFrom != null && hitCollider.transform.IsChildOf(ignoredFrom))
            || (ignoredTo != null && hitCollider.transform.IsChildOf(ignoredTo)))
        {
            return false;
        }

        int hitLayer = hitCollider.gameObject.layer;
        if (IsWallLayer(hitLayer))
        {
            return true;
        }

        if (IsPlayerLayer(hitLayer))
        {
            return !ignorePlayerAtDestination || Vector3.Distance(hitPoint, destination) > MovementTargetIgnoreDistance;
        }

        return false;
    }

    public static bool IsUnitAreaFree(Vector3 point, float radius, Transform ignoredRoot)
    {
        float safeRadius = Mathf.Max(0.05f, radius);
        Collider[] overlaps = Physics.OverlapSphere(point + Vector3.up * 0.5f, safeRadius, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null || (ignoredRoot != null && overlap.transform.IsChildOf(ignoredRoot)))
            {
                continue;
            }

            if (BelongsToCombatUnit(overlap.transform))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsValidSummonPoint(
        Transform owner,
        Vector3 candidate,
        float clearRadius,
        float raycastHeight,
        float raycastDistance,
        float wallCheckHeight,
        out Vector3 groundPoint)
    {
        groundPoint = candidate;
        if (owner == null || !TryFindGroundPoint(candidate, raycastHeight, raycastDistance, out groundPoint))
        {
            return false;
        }

        if (HasWallBetween(owner.position, groundPoint, wallCheckHeight, owner, null))
        {
            return false;
        }

        return IsUnitAreaFree(groundPoint, clearRadius, owner);
    }

    public static bool TryFindSummonPoint(
        Transform owner,
        float minDistance,
        float maxDistance,
        float stepDistance,
        int directionAttempts,
        float clearRadius,
        float raycastHeight,
        float raycastDistance,
        float wallCheckHeight,
        out Vector3 spawnPoint)
    {
        spawnPoint = owner != null ? owner.position : Vector3.zero;
        if (owner == null)
        {
            return false;
        }

        float safeMinDistance = Mathf.Max(0.1f, minDistance);
        float safeMaxDistance = Mathf.Max(safeMinDistance, maxDistance);
        float safeStepDistance = Mathf.Max(0.1f, stepDistance);
        int safeDirectionAttempts = Mathf.Max(1, directionAttempts);

        for (int i = 0; i < safeDirectionAttempts; i++)
        {
            Vector3 direction = GetRandomFlatDirection();
            float initialDistance = Random.Range(safeMinDistance, safeMaxDistance);
            if (TryFindPointOnLine(owner, direction, initialDistance, safeMinDistance, safeStepDistance, false, clearRadius, raycastHeight, raycastDistance, wallCheckHeight, out spawnPoint))
            {
                return true;
            }

            if (TryFindPointOnLine(owner, -direction, safeMinDistance, safeMaxDistance, safeStepDistance, true, clearRadius, raycastHeight, raycastDistance, wallCheckHeight, out spawnPoint))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindPointOnLine(
        Transform owner,
        Vector3 direction,
        float startDistance,
        float endDistance,
        float stepDistance,
        bool moveOutward,
        float clearRadius,
        float raycastHeight,
        float raycastDistance,
        float wallCheckHeight,
        out Vector3 spawnPoint)
    {
        spawnPoint = owner.position;
        Vector3 safeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.right;
        if (moveOutward)
        {
            for (float distance = startDistance; distance <= endDistance + 0.001f; distance += stepDistance)
            {
                Vector3 candidate = owner.position + safeDirection * distance;
                if (IsValidSummonPoint(owner, candidate, clearRadius, raycastHeight, raycastDistance, wallCheckHeight, out spawnPoint))
                {
                    return true;
                }
            }

            return false;
        }

        for (float distance = startDistance; distance >= endDistance - 0.001f; distance -= stepDistance)
        {
            Vector3 candidate = owner.position + safeDirection * distance;
            if (IsValidSummonPoint(owner, candidate, clearRadius, raycastHeight, raycastDistance, wallCheckHeight, out spawnPoint))
            {
                return true;
            }
        }

        return false;
    }

    private static Vector3 GetRandomFlatDirection()
    {
        Vector2 randomCircle = Random.insideUnitCircle;
        if (randomCircle.sqrMagnitude <= 0.01f)
        {
            randomCircle = Vector2.right;
        }

        return new Vector3(randomCircle.x, 0f, randomCircle.y).normalized;
    }

    private static bool BelongsToCombatUnit(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (current.GetComponent<CombatIdentity>() != null || current.GetComponent<CharacterRuntimeStats>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static int GetLayerMask(string layerName, int fallbackMask)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? 1 << layer : fallbackMask;
    }
}
