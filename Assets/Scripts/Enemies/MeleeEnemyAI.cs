using UnityEngine;

/// <summary>
/// Minimal melee enemy AI: finds the player, chases within detection range, and deals damage in attack range.
/// This is intentionally lightweight so it can later be replaced by NavMesh, animation events, or a larger state machine.
/// </summary>
public sealed class MeleeEnemyAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }

    [SerializeField] private CharacterRuntimeStats selfStats;
    [SerializeField] private CharacterRuntimeStats targetStats;
    [SerializeField] private Transform target;
    [SerializeField] private string targetName = "Player";
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float minimumAttackInterval = 0.35f;
    [SerializeField] private float combatMoveSpeedStatBonus = 2f;
    [SerializeField] private EnemyState currentState;

    private float nextAttackTime;

    public string CurrentStateName => currentState.ToString();

    private float MoveSpeed => selfStats != null ? selfStats.GetMoveSpeedMetersPerSecond(currentState == EnemyState.Idle ? 0f : combatMoveSpeedStatBonus) : 2f;
    private int AttackDamage => selfStats != null ? selfStats.AttackDamage : 1;

    private float AttackInterval
    {
        get
        {
            float attackSpeed = selfStats != null ? selfStats.AttackSpeed : 1f;
            return Mathf.Max(minimumAttackInterval, 1f / Mathf.Max(attackSpeed, 0.01f));
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();
        if (target == null || targetStats == null || targetStats.IsDead)
        {
            currentState = EnemyState.Idle;
            return;
        }

        Vector3 flatDelta = target.position - transform.position;
        flatDelta.y = 0f;
        float distance = flatDelta.magnitude;

        if (distance > detectionRange)
        {
            currentState = EnemyState.Idle;
            return;
        }

        if (distance > attackRange)
        {
            currentState = EnemyState.Chase;
            ChaseTarget(flatDelta);
            return;
        }

        currentState = EnemyState.Attack;
        FaceDirection(flatDelta);
        TryAttack();
    }

    private void ResolveReferences()
    {
        if (selfStats == null)
        {
            selfStats = GetComponent<CharacterRuntimeStats>();
        }

        if (target == null)
        {
            GameObject targetObject = GameObject.Find(targetName);
            if (targetObject != null)
            {
                target = targetObject.transform;
                targetStats = targetObject.GetComponent<CharacterRuntimeStats>();
            }
        }
        else if (targetStats == null)
        {
            targetStats = target.GetComponent<CharacterRuntimeStats>();
        }
    }

    private void ChaseTarget(Vector3 flatDelta)
    {
        FaceDirection(flatDelta);
        Vector3 targetPosition = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, MoveSpeed * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + AttackInterval;
        targetStats.TakeDamage(AttackDamage);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
