using UnityEngine;

/// <summary>
/// Spawns the configured summon prefab around the player when the summon key is pressed.
/// </summary>
public sealed class PlayerSummonSpawner : MonoBehaviour
{
    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private GameObject rangedSummonPrefab;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float spawnMinDistance = 3f;
    [SerializeField] private float spawnMaxDistance = 8f;
    [SerializeField] private int maxSummons = 5;

    private int activeSummonCount;
    private int nextSummonIndex;

    private void Awake()
    {
        if (followTarget == null)
        {
            followTarget = transform;
        }
    }



public bool TrySpawnSummon()
    {
        return TrySpawnSummon(1f);
    }

public bool TrySpawnSummon(float statMultiplier)
    {
        return TrySpawnConfiguredSummon(summonPrefab, statMultiplier);
    }

public bool TrySpawnRangedSummon()
    {
        return TrySpawnRangedSummon(1f);
    }

public bool TrySpawnRangedSummon(float statMultiplier)
    {
        return TrySpawnConfiguredSummon(rangedSummonPrefab, statMultiplier);
    }

public bool CanSpawnSummon(bool ranged)
    {
        GameObject prefab = ranged ? rangedSummonPrefab : summonPrefab;
        return prefab != null && activeSummonCount < maxSummons;
    }

    private bool TrySpawnConfiguredSummon(GameObject prefab, float statMultiplier)
    {
        if (prefab == null || activeSummonCount >= maxSummons)
        {
            return false;
        }

        Vector2 randomCircle = Random.insideUnitCircle;
        if (randomCircle.sqrMagnitude <= 0.01f)
        {
            randomCircle = Vector2.right;
        }

        float safeSpawnMaxDistance = Mathf.Max(spawnMinDistance, spawnMaxDistance);
        float spawnDistance = Random.Range(spawnMinDistance, safeSpawnMaxDistance);
        randomCircle = randomCircle.normalized * spawnDistance;

        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        GameObject summon = Instantiate(prefab, spawnPosition, Quaternion.identity);
        summon.name = $"Summon_{nextSummonIndex++}";
        ApplySummonMultiplier(summon, statMultiplier);

        PlayerSummonController controller = summon.GetComponent<PlayerSummonController>();
        if (controller != null)
        {
            controller.Initialize(followTarget, OnSummonReleased);
        }

        activeSummonCount++;
        return true;
    }

    private static void ApplySummonMultiplier(GameObject summon, float statMultiplier)
    {
        if (summon == null)
        {
            return;
        }

        float safeMultiplier = Mathf.Max(0.01f, statMultiplier);
        summon.transform.localScale *= safeMultiplier;

        CharacterRuntimeStats stats = summon.GetComponent<CharacterRuntimeStats>();
        if (stats != null)
        {
            stats.ApplyInstanceMultiplier(safeMultiplier);
        }
    }


    private void OnSummonReleased()
    {
        activeSummonCount = Mathf.Max(0, activeSummonCount - 1);
    }
}
