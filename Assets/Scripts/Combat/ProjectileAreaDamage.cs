using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional projectile splash damage. Put this on projectile prefabs that should
/// damage nearby combat units when their impact effect is triggered.
/// </summary>
public sealed class ProjectileAreaDamage : MonoBehaviour
{
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private bool affectDirectHitTarget;
    [SerializeField] private bool onlyDamageSpecificFaction = true;
    [SerializeField] private CombatFaction damagedFaction = CombatFaction.Enemy;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private bool drawDebug;

    private readonly Collider[] overlapResults = new Collider[32];

    public void Apply(CharacterRuntimeStats attacker, CharacterRuntimeStats directHitTarget, int baseDamage)
    {
        int splashDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * Mathf.Max(0f, damageMultiplier)));
        if (splashDamage <= 0 || radius <= 0f)
        {
            return;
        }

        Vector3 center = transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, overlapResults, targetMask, QueryTriggerInteraction.Ignore);
        HashSet<CharacterRuntimeStats> damagedStats = new HashSet<CharacterRuntimeStats>();

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null)
            {
                continue;
            }

            CombatIdentity identity = hit.GetComponentInParent<CombatIdentity>();
            CharacterRuntimeStats stats = identity != null ? identity.Stats : hit.GetComponentInParent<CharacterRuntimeStats>();
            if (stats == null || stats.IsDead || stats == attacker || damagedStats.Contains(stats))
            {
                continue;
            }

            if (!affectDirectHitTarget && stats == directHitTarget)
            {
                continue;
            }

            if (identity != null && !ShouldDamage(identity))
            {
                continue;
            }

            stats.TakeDamage(splashDamage);
            damagedStats.Add(stats);
        }

        if (drawDebug)
        {
            Debug.DrawRay(center, Vector3.up * radius, Color.red, 1f);
        }
    }

    private bool ShouldDamage(CombatIdentity identity)
    {
        if (!onlyDamageSpecificFaction)
        {
            return true;
        }

        return identity.Faction == damagedFaction;
    }
}
