using UnityEngine;

/// <summary>
/// Base class for attack styles used by CombatUnitAI.
/// Implementations decide how damage is delivered: melee contact, projectile, direct ranged hit, etc.
/// </summary>
public abstract class CombatAttackBehaviour : MonoBehaviour
{
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attackSpeedIntervalScale = 10f;
    [SerializeField] private float minimumAttackInterval = 0.1f;
    [SerializeField] private float rangeTolerance = 0.35f;

    public float AttackRange => attackRange;

    /// <summary>
    /// New attack speed rule: 1 stat point means 0.1 seconds of cooldown after the attack animation ends.
    /// The old serialized field name is kept so existing prefabs do not lose data.
    /// </summary>
    public float GetAttackCooldown(CharacterRuntimeStats attacker)
    {
                _ = attackSpeedIntervalScale;
float attackSpeed = attacker != null ? attacker.AttackSpeed : 1f;
        float cooldown = Mathf.Max(0f, attackSpeed) * 0.1f;
        return Mathf.Max(minimumAttackInterval, cooldown);
    }

    protected float GetAttackInterval(CharacterRuntimeStats attacker)
    {
        return GetAttackCooldown(attacker);
    }

    public virtual bool CanAttack(CharacterRuntimeStats attacker, float nextAttackTime)
    {
        return attacker != null && !attacker.IsDead && Time.time >= nextAttackTime;
    }

    public float GetNextAttackTime(CharacterRuntimeStats attacker)
    {
        return Time.time + GetAttackCooldown(attacker);
    }

    public bool IsTargetInRange(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        if (attacker == null || target == null || attacker.IsDead || target.IsDead)
        {
            return false;
        }

        Vector3 delta = target.transform.position - attacker.transform.position;
        delta.y = 0f;
        float allowedRange = attackRange + Mathf.Max(0f, rangeTolerance);
        return delta.sqrMagnitude <= allowedRange * allowedRange;
    }

    public virtual void DealDamageAtHitPoint(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        Attack(attacker, target);
    }

    public abstract void Attack(CharacterRuntimeStats attacker, CharacterRuntimeStats target);
}
