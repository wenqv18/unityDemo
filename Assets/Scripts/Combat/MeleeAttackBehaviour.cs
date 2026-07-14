using UnityEngine;

/// <summary>
/// Direct melee damage. CharacterActionController decides when the hit frame is reached.
/// </summary>
public sealed class MeleeAttackBehaviour : CombatAttackBehaviour
{
    [SerializeField] private string hitSoundResourcePath = GameSoundPlayer.SwordAttackPath;
    [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 1f;

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
        int dealtDamage = target.TakeDamage(damage);
        if (dealtDamage > 0)
        {
            GameSoundPlayer.PlayAt(hitSoundResourcePath, target.transform.position, hitSoundVolume);
        }
    }
}
