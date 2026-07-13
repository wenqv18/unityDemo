using UnityEngine;

/// <summary>
/// Marks a character as a combat participant and exposes its faction for target selection.
/// </summary>
public sealed class CombatIdentity : MonoBehaviour
{
    [SerializeField] private CombatFaction faction;
    [SerializeField] private CharacterRuntimeStats stats;

    public CombatFaction Faction => faction;
    public CharacterRuntimeStats Stats => stats;
    public bool CanBeTargeted => stats != null && !stats.IsDead;

    private void Awake()
    {
        ResolveStats();
    }

    private void OnValidate()
    {
        ResolveStats();
    }

    public void SetFaction(CombatFaction value)
    {
        faction = value;
    }

    private void ResolveStats()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterRuntimeStats>();
        }
    }
}
