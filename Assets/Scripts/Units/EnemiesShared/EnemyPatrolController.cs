using UnityEngine;

/// <summary>
/// Shared small-area patrol used by enemy units before combat starts.
/// </summary>
public sealed class EnemyPatrolController : MonoBehaviour, ICharacterAnimationStateProvider
{
    [SerializeField] private float patrolRadius = 2.2f;
    [SerializeField] private float patrolMinStepDistance = 0.55f;
    [SerializeField] private float patrolMaxStepDistance = 1.45f;
    [SerializeField] private float patrolPointReachDistance = 0.45f;
    [SerializeField] private float patrolRepathInterval = 2.4f;
    [SerializeField] private Vector2 patrolIdleTimeRange = new Vector2(0.7f, 1.5f);

    private Vector3 homePosition;
    private Vector3 patrolPoint;
    private float nextPatrolPickTime;
    private float patrolPausedUntil;
    private bool externalMovementLocked;
    private CharacterActionController actionController;

    public string AnimationStateName => "Patrol";
    public bool HasActiveCombatTarget => false;
    public bool WantsRunAnimation => false;
    public bool WantsWalkAnimation => !externalMovementLocked;

    private void Awake()
    {
        homePosition = transform.position;
        actionController = GetComponent<CharacterActionController>();
        PickPatrolPoint();
    }

    public void SetExternalMovementLocked(bool locked)
    {
        externalMovementLocked = locked;
    }

    public void TickPatrol(float moveSpeed)
    {
        if (externalMovementLocked || (actionController != null && !actionController.CanMove))
        {
            return;
        }

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

        MoveFlatTowards(patrolPoint, moveSpeed);
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
