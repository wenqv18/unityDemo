using UnityEngine;

/// <summary>
/// Ranged attack effect. CharacterActionController owns animation, hit timing, and cooldown;
/// this behaviour only fires the projectile when the attack reaches its hit frame.
/// </summary>
public sealed class RangedAttackBehaviour : CombatAttackBehaviour
{
    [SerializeField] private RangedProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float prepareDuration = 0.35f;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileArcHeight = 1.2f;
    [SerializeField] private float fallbackFireHeight = 1.2f;
    [SerializeField] private bool logMissingProjectile;

    public override void Attack(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        DealDamageAtHitPoint(attacker, target);
    }

    public override void DealDamageAtHitPoint(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        _ = prepareDuration;

        if (attacker == null || attacker.IsDead || target == null || target.IsDead || !IsTargetInRange(attacker, target))
        {
            return;
        }

        Shoot(attacker, target);
    }

    private void Shoot(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        if (projectilePrefab == null)
        {
            if (logMissingProjectile)
            {
                Debug.LogWarning($"{name} has no ranged projectile prefab assigned.", this);
            }
            return;
        }

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position + Vector3.up * fallbackFireHeight;
        RangedProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        int damage = CombatMath.GetAttackDamage(attacker);
        projectile.Launch(attacker, target, damage, projectileSpeed, projectileArcHeight);
    }
}
