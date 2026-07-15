using UnityEngine;

/// <summary>
/// Spawns the configured summon prefab around the player when the summon key is pressed.
/// </summary>
public sealed class PlayerSummonSpawner : MonoBehaviour
{
    public enum SummonKind
    {
        Melee,
        Ranged,
        Firemage,
        Berserker
    }

    private const string DefaultSummonVfxResourcePath = "Particleeffects/Prefabs/Portal";
    private const float MinimumSummonMultiplier = 0.5f;
    private const float MaximumSummonMultiplier = 2f;

    [SerializeField] private GameObject summonPrefab;
    [SerializeField] private GameObject rangedSummonPrefab;
    [SerializeField] private GameObject firemageSummonPrefab;
    [SerializeField] private GameObject berserkerSummonPrefab;
    [SerializeField] private Transform followTarget;
    [SerializeField] private float spawnMinDistance = 3f;
    [SerializeField] private float spawnMaxDistance = 8f;
    [SerializeField] private float berserkerSpawnYOffset = 1f;
    [SerializeField] private int maxSummons = 5;
    [Header("Summon Placement")]
    [SerializeField] private float spawnSearchStepDistance = 0.75f;
    [SerializeField] private int spawnSearchDirectionAttempts = 8;
    [SerializeField] private float spawnGroundRaycastHeight = 8f;
    [SerializeField] private float spawnGroundRaycastDistance = 16f;
    [SerializeField] private float spawnClearRadius = 0.8f;
    [SerializeField] private float spawnWallCheckHeight = 0.9f;
    [Header("Summon Energy")]
    [SerializeField] private SummonEnergyData summonEnergyData;
    [SerializeField] private SummonEnergyData rangedSummonEnergyData;
    [SerializeField] private SummonEnergyData firemageSummonEnergyData;
    [SerializeField] private SummonEnergyData berserkerSummonEnergyData;
    [Header("Drawing Summon Cooldown")]
    [SerializeField] private float drawingSummonCooldown = 3f;
    [Header("Summon VFX")]
    [SerializeField] private GameObject summonVfxPrefab;
    [SerializeField] private string summonVfxAnchorName = "Bottom";
    [SerializeField] private float summonVfxFallbackLifetime = 3f;

    private int activeSummonCount;
    private int nextSummonIndex;
    private float drawingSummonCooldownEndsAt;

    public float DrawingSummonCooldownDuration => Mathf.Max(0f, drawingSummonCooldown);
    public float DrawingSummonCooldownRemaining => Mathf.Max(0f, drawingSummonCooldownEndsAt - Time.time);
    public bool IsDrawingSummonCoolingDown => DrawingSummonCooldownRemaining > 0f;
    public float DrawingSummonCooldownFill => DrawingSummonCooldownDuration <= 0f ? 0f : DrawingSummonCooldownRemaining / DrawingSummonCooldownDuration;

    private void Awake()
    {
        if (followTarget == null)
        {
            followTarget = transform;
        }

        if (summonVfxAnchorName == "Center")
        {
            summonVfxAnchorName = "Bottom";
        }

        if (summonVfxPrefab == null)
        {
            summonVfxPrefab = Resources.Load<GameObject>(DefaultSummonVfxResourcePath);
        }
    }

    public bool TrySpawnSummon()
    {
        return TrySpawnSummon(1f);
    }

    public bool TrySpawnSummon(float statMultiplier)
    {
        return TrySpawnConfiguredSummon(SummonKind.Melee, summonPrefab, statMultiplier);
    }

    public bool TrySpawnRangedSummon()
    {
        return TrySpawnRangedSummon(1f);
    }

    public bool TrySpawnRangedSummon(float statMultiplier)
    {
        return TrySpawnConfiguredSummon(SummonKind.Ranged, rangedSummonPrefab, statMultiplier);
    }

    public bool TrySpawnFiremageSummon()
    {
        return TrySpawnFiremageSummon(1f);
    }

    public bool TrySpawnFiremageSummon(float statMultiplier)
    {
        return TrySpawnConfiguredSummon(SummonKind.Firemage, firemageSummonPrefab, statMultiplier);
    }

    public bool CanSpawnSummon(bool ranged)
    {
        return CanSpawnSummon(ranged ? SummonKind.Ranged : SummonKind.Melee);
    }

    public bool CanSpawnSummon(SummonKind kind)
    {
        GameObject prefab = GetSummonPrefab(kind);
        return prefab != null && activeSummonCount < maxSummons;
    }

    public int GetSummonEnergyBaseCost(bool ranged)
    {
        return GetSummonEnergyBaseCost(ranged ? SummonKind.Ranged : SummonKind.Melee);
    }

    public int GetSummonEnergyBaseCost(SummonKind kind)
    {
        SummonEnergyData data = GetSummonEnergyData(kind);
        return data != null ? data.EnergyBaseCost : 100;
    }

    public bool TrySpawnSummonWithEnergy(bool ranged, int energyCost)
    {
        return TrySpawnSummonWithEnergy(ranged, energyCost, out _, out _);
    }

    public bool TrySpawnSummonWithEnergy(bool ranged, int energyCost, out float multiplier, out int effectiveEnergyCost)
    {
        return TrySpawnSummonWithEnergy(ranged ? SummonKind.Ranged : SummonKind.Melee, energyCost, out multiplier, out effectiveEnergyCost);
    }

    public bool TrySpawnSummonWithEnergy(SummonKind kind, int energyCost, out float multiplier, out int effectiveEnergyCost)
    {
        return TrySpawnSummonWithEnergy(kind, energyCost, false, out multiplier, out effectiveEnergyCost);
    }

