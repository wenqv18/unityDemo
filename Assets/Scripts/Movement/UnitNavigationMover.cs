using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared combat movement wrapper. It uses cheap line checks for direct movement and
/// falls back to a small A* grid when a wall blocks the target.
/// </summary>
public sealed class UnitNavigationMover : MonoBehaviour
{
    [SerializeField] private float repathInterval = 0.2f;
    [SerializeField] private float gridStep = 1.2f;
    [SerializeField] private float searchRadius = 18f;
    [SerializeField] private int maxExpandedNodes = 700;
    [SerializeField] private float cornerReachDistance = 0.25f;
    [SerializeField] private float lineOfSightHeight = 0.9f;
    [SerializeField] private float movementClearanceRadius = 0.75f;
    [SerializeField] private float pathCommitDuration = 0.6f;
    [SerializeField] private float clearLineResumeDelay = 0.25f;

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private readonly List<GridKey> openList = new List<GridKey>();
    private readonly Dictionary<GridKey, PathNode> nodes = new Dictionary<GridKey, PathNode>();
    private readonly HashSet<GridKey> closedSet = new HashSet<GridKey>();

    private Vector3 lastTargetPosition;
    private float nextRepathTime;
    private float committedPathUntilTime;
    private float clearLineSinceTime = -1f;
    private int pathIndex;

    public bool MoveTowards(Vector3 targetPosition, float speed, float stoppingDistance = 0f)
    {
        if (speed <= 0f)
        {
            return false;
        }

        Vector3 flatDelta = targetPosition - transform.position;
        flatDelta.y = 0f;
        if (flatDelta.magnitude <= Mathf.Max(0f, stoppingDistance))
        {
            FaceFlatDirection(flatDelta);
            return true;
        }

        bool hasPath = currentPath.Count > 0 && pathIndex < currentPath.Count;
        bool committedToPath = hasPath && Time.time < committedPathUntilTime;
        bool clearLineReady = HasStableClearLineTo(targetPosition);
        if (!committedToPath && clearLineReady)
        {
            ClearPath();
            MoveDirectly(targetPosition, speed);
            return true;
        }

        if (ShouldRepath(targetPosition))
        {
            BuildPath(targetPosition);
        }

        return MoveAlongPath(speed);
    }

    public void Stop()
    {
        ClearPath();
    }

    public void SetMovementClearanceRadius(float radius)
    {
        movementClearanceRadius = Mathf.Max(0f, radius);
    }

    public bool HasClearLineTo(Transform target)
    {
        return target != null && HasClearLineTo(target.position, target);
    }

    public bool HasClearLineTo(Vector3 targetPosition, Transform ignoredTarget = null)
    {
        return !IsBlockedSegment(transform.position, targetPosition, ignoredTarget);
    }

    private bool ShouldRepath(Vector3 targetPosition)
    {
        Vector3 flatDelta = targetPosition - lastTargetPosition;
        flatDelta.y = 0f;
        return currentPath.Count == 0 || Time.time >= nextRepathTime || flatDelta.sqrMagnitude > 0.5f * 0.5f;
    }

