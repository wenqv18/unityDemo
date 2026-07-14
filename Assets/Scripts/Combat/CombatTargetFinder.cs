using UnityEngine;

/// <summary>
/// Shared target lookup for combat units. It intentionally stays small so summon and enemy
/// AI can own their own state machines while using the same targeting rules.
/// </summary>
public static class CombatTargetFinder
{
    public static CombatIdentity FindNearestTarget(CombatIdentity self, CombatFaction[] targetFactions, float range)
    {
        if (self == null || targetFactions == null || targetFactions.Length == 0 || range <= 0f)
        {
            return null;
        }

        CombatIdentity[] identities = Object.FindObjectsOfType<CombatIdentity>();
        CombatIdentity nearest = null;
        float nearestDistanceSqr = float.PositiveInfinity;
        float rangeSqr = range * range;

        for (int i = 0; i < identities.Length; i++)
        {
            CombatIdentity candidate = identities[i];
            if (candidate == null || candidate == self || !candidate.CanBeTargeted || !IsTargetFaction(candidate.Faction, targetFactions))
            {
                continue;
            }

            Vector3 delta = candidate.transform.position - self.transform.position;
            delta.y = 0f;
            float distanceSqr = delta.sqrMagnitude;
            if (distanceSqr > rangeSqr || distanceSqr >= nearestDistanceSqr)
            {
                continue;
            }

            nearest = candidate;
            nearestDistanceSqr = distanceSqr;
        }

        return nearest;
    }

    public static bool IsTargetFaction(CombatFaction faction, CombatFaction[] targetFactions)
    {
        if (targetFactions == null)
        {
            return false;
        }

        for (int i = 0; i < targetFactions.Length; i++)
        {
            if (targetFactions[i] == faction)
            {
                return true;
            }
        }

        return false;
    }
}
