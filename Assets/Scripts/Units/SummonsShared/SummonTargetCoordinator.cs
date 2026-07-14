/// <summary>
/// Keeps the "one summon finds a target, all summons help" behavior centralized.
/// Enemy grouping remains local and separate.
/// </summary>
public static class SummonTargetCoordinator
{
    private static CombatIdentity sharedTarget;

    public static CombatIdentity SharedTarget
    {
        get
        {
            if (sharedTarget == null || !sharedTarget.CanBeTargeted)
            {
                sharedTarget = null;
            }

            return sharedTarget;
        }
    }

    public static void SetSharedTarget(CombatIdentity target)
    {
        if (target != null && target.CanBeTargeted)
        {
            if (sharedTarget != target)
            {
                GameSoundPlayer.PlayAt(GameSoundPlayer.SummonBeginPath, target.transform.position);
            }

            sharedTarget = target;
        }
    }

    public static void ClearIfTarget(CombatIdentity target)
    {
        if (sharedTarget == target)
        {
            sharedTarget = null;
        }
    }
}
