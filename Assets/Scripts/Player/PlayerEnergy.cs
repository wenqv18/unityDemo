using System;
using UnityEngine;

/// <summary>
/// Player-only runtime energy used by drawing and inventory items.
/// </summary>
public sealed class PlayerEnergy : MonoBehaviour
{
    [SerializeField] private int maxEnergy = 1000;
    [SerializeField] private int currentEnergy = 1000;

    public int MaxEnergy => maxEnergy;
    public int CurrentEnergy => currentEnergy;
    public event Action<PlayerEnergy> EnergyChanged;

    private void Awake()
    {
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        EnergyChanged?.Invoke(this);
    }

    public bool CanSpend(int amount)
    {
        return Mathf.Max(0, amount) <= currentEnergy;
    }

    public bool TrySpend(int amount)
    {
        int cost = Mathf.Max(0, amount);
        if (cost > currentEnergy)
        {
            return false;
        }

        if (cost == 0)
        {
            return true;
        }

        currentEnergy -= cost;
        EnergyChanged?.Invoke(this);
        return true;
    }

    public int Restore(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int previous = currentEnergy;
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
        int restored = currentEnergy - previous;
        if (restored > 0)
        {
            EnergyChanged?.Invoke(this);
        }

        return restored;
    }
}
