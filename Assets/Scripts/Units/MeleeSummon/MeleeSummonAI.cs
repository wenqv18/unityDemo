using UnityEngine;

/// <summary>
/// Melee summon combat brain. Non-combat follow/return is owned by SummonFollowController.
/// </summary>
public sealed class MeleeSummonAI : MonoBehaviour, ICharacterAnimationStateProvider
{
    [SerializeField] private CharacterRuntimeStats selfStats;
    [SerializeField] private CombatIdentity selfIdentity;
    [SerializeField] private MeleeAttackBehaviour attackBehaviour;
    [SerializeField] private CharacterActionController actionController;
    [SerializeField] private SummonFollowController followController;
    [SerializeField] private UnitNavigationMover navigationMover;
    [SerializeField] private CombatFaction[] targetFactions = { CombatFaction.Enemy };
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private float combatMoveSpeedStatBonus = 2f;
    [SerializeField] private string currentState = "Idle";

    private CombatIdentity currentTarget;
    private float nextTargetRefreshTime;

    public string AnimationStateName => currentState;
    public bool HasActiveCombatTarget => HasTarget;
    public bool WantsRunAnimation => currentState == "Chase";
    public bool WantsWalkAnimation => false;

    private bool HasTarget => currentTarget != null && currentTarget.CanBeTargeted;
    private float MoveSpeed => selfStats != null ? selfStats.GetMoveSpeedMetersPerSecond(HasTarget ? combatMoveSpeedStatBonus : 0f) : 2f;
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
            ClearTarget();
            SetFollowLocked(false);
            currentState = "Idle";
            return;
        }

        if (followController != null && followController.IsSpawnMoveLocked)
        {
            SetFollowLocked(false);
            currentState = "Idle";
            return;
        }

        RefreshTargetIfNeeded();
        SetFollowLocked(HasTarget);
        if (!HasTarget)
        {
            currentState = "Idle";
            return;
        }

        RunMeleeCombat();
    }

    private void RunMeleeCombat()
    {
        Vector3 flatDelta = currentTarget.transform.position - transform.position;
        flatDelta.y = 0f;
        float attackRange = attackBehaviour != null ? attackBehaviour.AttackRange : 1.5f;
        bool hasClearLine = navigationMover == null || navigationMover.HasClearLineTo(currentTarget.transform);
        if (flatDelta.sqrMagnitude > attackRange * attackRange || !hasClearLine)
        {
            currentState = "Chase";
            if (CanMoveNow)
            {
                MoveFlatTowards(currentTarget.transform.position, MoveSpeed, attackRange * 0.9f);
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
        CombatIdentity nearest = CombatTargetFinder.FindNearestTarget(selfIdentity, targetFactions, detectionRange);
        if (nearest != null)
        {
            currentTarget = nearest;
            SummonTargetCoordinator.SetSharedTarget(nearest);
            return;
        }

        CombatIdentity shared = SummonTargetCoordinator.SharedTarget;
        currentTarget = shared != null && CombatTargetFinder.IsTargetFaction(shared.Faction, targetFactions) ? shared : null;
    }

    private void ResolveReferences()
    {
        if (selfStats == null) selfStats = GetComponent<CharacterRuntimeStats>();
        if (selfIdentity == null) selfIdentity = GetComponent<CombatIdentity>();
        if (attackBehaviour == null) attackBehaviour = GetComponent<MeleeAttackBehaviour>();
        if (actionController == null) actionController = GetComponent<CharacterActionController>();
        if (followController == null) followController = GetComponent<SummonFollowController>();
        if (navigationMover == null) navigationMover = GetComponent<UnitNavigationMover>();
        if (navigationMover == null) navigationMover = gameObject.AddComponent<UnitNavigationMover>();
    }

    private void ClearTarget()
    {
        SummonTargetCoordinator.ClearIfTarget(currentTarget);
        currentTarget = null;
    }

    private void SetFollowLocked(bool locked)
    {
        if (followController != null)
        {
            followController.SetExternalMovementLocked(locked);
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
