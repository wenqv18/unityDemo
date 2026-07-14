using System.Collections;
using UnityEngine;

/// <summary>
/// Coordinates high-priority character actions: attack, hit reaction, death, movement lock,
/// and attack cooldown timing. Unit AI scripts ask this component before moving or attacking.
/// </summary>
[RequireComponent(typeof(CharacterRuntimeStats))]
public sealed class CharacterActionController : MonoBehaviour
{
    [SerializeField] private CharacterRuntimeStats stats;
    [SerializeField] private CharacterAnimationDriver animationDriver;
    [SerializeField] private float attackAnimationDuration = 1.667f;
    [SerializeField, Range(0.01f, 0.99f)] private float attackHitNormalizedTime = 0.5f;
    [SerializeField] private float hitReactionDuration = 0.7f;
    [SerializeField] private float hitReactionInterval = 5f;
    [SerializeField] private bool pauseAttackCooldownDuringHit = true;

    private Coroutine attackRoutine;
    private Coroutine hitRoutine;
    private float remainingAttackCooldown;
    private float nextHitReactionAllowedTime;
    private int previousHealth;
    private bool isAttacking;
    private bool isHitReacting;
    private bool isDead;

    public bool IsAttacking => isAttacking;
    public bool IsHitReacting => isHitReacting;
    public bool IsDead => isDead || (stats != null && stats.IsDead);
    public bool CanMove => !IsDead && !isAttacking && !isHitReacting;
    public bool CanStartAttack => !IsDead && !isAttacking && !isHitReacting && remainingAttackCooldown <= 0f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (stats != null)
        {
            isDead = stats.IsDead;
            previousHealth = stats.CurrentHealth;
            stats.HealthChanged += HandleHealthChanged;
            stats.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.HealthChanged -= HandleHealthChanged;
            stats.Died -= HandleDied;
        }
    }

    private void Update()
    {
        if (IsDead || remainingAttackCooldown <= 0f)
        {
            return;
        }

        if (pauseAttackCooldownDuringHit && isHitReacting)
        {
            return;
        }

        remainingAttackCooldown = Mathf.Max(0f, remainingAttackCooldown - Time.deltaTime);
    }

    public bool TryStartAttack(CharacterRuntimeStats attacker, CharacterRuntimeStats target, CombatAttackBehaviour attackBehaviour)
    {
        if (!CanStartAttack || attacker == null || attacker.IsDead || target == null || target.IsDead || attackBehaviour == null)
        {
            return false;
        }

        if (!attackBehaviour.IsTargetInRange(attacker, target))
        {
            return false;
        }

        attackRoutine = StartCoroutine(AttackRoutine(attacker, target, attackBehaviour));
        return true;
    }

    public void ForceDeath()
    {
        HandleDied(stats);
    }

    private IEnumerator AttackRoutine(CharacterRuntimeStats attacker, CharacterRuntimeStats target, CombatAttackBehaviour attackBehaviour)
    {
        isAttacking = true;
        bool damageApplied = false;
        float safeDuration = Mathf.Max(0.01f, attackAnimationDuration);
        float hitTime = safeDuration * Mathf.Clamp01(attackHitNormalizedTime);

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
            isHitReacting = false;
        }

        if (animationDriver != null)
        {
            animationDriver.PlayAttack(safeDuration);
        }

        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            if (IsDead || attacker == null || attacker.IsDead)
            {
                isAttacking = false;
                attackRoutine = null;
                yield break;
            }

            if (!damageApplied && elapsed >= hitTime)
            {
                if (target != null && !target.IsDead && attackBehaviour.IsTargetInRange(attacker, target))
                {
                    attackBehaviour.DealDamageAtHitPoint(attacker, target);
                }

                damageApplied = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!damageApplied && !IsDead && attacker != null && !attacker.IsDead && target != null && !target.IsDead && attackBehaviour.IsTargetInRange(attacker, target))
        {
            attackBehaviour.DealDamageAtHitPoint(attacker, target);
        }

        remainingAttackCooldown = attackBehaviour.GetAttackCooldown(attacker);
        isAttacking = false;
        attackRoutine = null;
    }

    private void HandleHealthChanged(CharacterRuntimeStats changedStats)
    {
        if (changedStats == null)
        {
            return;
        }

        bool tookDamage = changedStats.CurrentHealth < previousHealth;
        previousHealth = changedStats.CurrentHealth;

        if (!tookDamage || changedStats.IsDead || isAttacking || isDead)
        {
            return;
        }

        if (Time.time < nextHitReactionAllowedTime || isHitReacting)
        {
            return;
        }

        nextHitReactionAllowedTime = Time.time + hitReactionInterval;
        hitRoutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator HitReactionRoutine()
    {
        isHitReacting = true;
        if (animationDriver != null)
        {
            animationDriver.PlayHit(hitReactionDuration);
        }

        float elapsed = 0f;
        while (elapsed < hitReactionDuration)
        {
            if (IsDead)
            {
                isHitReacting = false;
                hitRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        isHitReacting = false;
        hitRoutine = null;
    }

    private void HandleDied(CharacterRuntimeStats deadStats)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        isAttacking = false;
        isHitReacting = false;
        remainingAttackCooldown = 0f;

        if (animationDriver != null)
        {
            animationDriver.PlayDeath();
        }
    }

    private void ResolveReferences()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterRuntimeStats>();
        }

        if (animationDriver == null)
        {
            animationDriver = GetComponent<CharacterAnimationDriver>();
        }
    }
}
