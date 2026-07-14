using System;
using UnityEngine;

/// <summary>
/// Shared non-combat summon movement: keeps distance from the player, patrols nearby,
/// and returns with run animation when too far away.
/// </summary>
public sealed class SummonFollowController : MonoBehaviour, ICharacterAnimationStateProvider
{
    [SerializeField] private CharacterData characterData;
    [SerializeField] private CharacterRuntimeStats runtimeStats;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float fallbackMoveSpeedMetersPerSecond = 2f;
    [SerializeField] private float minimumPlayerDistance = 2.3f;
    [SerializeField] private float patrolMinDistance = 3f;
    [SerializeField] private float patrolMaxDistance = 8f;
    [SerializeField] private float idlePatrolSpeedMultiplier = 0.2f;
    [SerializeField] private float playerMoveDetectionThreshold = 0.05f;
    [SerializeField] private float patrolPointReachDistance = 0.6f;
    [SerializeField] private float patrolRepathInterval = 3f;
    [SerializeField] private float returnToPlayerDistance = 8f;
    [SerializeField] private float returnCompleteDistance = 4.5f;
    [SerializeField] private float returnMoveSpeedStatBonus = 2f;
    [SerializeField] private float initialSpawnMoveLockDuration = 1f;

    private Vector3 patrolPoint;
    private Vector3 patrolOffset;
    private float nextPatrolPickTime;
    private float spawnMoveLockUntilTime;
    private Vector3 previousFollowTargetPosition;
    private bool hasPreviousFollowTargetPosition;
    private bool isReturningToPlayer;
    private bool externalMovementLocked;
    private Action releasedCallback;
    private CharacterActionController actionController;

    private float MoveSpeed => runtimeStats != null ? runtimeStats.MoveSpeed : (characterData != null ? Mathf.Max(0f, characterData.MoveSpeed) * 0.2f : fallbackMoveSpeedMetersPerSecond);
    private float ReturnMoveSpeed => runtimeStats != null ? runtimeStats.GetMoveSpeedMetersPerSecond(returnMoveSpeedStatBonus) : MoveSpeed;

    public bool IsReturningToPlayer => isReturningToPlayer;
    public bool IsSpawnMoveLocked => Time.time < spawnMoveLockUntilTime;
    public string AnimationStateName => IsSpawnMoveLocked ? "SpawnLock" : (isReturningToPlayer ? "Return" : "Patrol");
    public bool HasActiveCombatTarget => false;
    public bool WantsRunAnimation => !IsSpawnMoveLocked && isReturningToPlayer;
    public bool WantsWalkAnimation => !IsSpawnMoveLocked && !externalMovementLocked;

    public void Initialize(Transform target, Action onReleased)
    {
        followTarget = target;
        releasedCallback = onReleased;
        StartSpawnMoveLock();
        CacheFollowTargetPosition();
        PickNewPatrolPoint();
    }

    public void SetExternalMovementLocked(bool locked)
    {
        externalMovementLocked = locked;
    }

    private void Awake()
    {
        StartSpawnMoveLock();
        actionController = GetComponent<CharacterActionController>();
        if (runtimeStats == null)
        {
            runtimeStats = GetComponent<CharacterRuntimeStats>();
        }

        ResolveFollowTargetIfNeeded();
        CacheFollowTargetPosition();
        PickNewPatrolPoint();
    }

    private void OnDestroy()
    {
        releasedCallback?.Invoke();
    }

    private void Update()
    {
        if (IsSpawnMoveLocked || externalMovementLocked || (actionController != null && !actionController.CanMove))
        {
            return;
        }

        PatrolAroundPlayer();
    }

    private void PatrolAroundPlayer()
    {
        ResolveFollowTargetIfNeeded();
        if (followTarget == null)
        {
            return;
        }

        Vector3 playerPosition = followTarget.position;
        Vector3 flatFromPlayer = transform.position - playerPosition;
        flatFromPlayer.y = 0f;
        float distanceFromPlayer = flatFromPlayer.magnitude;
        bool playerIsMoving = IsFollowTargetMoving(playerPosition);

        if (ShouldReturnToPlayer(distanceFromPlayer))
        {
            ReturnToPlayer(playerPosition, flatFromPlayer, distanceFromPlayer);
            return;
        }

        if (distanceFromPlayer < minimumPlayerDistance)
        {
            isReturningToPlayer = false;
            Vector3 safeDirection = GetSafeDirectionAwayFromPlayer(flatFromPlayer);
            patrolOffset = safeDirection * patrolMinDistance;
            patrolPoint = playerPosition + safeDirection * patrolMinDistance;
            nextPatrolPickTime = Time.time + patrolRepathInterval;
            MoveFlatTowards(patrolPoint, MoveSpeed);
            return;
        }

        patrolPoint = playerPosition + patrolOffset;
        float reachDistanceSqr = patrolPointReachDistance * patrolPointReachDistance;
        if ((patrolPoint - transform.position).sqrMagnitude <= reachDistanceSqr
            || distanceFromPlayer > patrolMaxDistance
            || Time.time >= nextPatrolPickTime
            || IsPatrolPointTooCloseToPlayer(patrolPoint, playerPosition))
        {
            PickNewPatrolPoint();
            patrolPoint = playerPosition + patrolOffset;
        }

        isReturningToPlayer = false;
        float patrolSpeed = playerIsMoving ? MoveSpeed : MoveSpeed * idlePatrolSpeedMultiplier;
        MoveFlatTowards(patrolPoint, patrolSpeed);
    }

