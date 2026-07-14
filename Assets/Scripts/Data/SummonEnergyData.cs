using UnityEngine;

/// <summary>
/// Extra summon-only data used by drawing cost scaling.
/// This stays separate from CharacterData so enemy and player data remain untouched.
/// </summary>
[CreateAssetMenu(fileName = "SummonEnergyData", menuName = "Game Data/Summon Energy Data")]
public sealed class SummonEnergyData : ScriptableObject
{
    [SerializeField] private int energyBaseCost = 100;

    public int EnergyBaseCost => Mathf.Max(1, energyBaseCost);
}
