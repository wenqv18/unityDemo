using UnityEngine;

/// <summary>
/// Plays a visual effect when this character takes damage. It listens to runtime health
/// changes so damage logic stays separate from visual presentation.
/// </summary>
public sealed class DamageVfxEmitter : MonoBehaviour
{
    [SerializeField] private CharacterRuntimeStats stats;
    [SerializeField] private GameObject damageVfxPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private string spawnPointName = "Center";
    [SerializeField] private float fallbackLifetime = 2f;
    [SerializeField] private bool parentToSpawnPoint;

    private int previousHealth;
    private GameObject activeVfx;

    private void Awake()
    {
        ResolveReferences();
        previousHealth = stats != null ? stats.CurrentHealth : 0;
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (stats == null)
        {
            return;
        }

        previousHealth = stats.CurrentHealth;
        stats.HealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.HealthChanged -= HandleHealthChanged;
        }
    }

    private void HandleHealthChanged(CharacterRuntimeStats changedStats)
    {
        if (changedStats == null)
        {
            return;
        }

        bool tookDamage = changedStats.CurrentHealth < previousHealth;
        previousHealth = changedStats.CurrentHealth;
        if (!tookDamage || damageVfxPrefab == null || activeVfx != null)
        {
            return;
        }

        PlayDamageVfx();
    }

    private void PlayDamageVfx()
    {
        Transform point = ResolveSpawnPoint();
        Vector3 position = point != null ? point.position : transform.position;
        Quaternion rotation = point != null ? point.rotation : transform.rotation;
        Transform parent = parentToSpawnPoint ? point : null;

        activeVfx = Instantiate(damageVfxPrefab, position, rotation, parent);
        Destroy(activeVfx, GetLifetime(activeVfx));
        StartCoroutine(ClearActiveVfxWhenFinished(activeVfx));
    }

    private System.Collections.IEnumerator ClearActiveVfxWhenFinished(GameObject instance)
    {
        while (instance != null)
        {
            yield return null;
        }

        if (activeVfx == instance)
        {
            activeVfx = null;
        }
        else if (activeVfx == null)
        {
            activeVfx = null;
        }
    }

    private float GetLifetime(GameObject instance)
    {
        float lifetime = Mathf.Max(0.1f, fallbackLifetime);
        if (instance == null)
        {
            return lifetime;
        }

        ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem.MainModule main = systems[i].main;
            float duration = main.duration;
            float startLifetime = main.startLifetime.constantMax;
            lifetime = Mathf.Max(lifetime, duration + startLifetime);
        }

        return lifetime;
    }

    private Transform ResolveSpawnPoint()
    {
        if (spawnPoint != null)
        {
            return spawnPoint;
        }

        if (!string.IsNullOrEmpty(spawnPointName))
        {
            spawnPoint = transform.Find(spawnPointName);
        }

        return spawnPoint;
    }

    private void ResolveReferences()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterRuntimeStats>();
        }

        if (damageVfxPrefab == null)
        {
            damageVfxPrefab = Resources.Load<GameObject>("Particleeffects/Prefabs/BloodSplatter");
        }

        ResolveSpawnPoint();
    }
}
