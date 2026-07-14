using UnityEngine;

/// <summary>
/// Temporary summon input bridge for testing melee summon creation.
/// </summary>
public sealed class PlayerSummonInputController : MonoBehaviour
{
    [SerializeField] private PlayerSummonSpawner summonSpawner;
    [SerializeField] private KeyCode summonKey = KeyCode.Q;

    private void Awake()
    {
        if (summonSpawner == null)
        {
            summonSpawner = GetComponent<PlayerSummonSpawner>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(summonKey) && summonSpawner != null)
        {
            summonSpawner.TrySpawnSummonWithEnergy(false, summonSpawner.GetSummonEnergyBaseCost(false));
        }
    }
}
