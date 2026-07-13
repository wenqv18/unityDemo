using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared combat state machine for summons and enemies. Attack type is provided by a CombatAttackBehaviour component.
/// High-priority action locks are owned by CharacterActionController.
/// </summary>
public sealed class CombatUnitAI : MonoBehaviour
{
    private enum CombatState
    {
        Idle,
        Patrol,
        Chase,
        HoldDistance,
        Attack
    }

    private enum CombatMovementMode
    {
        Melee,
        Ranged
    }

    private static readonly Dictionary<CombatFaction, CombatIdentity> SharedTargets = new Dictionary<CombatFaction, CombatIdentity>();

    [SerializeField] private CharacterRuntimeStats selfStats;
    [SerializeField] private CombatIdentity selfIdentity;
    [SerializeField] private CombatAttackBehaviour attackBehaviour;
    [SerializeField] private CharacterActionController actionController;
    [SerializeField] private CombatFaction[] targetFactions;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private bool shareFactionTarget = true;
    [SerializeField] private CombatMovementMode movementMode = CombatMovementMode.Melee;
    [SerializeField] private float combatMoveSpeedStatBonus = 2f;
    [SerializeField] private float summonPlayerLeashDistance = 14f;
    [SerializeField] private float rangedPreferredDistance = 6f;
    [SerializeField] private float rangedDistanceTolerance = 0.75f;
    [SerializeField] private float patrolRadius = 2.5f;
    [SerializeField] private float patrolMinStepDistance = 0.8f;
    [SerializeField] private float patrolMaxStepDistance = 2f;
    [SerializeField] private float patrolPointReachDistance = 0.6f;
    [SerializeField] private float patrolRepathInterval = 3f;
    [SerializeField] private Vector2 patrolIdleTimeRange = new Vector2(0.6f, 1.4f);
    [SerializeField] private CombatState currentState;

    private CombatIdentity currentTarget;
    private Vector3 homePosition;
    private Vector3 patrolPoint;
    private float nextTargetRefreshTime;
    private float nextAttackTime;
    private float nextPatrolPickTime;
    private float patrolPausedUntil;

    public string CurrentStateName => currentState.ToString();
    public bool HasActiveTarget => currentTarget != null && currentTarget.CanBeTargeted;
    public CharacterRuntimeStats CurrentTargetStats => HasActiveTarget ? currentTarget.Stats : null;

    private float MoveSpeed => selfStats != null ? selfStats.GetMoveSpeedMetersPerSecond(HasActiveTarget ? combatMoveSpeedStatBonus : 0f) : 2f;
    private bool CanMoveNow => actionController == null || actionController.CanMove;

    private void Awake()
    {
        ResolveReferences();
        homePosition = transform.position;
        PickPatrolPoint();
    }

    private void Update()
    {
        ResolveReferences();
        if (selfStats == null || selfStats.IsDead || (actionController != null && actionController.IsDead))
        {
            ClearTarget();
            currentState = CombatState.Idle;
            return;
        }

        if (actionController != null && actionController.IsAttacking)
        {
            currentState = CombatState.Attack;
            return;
        }

        if (IsSummonTooFarFromPlayer())
        {
            ClearTarget();
            currentState = CombatState.Idle;
            return;
        }

        RefreshTargetIfNeeded();
        if (!HasActiveTarget)
        {
            RunNoTargetState();
            return;
        }

        RunCombatState();
    }

    private void ResolveReferences()
    {
        if (selfStats == null)
        {
            selfStats = GetComponent<CharacterRuntimeStats>();
        }

        if (selfIdentity == null)
        {
            selfIdentity = GetComponent<CombatIdentity>();
        }

        if (attackBehaviour == null)
        {
            attackBehaviour = GetComponent<CombatAttackBehaviour>();
        }

        if (actionController == null)
        {
            actionController = GetComponent<CharacterActionController>();
        }
    }

    private void RefreshTargetIfNeeded()
    {
        if (Time.time < nextTargetRefreshTime && HasActiveTarget)
        {
            return;
        }

        nextTargetRefreshTime = Time.time + targetRefreshInterval;

        CombatIdentity nearest = FindNearestTarget(detectionRange);
        if (nearest != null)
        {
            SetTarget(nearest);
            return;
        }

        CombatIdentity sharedTarget = GetSharedTarget();
        if (sharedTarget != null)
        {
            currentTarget = sharedTarget;
            return;
        }

        ClearTarget();
    }