    private bool ShouldReturnToPlayer(float distanceFromPlayer)
    {
        return isReturningToPlayer ? distanceFromPlayer > returnCompleteDistance : distanceFromPlayer > returnToPlayerDistance;
    }

    private void ReturnToPlayer(Vector3 playerPosition, Vector3 flatFromPlayer, float distanceFromPlayer)
    {
        isReturningToPlayer = true;
        float targetDistance = Mathf.Clamp(returnCompleteDistance, patrolMinDistance, patrolMaxDistance);
        Vector3 returnDirection = GetSafeDirectionAwayFromPlayer(flatFromPlayer);
        patrolOffset = returnDirection * targetDistance;
        patrolPoint = playerPosition + patrolOffset;
        nextPatrolPickTime = Time.time + patrolRepathInterval;
        MoveFlatTowards(patrolPoint, ReturnMoveSpeed);

        if (distanceFromPlayer <= returnCompleteDistance + patrolPointReachDistance)
        {
            isReturningToPlayer = false;
            PickNewPatrolPoint();
        }
    }

    private void PickNewPatrolPoint()
    {
        if (followTarget == null)
        {
            patrolOffset = Vector3.zero;
            patrolPoint = transform.position;
            nextPatrolPickTime = Time.time + patrolRepathInterval;
            return;
        }

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle;
        if (randomCircle.sqrMagnitude <= 0.01f)
        {
            randomCircle = Vector2.right;
        }

        float safePatrolMinDistance = Mathf.Max(minimumPlayerDistance, patrolMinDistance);
        float safePatrolMaxDistance = Mathf.Max(safePatrolMinDistance, patrolMaxDistance);
        float radius = UnityEngine.Random.Range(safePatrolMinDistance, safePatrolMaxDistance);
        randomCircle = randomCircle.normalized * radius;
        patrolOffset = new Vector3(randomCircle.x, 0f, randomCircle.y);
        patrolPoint = followTarget.position + patrolOffset;
        nextPatrolPickTime = Time.time + UnityEngine.Random.Range(patrolRepathInterval * 0.85f, patrolRepathInterval * 1.25f);
    }

    private bool IsPatrolPointTooCloseToPlayer(Vector3 point, Vector3 playerPosition)
    {
        Vector3 flatDelta = point - playerPosition;
        flatDelta.y = 0f;
        return flatDelta.sqrMagnitude < minimumPlayerDistance * minimumPlayerDistance;
    }

    private Vector3 GetSafeDirectionAwayFromPlayer(Vector3 flatFromPlayer)
    {
        if (flatFromPlayer.sqrMagnitude > 0.0001f)
        {
            return flatFromPlayer.normalized;
        }

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle;
        if (randomCircle.sqrMagnitude <= 0.01f)
        {
            randomCircle = Vector2.right;
        }

        return new Vector3(randomCircle.x, 0f, randomCircle.y).normalized;
    }

    private bool IsFollowTargetMoving(Vector3 currentFollowTargetPosition)
    {
        if (!hasPreviousFollowTargetPosition)
        {
            previousFollowTargetPosition = currentFollowTargetPosition;
            hasPreviousFollowTargetPosition = true;
            return false;
        }

        Vector3 flatDelta = currentFollowTargetPosition - previousFollowTargetPosition;
        flatDelta.y = 0f;
        previousFollowTargetPosition = currentFollowTargetPosition;
        return flatDelta.sqrMagnitude > playerMoveDetectionThreshold * playerMoveDetectionThreshold;
    }

    private void CacheFollowTargetPosition()
    {
        if (followTarget == null)
        {
            hasPreviousFollowTargetPosition = false;
            return;
        }

        previousFollowTargetPosition = followTarget.position;
        hasPreviousFollowTargetPosition = true;
    }

    private void StartSpawnMoveLock()
    {
        spawnMoveLockUntilTime = Time.time + Mathf.Max(0f, initialSpawnMoveLockDuration);
    }

    private void MoveFlatTowards(Vector3 targetPosition, float speed)
    {
        if (actionController != null && !actionController.CanMove)
        {
            return;
        }

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

    private void ResolveFollowTargetIfNeeded()
    {
        if (followTarget != null)
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            followTarget = player.transform;
        }
    }
}
