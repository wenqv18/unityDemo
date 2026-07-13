/// <summary>
/// Direct melee damage. CharacterActionController decides when the hit frame is reached.
/// </summary>
public sealed class MeleeAttackBehaviour : CombatAttackBehaviour
{
    public override void Attack(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        DealDamageAtHitPoint(attacker, target);
    }

    public override void DealDamageAtHitPoint(CharacterRuntimeStats attacker, CharacterRuntimeStats target)
    {
        if (attacker == null || attacker.IsDead || target == null || target.IsDead)
        {
            return;
        }

        int damage = CombatMath.GetAttackDamage(attacker);
        target.TakeDamage(damage);
    }
}
