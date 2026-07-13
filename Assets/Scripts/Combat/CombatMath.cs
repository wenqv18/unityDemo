using UnityEngine;

/// <summary>
/// Shared combat number helpers. Keep formulas here so every attack style uses the same rules.
/// </summary>
public static class CombatMath
{
    private const float MinimumSpeed = 0.01f;

    /// <summary>
    /// Converts an attack speed value into seconds between attacks.
    /// With the default scale of 10, attackSpeed 10 means about one attack per second.
    /// </summary>
    public static float GetAttackInterval(float attackSpeed, float intervalScale, float minimumInterval)
    {
        float safeSpeed = Mathf.Max(attackSpeed, MinimumSpeed);
        float safeScale = Mathf.Max(intervalScale, MinimumSpeed);
        float interval = safeScale / safeSpeed;
        return Mathf.Max(minimumInterval, interval);
    }

    /// <summary>
    /// Reads attack damage from the runtime stats and clamps invalid values.
    /// Defense is applied by CharacterRuntimeStats.TakeDamage on the target.
    /// </summary>
    public static int GetAttackDamage(CharacterRuntimeStats attacker)
    {
        return attacker != null ? Mathf.Max(0, attacker.AttackDamage) : 0;
    }
}