    private void RunNoTargetState()
    {
        if (selfIdentity != null && selfIdentity.Faction == CombatFaction.Enemy)
        {
            currentState = CombatState.Patrol;
            if (CanMoveNow)
            {
                PatrolAroundHome();
            }
            return;
        }

        currentState = CombatState.Idle;
    }

    private void RunCombatState()
    {
        Vector3 flatDelta = currentTarget.transform.position - transform.position;
        flatDelta.y = 0f;

        if (movementMode == CombatMovementMode.Ranged)
        {
            RunRangedState(flatDelta);
            return;
        }

        RunMeleeState(flatDelta);
    }

    private void RunMeleeState(Vector3 flatDelta)
    {
        float attackRange = GetAttackRange();
        if (flatDelta.sqrMagnitude > attackRange * attackRange)
        {
            currentState = CombatState.Chase;
            if (CanMoveNow)
            {
                MoveFlatTowards(currentTarget.transform.position, MoveSpeed);
            }
            return;
        }

        currentState = CombatState.Attack;
        FaceFlatDirection(flatDelta);
        TryAttack();
    }

    private void RunRangedState(Vector3 flatDelta)
    {
        float distance = flatDelta.magnitude;
        float tooNearDistance = Mathf.Max(0.5f, rangedPreferredDistance - rangedDistanceTolerance);
        float tooFarDistance = rangedPreferredDistance + rangedDistanceTolerance;

        if (distance > tooFarDistance)
        {
            currentState = CombatState.Chase;
            if (CanMoveNow)
            {
                MoveFlatTowards(currentTarget.transform.position, MoveSpeed);
            }
            return;
        }

        if (distance < tooNearDistance && !IsTargetApproachingSelf(flatDelta))
        {
            currentState = CombatState.HoldDistance;
            if (CanMoveNow)
            {
                Vector3 awayPosition = transform.position - flatDelta.normalized * (tooNearDistance - distance + 0.25f);
                MoveFlatTowards(awayPosition, MoveSpeed);
            }
            return;
        }

        currentState = CombatState.Attack;
        FaceFlatDirection(flatDelta);
        TryAttack();
    }

    private CombatIdentity FindNearestTarget(float range)
    {
        CombatIdentity[] identities = FindObjectsOfType<CombatIdentity>();
        CombatIdentity nearest = null;
        float nearestDistanceSqr = float.PositiveInfinity;
        float rangeSqr = range * range;

        foreach (CombatIdentity identity in identities)
        {
            if (identity == null || identity == selfIdentity || !identity.CanBeTargeted || !IsTargetFaction(identity.Faction))
            {
                continue;
            }

            Vector3 delta = identity.transform.position - transform.position;
            delta.y = 0f;
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr > rangeSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            nearestDistanceSqr = distanceSqr;
            nearest = identity;
        }

        return nearest;
    }

    private void SetTarget(CombatIdentity target)
    {
        currentTarget = target;
        if (ShouldShareFactionTarget() && target != null && target.CanBeTargeted)
        {
            SharedTargets[selfIdentity.Faction] = target;
        }
    }

    private CombatIdentity GetSharedTarget()
    {
        if (!ShouldShareFactionTarget())
        {
            return null;
        }

        CombatIdentity sharedTarget;
        if (!SharedTargets.TryGetValue(selfIdentity.Faction, out sharedTarget) || sharedTarget == null || !sharedTarget.CanBeTargeted)
        {
            SharedTargets.Remove(selfIdentity.Faction);
            return null;
        }

        return IsTargetFaction(sharedTarget.Faction) ? sharedTarget : null;
    }

    private void ClearTarget()
    {
        if (ShouldShareFactionTarget() && SharedTargets.ContainsKey(selfIdentity.Faction) && SharedTargets[selfIdentity.Faction] == currentTarget)
        {
            SharedTargets.Remove(selfIdentity.Faction);
        }

        currentTarget = null;
    }

    private bool ShouldShareFactionTarget()
    {
        return shareFactionTarget && selfIdentity != null && selfIdentity.Faction == CombatFaction.Summon;
    }

