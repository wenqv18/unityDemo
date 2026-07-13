using UnityEngine;

/// <summary>
/// MVC Controller service: maps an inventory item to gameplay behavior.
/// Add concrete item effects here after PackageTable ids are defined.
/// </summary>
public static class PackageItemUseService
{
    private const int EnergyItemId = 0;
    private const int EnergyRestoreAmount = 200;
    private const int HealItemId = 1;
    private const int HealAmount = 40;

    public static bool TryUse(PackageLocalData.PackageLocalItem item)
    {
        if (item == null || item.Number <= 0)
        {
            return false;
        }

        switch (item.id)
        {
            case EnergyItemId:
                return TryRestorePlayerEnergy();
            case HealItemId:
                return TryHealPlayer();
            default:
                Debug.Log("Item id " + item.id + " has no use behavior yet.");
                return false;
        }
    }

    private static bool TryRestorePlayerEnergy()
    {
        GameObject player = GameObject.Find("Player");
        PlayerEnergy energy = player != null ? player.GetComponent<PlayerEnergy>() : Object.FindFirstObjectByType<PlayerEnergy>();
        if (energy == null)
        {
            Debug.LogWarning("Player energy not found. Item use cancelled.");
            return false;
        }

        energy.Restore(EnergyRestoreAmount);
        return true;
    }

    private static bool TryHealPlayer()
    {
        GameObject player = GameObject.Find("Player");
        CharacterRuntimeStats stats = player != null ? player.GetComponent<CharacterRuntimeStats>() : Object.FindFirstObjectByType<CharacterRuntimeStats>();
        if (stats == null)
        {
            Debug.LogWarning("Player stats not found. Item use cancelled.");
            return false;
        }

        if (stats.IsDead)
        {
            return false;
        }

        stats.Heal(HealAmount);
        return true;
    }
}