    public bool TrySpawnSummonWithEnergy(SummonKind kind, int energyCost, bool useDrawingCooldown, out float multiplier, out int effectiveEnergyCost)
    {
        multiplier = 1f;
        effectiveEnergyCost = 0;

        if (useDrawingCooldown && IsDrawingSummonCoolingDown)
        {
            Debug.Log("Summon failed: drawing summon is cooling down.");
            return false;
        }

        int baseEnergyCost = GetSummonEnergyBaseCost(kind);
        if (!TryCalculateSummonMultiplier(energyCost, baseEnergyCost, out multiplier, out effectiveEnergyCost))
        {
            return false;
        }

        if (!CanSpawnSummon(kind))
        {
            Debug.Log("Summon failed: summon limit reached or prefab missing.");
            return false;
        }

        PlayerEnergy playerEnergy = GetComponent<PlayerEnergy>();
        if (playerEnergy == null)
        {
            Debug.LogWarning("Summon failed: PlayerEnergy not found.");
            return false;
        }

        if (!playerEnergy.TrySpend(effectiveEnergyCost))
        {
            Debug.Log("Summon failed: not enough energy.");
            return false;
        }

        bool spawned = TrySpawnSummon(kind, multiplier);
        if (!spawned)
        {
            playerEnergy.Restore(effectiveEnergyCost);
            Debug.Log("Summon failed: energy refunded.");
            return false;
        }

        if (useDrawingCooldown)
        {
            StartDrawingSummonCooldown();
        }

        return true;
    }

    private bool TrySpawnSummon(SummonKind kind, float statMultiplier)
    {
        return TrySpawnConfiguredSummon(kind, GetSummonPrefab(kind), statMultiplier);
    }

    private GameObject GetSummonPrefab(SummonKind kind)
    {
        switch (kind)
        {
            case SummonKind.Ranged:
                return rangedSummonPrefab;
            case SummonKind.Firemage:
                return firemageSummonPrefab;
            case SummonKind.Berserker:
                return berserkerSummonPrefab;
            default:
                return summonPrefab;
        }
    }

    private SummonEnergyData GetSummonEnergyData(SummonKind kind)
    {
        switch (kind)
        {
            case SummonKind.Ranged:
                return rangedSummonEnergyData;
            case SummonKind.Firemage:
                return firemageSummonEnergyData;
            case SummonKind.Berserker:
                return berserkerSummonEnergyData;
            default:
                return summonEnergyData;
        }
    }

    private bool TrySpawnConfiguredSummon(SummonKind kind, GameObject prefab, float statMultiplier)
    {
        if (prefab == null || activeSummonCount >= maxSummons)
        {
            return false;
        }

        if (!CombatSpatialQuery.TryFindSummonPoint(
                transform,
                spawnMinDistance,
                spawnMaxDistance,
                spawnSearchStepDistance,
                spawnSearchDirectionAttempts,
                spawnClearRadius,
                spawnGroundRaycastHeight,
                spawnGroundRaycastDistance,
                spawnWallCheckHeight,
                out Vector3 spawnPosition))
        {
            Debug.Log("Summon failed: no valid ground point found.");
            return false;
        }

        float yOffset = kind == SummonKind.Berserker ? berserkerSpawnYOffset : 0f;
        spawnPosition += Vector3.up * yOffset;
        GameObject summon = Instantiate(prefab, spawnPosition, Quaternion.identity);
        summon.name = $"Summon_{nextSummonIndex++}";
        ApplySummonMultiplier(summon, statMultiplier);
        PlaySummonVfx(summon, spawnPosition);

        SummonFollowController controller = summon.GetComponent<SummonFollowController>();
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

    private static bool TryCalculateSummonMultiplier(int energyCost, int baseEnergyCost, out float multiplier, out int effectiveEnergyCost)
    {
        int safeBaseEnergyCost = Mathf.Max(1, baseEnergyCost);
        effectiveEnergyCost = Mathf.Clamp(energyCost, 0, safeBaseEnergyCost * 2);
        multiplier = effectiveEnergyCost / (float)safeBaseEnergyCost;
        if (multiplier < MinimumSummonMultiplier)
        {
            Debug.Log("Summon failed: drawing is too small.");
            return false;
        }

        multiplier = Mathf.Min(multiplier, MaximumSummonMultiplier);
        return true;
    }

    private void StartDrawingSummonCooldown()
    {
        float duration = DrawingSummonCooldownDuration;
        drawingSummonCooldownEndsAt = duration > 0f ? Time.time + duration : 0f;
    }

    private void PlaySummonVfx(GameObject summon, Vector3 fallbackPosition)
    {
        if (summonVfxPrefab == null)
        {
            return;
        }

        Transform anchor = FindChildRecursive(summon.transform, summonVfxAnchorName);
        Vector3 vfxPosition = anchor != null ? anchor.position : fallbackPosition;
        Quaternion vfxRotation = anchor != null ? anchor.rotation : Quaternion.identity;
        GameObject vfx = Instantiate(summonVfxPrefab, vfxPosition, vfxRotation);
        Destroy(vfx, GetParticleLifetime(vfx));
    }

    private float GetParticleLifetime(GameObject vfx)
    {
        ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
        float longestLifetime = summonVfxFallbackLifetime;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem.MainModule main = particleSystems[i].main;
            float startLifetime = main.startLifetime.constantMax;
            longestLifetime = Mathf.Max(longestLifetime, main.duration + startLifetime);
        }

        return longestLifetime;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindChildRecursive(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }


    private void OnSummonReleased()
    {
        activeSummonCount = Mathf.Max(0, activeSummonCount - 1);
    }
}
