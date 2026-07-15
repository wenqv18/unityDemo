using UnityEngine;

/// <summary>
/// Ranged enemy brain: patrols at home before combat, then keeps range and fires projectiles.
/// </summary>
public sealed class RangedEnemyAI : MonoBehaviour, ICharacterAnimationStateProvider
{
    [SerializeField] private CharacterRuntimeStats selfStats;
    [SerializeField] private CombatIdentity selfIdentity;
    [SerializeField] private RangedAttackBehaviour attackBehaviour;
    [SerializeField] private CharacterActionController actionController;
    [SerializeField] private EnemyPatrolController patrolController;
    [SerializeField] private UnitNavigationMover navigationMover;
    [SerializeField] private CombatFaction[] targetFactions = { CombatFaction.Player, CombatFaction.Summon };
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private float combatMoveSpeedStatBonus = 2f;
    [SerializeField] private float preferredDistance = 6f;
    [SerializeField] private float distanceTolerance = 0.75f;
    [SerializeField] private string currentState = "Idle";

    private CombatIdentity currentTarget;
    private float nextTargetRefreshTime;

    public string AnimationStateName => currentState;
    public bool HasActiveCombatTarget => HasTarget;
    public bool WantsRunAnimation => currentState == "Chase" || currentState == "HoldDistance";
    public bool WantsWalkAnimation => currentState == "Patrol";

    private bool HasTarget => currentTarget != null && currentTarget.CanBeTargeted;
    private float NormalMoveSpeed => selfStats != null ? selfStats.GetMoveSpeedMetersPerSecond() : 2f;
    private float CombatMoveSpeed => selfStats != null ? selfStats.GetMoveSpeedMetersPerSecond(combatMoveSpeedStatBonus) : 2f;
    private bool CanMoveNow => actionController == null || actionController.CanMove;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();
        if (selfStats == null || selfStats.IsDead || (actionController != null && actionController.IsDead))
        {
            currentTarget = null;
            SetPatrolLocked(true);
            currentState = "Idle";
            return;
        }

        RefreshTargetIfNeeded();
        SetPatrolLocked(HasTarget);
        if (!HasTarget)
        {
            currentState = "Patrol";
            if (patrolController != null)
            {
                patrolController.TickPatrol(NormalMoveSpeed);
            }
            return;
        }

        RunRangedCombat();
    }

    private void RunRangedCombat()
    {
        Vector3 flatDelta = currentTarget.transform.position - transform.position;
        flatDelta.y = 0f;
        float distance = flatDelta.magnitude;
        float tooNearDistance = Mathf.Max(0.5f, preferredDistance - distanceTolerance);
        float tooFarDistance = preferredDistance + distanceTolerance;
        bool hasClearLine = navigationMover == null || navigationMover.HasClearLineTo(currentTarget.transform);

        if (distance > tooFarDistance || !hasClearLine)
        {
            currentState = "Chase";
            if (CanMoveNow)
            {
                MoveFlatTowards(currentTarget.transform.position, CombatMoveSpeed, preferredDistance);
            }
            return;
        }

        if (distance < tooNearDistance && !IsTargetApproachingSelf(flatDelta))
        {
            currentState = "HoldDistance";
            if (CanMoveNow)
            {
                Vector3 awayPosition = transform.position - flatDelta.normalized * (tooNearDistance - distance + 0.25f);
                MoveFlatTowards(awayPosition, CombatMoveSpeed);
            }
            return;
        }

        currentState = "Attack";
        FaceFlatDirection(flatDelta);
        if (actionController != null)
        {
            actionController.TryStartAttack(selfStats, currentTarget.Stats, attackBehaviour);
        }
    }

    private void RefreshTargetIfNeeded()
    {
        if (Time.time < nextTargetRefreshTime && HasTarget)
        {
            return;
        }

        nextTargetRefreshTime = Time.time + targetRefreshInterval;
        currentTarget = CombatTargetFinder.FindNearestTarget(selfIdentity, targetFactions, detectionRange);
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

    private void ResolveReferences()
    {
        if (selfStats == null) selfStats = GetComponent<CharacterRuntimeStats>();
        if (selfIdentity == null) selfIdentity = GetComponent<CombatIdentity>();
        if (attackBehaviour == null) attackBehaviour = GetComponent<RangedAttackBehaviour>();
        if (actionController == null) actionController = GetComponent<CharacterActionController>();
        if (patrolController == null) patrolController = GetComponent<EnemyPatrolController>();
        if (navigationMover == null) navigationMover = GetComponent<UnitNavigationMover>();
        if (navigationMover == null) navigationMover = gameObject.AddComponent<UnitNavigationMover>();
    }

    private void SetPatrolLocked(bool locked)
    {
        if (patrolController != null)
        {
            patrolController.SetExternalMovementLocked(locked);
        }
    }

    private void MoveFlatTowards(Vector3 targetPosition, float speed, float stoppingDistance = 0f)
    {
        if (navigationMover != null)
        {
            navigationMover.MoveTowards(targetPosition, speed, stoppingDistance);
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
}
