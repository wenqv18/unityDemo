using UnityEngine;

/// <summary>
/// Rolls loot when this enemy dies and spawns a pickup at the death position.
/// </summary>
[RequireComponent(typeof(CharacterRuntimeStats))]
public sealed class LootDropper : MonoBehaviour
{
    [SerializeField] private CharacterRuntimeStats stats;
    [SerializeField] private CombatIdentity identity;
    [SerializeField] private WorldDropItem dropPrefab;
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.2f;
    [SerializeField] private int[] itemIds = { 0, 1 };
    [SerializeField] private int amount = 1;
    [SerializeField] private bool onlyEnemyFaction = true;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.2f, 0f);

    private bool rolled;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        rolled = false;
        if (stats != null)
        {
            stats.Died += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.Died -= HandleDied;
        }
    }

    private void HandleDied(CharacterRuntimeStats deadStats)
    {
        if (rolled || dropPrefab == null || itemIds == null || itemIds.Length == 0)
        {
            return;
        }

        rolled = true;
        if (onlyEnemyFaction && identity != null && identity.Faction != CombatFaction.Enemy)
        {
            return;
        }

        if (Random.value > dropChance)
        {
            return;
        }

        int itemId = itemIds[Random.Range(0, itemIds.Length)];
        WorldDropItem drop = Instantiate(dropPrefab, transform.position + spawnOffset, Quaternion.identity);
        drop.Initialize(itemId, Mathf.Max(1, amount));
    }

    private void ResolveReferences()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterRuntimeStats>();
        }

        if (identity == null)
        {
            identity = GetComponent<CombatIdentity>();
        }
    }
}