    private void BuildPath(Vector3 targetPosition)
    {
        ClearPath();
        nextRepathTime = Time.time + repathInterval;
        lastTargetPosition = targetPosition;

        Vector3 start = Flatten(transform.position);
        Vector3 target = Flatten(targetPosition);
        GridKey startKey = GridKey.Zero;
        GridKey bestKey = startKey;
        float bestDistance = FlatDistance(start, target);

        nodes.Clear();
        openList.Clear();
        closedSet.Clear();

        nodes[startKey] = new PathNode(startKey, startKey, 0f, Heuristic(start, target));
        openList.Add(startKey);

        int expandedNodes = 0;
        while (openList.Count > 0 && expandedNodes < maxExpandedNodes)
        {
            GridKey currentKey = PopBestOpenNode();
            PathNode currentNode = nodes[currentKey];
            closedSet.Add(currentKey);
            expandedNodes++;

            Vector3 currentWorld = KeyToWorld(start, currentKey);
            float currentTargetDistance = FlatDistance(currentWorld, target);
            if (currentTargetDistance < bestDistance)
            {
                bestDistance = currentTargetDistance;
                bestKey = currentKey;
            }

            if (currentTargetDistance <= gridStep * 1.25f && !IsBlockedSegment(currentWorld, target))
            {
                ReconstructPath(start, currentKey, target);
                return;
            }

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0)
                    {
                        continue;
                    }

                    GridKey neighborKey = new GridKey(currentKey.X + x, currentKey.Z + z);
                    if (closedSet.Contains(neighborKey) || IsOutsideSearchRadius(neighborKey))
                    {
                        continue;
                    }

                    Vector3 neighborWorld = KeyToWorld(start, neighborKey);
                    if (IsBlockedSegment(currentWorld, neighborWorld))
                    {
                        continue;
                    }

                    float stepCost = x != 0 && z != 0 ? 1.4142f * gridStep : gridStep;
                    float newCost = currentNode.CostFromStart + stepCost;
                    PathNode oldNode;
                    if (nodes.TryGetValue(neighborKey, out oldNode) && newCost >= oldNode.CostFromStart)
                    {
                        continue;
                    }

                    nodes[neighborKey] = new PathNode(neighborKey, currentKey, newCost, Heuristic(neighborWorld, target));
                    if (!openList.Contains(neighborKey))
                    {
                        openList.Add(neighborKey);
                    }
                }
            }
        }

        if (!bestKey.Equals(startKey))
        {
            ReconstructPath(start, bestKey, KeyToWorld(start, bestKey));
        }
    }

    private bool HasStableClearLineTo(Vector3 targetPosition)
    {
        if (!HasClearLineTo(targetPosition))
        {
            clearLineSinceTime = -1f;
            return false;
        }

        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            clearLineSinceTime = Time.time;
            return true;
        }

        if (clearLineSinceTime < 0f)
        {
            clearLineSinceTime = Time.time;
            return false;
        }

        return Time.time - clearLineSinceTime >= clearLineResumeDelay;
    }

    private bool MoveAlongPath(float speed)
    {
        if (currentPath.Count == 0 || pathIndex >= currentPath.Count)
        {
            return false;
        }

        Vector3 waypoint = currentPath[pathIndex];
        Vector3 flatDelta = waypoint - transform.position;
        flatDelta.y = 0f;
        while (flatDelta.sqrMagnitude <= cornerReachDistance * cornerReachDistance && pathIndex < currentPath.Count - 1)
        {
            pathIndex++;
            waypoint = currentPath[pathIndex];
            flatDelta = waypoint - transform.position;
            flatDelta.y = 0f;
        }

        if (flatDelta.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        MoveDirectly(waypoint, speed);
        return true;
    }

    private void ReconstructPath(Vector3 start, GridKey endKey, Vector3 finalPoint)
    {
        List<Vector3> reversed = new List<Vector3>();
        GridKey currentKey = endKey;
        int guard = 0;
        while (nodes.ContainsKey(currentKey) && guard < maxExpandedNodes)
        {
            reversed.Add(KeyToWorld(start, currentKey));
            PathNode node = nodes[currentKey];
            if (node.Parent.Equals(currentKey))
            {
                break;
            }

            currentKey = node.Parent;
            guard++;
        }

        reversed.Reverse();
        currentPath.Clear();
        for (int i = 1; i < reversed.Count; i++)
        {
            currentPath.Add(reversed[i]);
        }

        currentPath.Add(finalPoint);
        pathIndex = 0;
        clearLineSinceTime = -1f;
        committedPathUntilTime = Time.time + Mathf.Max(0f, pathCommitDuration);
    }

    private GridKey PopBestOpenNode()
    {
        int bestIndex = 0;
        float bestScore = nodes[openList[0]].EstimatedTotalCost;
        for (int i = 1; i < openList.Count; i++)
        {
            float score = nodes[openList[i]].EstimatedTotalCost;
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        GridKey best = openList[bestIndex];
        openList.RemoveAt(bestIndex);
        return best;
    }

    private bool IsBlockedSegment(Vector3 from, Vector3 to, Transform ignoredTarget = null)
    {
        return CombatSpatialQuery.HasMovementObstacleBetween(from, to, lineOfSightHeight, movementClearanceRadius, transform, ignoredTarget);
    }

    private bool IsOutsideSearchRadius(GridKey key)
    {
        Vector2 offset = new Vector2(key.X * gridStep, key.Z * gridStep);
        return offset.sqrMagnitude > searchRadius * searchRadius;
    }

    private Vector3 KeyToWorld(Vector3 start, GridKey key)
    {
        return start + new Vector3(key.X * gridStep, 0f, key.Z * gridStep);
    }

    private float Heuristic(Vector3 position, Vector3 target)
    {
        return FlatDistance(position, target);
    }

    private float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 delta = a - b;
        delta.y = 0f;
        return delta.magnitude;
    }

    private Vector3 Flatten(Vector3 position)
    {
        return new Vector3(position.x, transform.position.y, position.z);
    }

    private void MoveDirectly(Vector3 targetPosition, float speed)
    {
        Vector3 currentPosition = transform.position;
        Vector3 target = new Vector3(targetPosition.x, currentPosition.y, targetPosition.z);
        Vector3 nextPosition = Vector3.MoveTowards(currentPosition, target, speed * Time.deltaTime);
        Vector3 movement = nextPosition - currentPosition;
        transform.position = nextPosition;
        FaceFlatDirection(movement);
    }

    private void FaceFlatDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private void ClearPath()
    {
        currentPath.Clear();
        pathIndex = 0;
        committedPathUntilTime = 0f;
    }

    private struct GridKey : IEquatable<GridKey>
    {
        public static readonly GridKey Zero = new GridKey(0, 0);

        public readonly int X;
        public readonly int Z;

        public GridKey(int x, int z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(GridKey other)
        {
            return X == other.X && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is GridKey && Equals((GridKey)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Z;
            }
        }
    }

    private struct PathNode
    {
        public readonly GridKey Key;
        public readonly GridKey Parent;
        public readonly float CostFromStart;
        public readonly float EstimatedTotalCost;

        public PathNode(GridKey key, GridKey parent, float costFromStart, float heuristic)
        {
            Key = key;
            Parent = parent;
            CostFromStart = costFromStart;
            EstimatedTotalCost = costFromStart + heuristic;
        }
    }
}
