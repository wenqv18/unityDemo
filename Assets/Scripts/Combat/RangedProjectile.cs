using UnityEngine;

/// <summary>
/// Projectile that follows a curved flight path toward a combat target and applies damage on arrival.
/// Visual children can be replaced later without changing combat logic.
/// </summary>
public sealed class RangedProjectile : MonoBehaviour
{
    [SerializeField] private float defaultSpeed = 18f;
    [SerializeField] private float defaultArcHeight = 1.2f;
    [SerializeField] private float hitRadius = 0.35f;
    [SerializeField] private float targetAimHeight = 0.9f;
    [SerializeField] private float maxLifetime = 5f;

    private CharacterRuntimeStats attacker;
    private CharacterRuntimeStats target;
    private Vector3 startPosition;
    private Vector3 lastPosition;
    private float elapsedTime;
    private float flightDuration;
    private int damage;
    private bool launched;

    public void Launch(CharacterRuntimeStats source, CharacterRuntimeStats destination, int attackDamage, float speed, float arcHeight)
    {
        attacker = source;
        target = destination;
        damage = attackDamage;
        defaultSpeed = Mathf.Max(0.1f, speed);
        defaultArcHeight = Mathf.Max(0f, arcHeight);
        startPosition = transform.position;
        lastPosition = startPosition;

        Vector3 targetPosition = GetTargetPosition();
        float distance = Vector3.Distance(startPosition, targetPosition);
        flightDuration = Mathf.Max(0.05f, distance / defaultSpeed);
        elapsedTime = 0f;
        launched = true;
        FaceMovement(targetPosition - startPosition);
    }

    private void Update()
    {
        if (!launched)
        {
            return;
        }

        if (target == null || target.IsDead || elapsedTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;
        Vector3 targetPosition = GetTargetPosition();
        float t = Mathf.Clamp01(elapsedTime / flightDuration);
        Vector3 nextPosition = Vector3.Lerp(startPosition, targetPosition, t);
        nextPosition.y += Mathf.Sin(t * Mathf.PI) * defaultArcHeight;

        transform.position = nextPosition;
        FaceMovement(nextPosition - lastPosition);
        lastPosition = nextPosition;

        if (t >= 1f || Vector3.Distance(nextPosition, targetPosition) <= hitRadius)
        {
            HitTarget();
        }
    }

    private Vector3 GetTargetPosition()
    {
        if (target == null)
        {
            return transform.position;
        }

        return target.transform.position + Vector3.up * targetAimHeight;
    }

    private void HitTarget()
    {
        if (target != null && !target.IsDead)
        {
            target.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    private void FaceMovement(Vector3 movement)
    {
        if (movement.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
    }
}
