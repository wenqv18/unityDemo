using UnityEngine;

/// <summary>
/// Temporary summon input bridge. Later drawing recognition can call the same spawner directly.
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
            summonSpawner.TrySpawnSummon();
        }
    }
}
