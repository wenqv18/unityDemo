using UnityEngine;

/// <summary>
/// Melee enemy brain: patrols at home before combat, then chases and attacks hostile units.
/// </summary>
public sealed class MeleeEnemyAI : MonoBehaviour, ICharacterAnimationStateProvider
{
    [SerializeField] private CharacterRuntimeStats selfStats;
    [SerializeField] private CombatIdentity selfIdentity;
    [SerializeField] private MeleeAttackBehaviour attackBehaviour;
    [SerializeField] private CharacterActionController actionController;
    [SerializeField] private EnemyPatrolController patrolController;
    [SerializeField] private CombatFaction[] targetFactions = { CombatFaction.Player, CombatFaction.Summon };
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private float combatMoveSpeedStatBonus = 2f;
    [SerializeField] private string currentState = "Idle";

    private CombatIdentity currentTarget;
    private float nextTargetRefreshTime;

    public string AnimationStateName => currentState;
    public bool HasActiveCombatTarget => HasTarget;
    public bool WantsRunAnimation => currentState == "Chase";
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

        RunMeleeCombat();
    }

    private void RunMeleeCombat()
    {
        Vector3 flatDelta = currentTarget.transform.position - transform.position;
        flatDelta.y = 0f;
        float attackRange = attackBehaviour != null ? attackBehaviour.AttackRange : 1.5f;
        if (flatDelta.sqrMagnitude > attackRange * attackRange)
        {
            currentState = "Chase";
            if (CanMoveNow)
            {
                MoveFlatTowards(currentTarget.transform.position, CombatMoveSpeed);
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

    private void ResolveReferences()
    {
        if (selfStats == null) selfStats = GetComponent<CharacterRuntimeStats>();
        if (selfIdentity == null) selfIdentity = GetComponent<CombatIdentity>();
        if (attackBehaviour == null) attackBehaviour = GetComponent<MeleeAttackBehaviour>();
        if (actionController == null) actionController = GetComponent<CharacterActionController>();
        if (patrolController == null) patrolController = GetComponent<EnemyPatrolController>();
    }

    private void SetPatrolLocked(bool locked)
    {
        if (patrolController != null)
        {
            patrolController.SetExternalMovementLocked(locked);
        }
    }

    private void MoveFlatTowards(Vector3 targetPosition, float speed)
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
}