    private bool IsTargetFaction(CombatFaction faction)
    {
        if (targetFactions == null)
        {
            return false;
        }

        for (int i = 0; i < targetFactions.Length; i++)
        {
            if (targetFactions[i] == faction)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsSummonTooFarFromPlayer()
    {
        if (selfIdentity == null || selfIdentity.Faction != CombatFaction.Summon)
        {
            return false;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            return false;
        }

        Vector3 delta = player.transform.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude > summonPlayerLeashDistance * summonPlayerLeashDistance;
    }

    private bool IsTargetApproachingSelf(Vector3 selfToTarget)
    {
        if (currentTarget == null)
        {
            return false;
        }

        Vector3 targetForward = currentTarget.transform.forward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude <= 0.0001f || selfToTarget.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 targetToSelf = -selfToTarget.normalized;
        return Vector3.Dot(targetForward.normalized, targetToSelf) > 0.45f;
    }

    private void PatrolAroundHome()
    {
        if (Time.time < patrolPausedUntil)
        {
            return;
        }

        bool reachedPoint = (patrolPoint - transform.position).sqrMagnitude <= patrolPointReachDistance * patrolPointReachDistance;
        if (reachedPoint)
        {
            PauseBeforeNextPatrolPoint();
            return;
        }

        if (Time.time >= nextPatrolPickTime)
        {
            PickPatrolPoint();
        }

        MoveFlatTowards(patrolPoint, MoveSpeed);
    }

    private void PickPatrolPoint()
    {
        Vector3 fromHome = transform.position - homePosition;
        fromHome.y = 0f;
        Vector2 randomDirection = Random.insideUnitCircle;
        if (randomDirection.sqrMagnitude <= 0.01f)
        {
            randomDirection = Vector2.right;
        }

        float maxStep = Mathf.Min(Mathf.Max(0.1f, patrolMaxStepDistance), Mathf.Max(0.1f, patrolRadius));
        float minStep = Mathf.Min(Mathf.Max(0f, patrolMinStepDistance), maxStep);
        float stepDistance = Random.Range(minStep, maxStep);
        Vector3 step = new Vector3(randomDirection.normalized.x, 0f, randomDirection.normalized.y) * stepDistance;

        Vector3 candidate = transform.position + step;
        Vector3 candidateFromHome = candidate - homePosition;
        candidateFromHome.y = 0f;
        if (candidateFromHome.magnitude > patrolRadius)
        {
            Vector3 returnDirection = -fromHome.normalized;
            if (returnDirection.sqrMagnitude <= 0.0001f)
            {
                returnDirection = (homePosition - candidate).normalized;
            }

            candidate = transform.position + returnDirection * stepDistance;
        }

        patrolPoint = candidate;
        nextPatrolPickTime = Time.time + Random.Range(patrolRepathInterval * 0.85f, patrolRepathInterval * 1.25f);
    }

    private void PauseBeforeNextPatrolPoint()
    {
        float minPause = Mathf.Max(0f, Mathf.Min(patrolIdleTimeRange.x, patrolIdleTimeRange.y));
        float maxPause = Mathf.Max(minPause, Mathf.Max(patrolIdleTimeRange.x, patrolIdleTimeRange.y));
        patrolPausedUntil = Time.time + Random.Range(minPause, maxPause);
        PickPatrolPoint();
    }

    private float GetAttackRange()
    {
        return attackBehaviour != null ? attackBehaviour.AttackRange : 1.5f;
    }

    private void TryAttack()
    {
        if (attackBehaviour == null || selfStats == null || currentTarget == null || currentTarget.Stats == null)
        {
            return;
        }

        if (actionController != null)
        {
            actionController.TryStartAttack(selfStats, currentTarget.Stats, attackBehaviour);
            return;
        }

        if (!attackBehaviour.CanAttack(selfStats, nextAttackTime))
        {
            return;
        }

        attackBehaviour.Attack(selfStats, currentTarget.Stats);
        nextAttackTime = attackBehaviour.GetNextAttackTime(selfStats);
    }

    private void MoveFlatTowards(Vector3 targetPosition, float speed)
    {
        if (!CanMoveNow)
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
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
