using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns fixed level enemies from EnemySpawnPoint markers and reports victory when all registered enemies die.
/// </summary>
public sealed class LevelEnemyPlacement : MonoBehaviour
{
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool autoFindSpawnPoints = true;
    [SerializeField] private bool registerExistingSceneEnemies = true;
    [SerializeField] private bool showWinWhenAllEnemiesDefeated = true;
    [SerializeField] private Transform spawnedEnemyParent;
    [SerializeField] private EnemySpawnPoint[] spawnPoints;
    [SerializeField] private UnityEvent onAllEnemiesDefeated;

    private readonly List<CharacterRuntimeStats> livingEnemies = new List<CharacterRuntimeStats>();
    private bool spawned;
    private bool completed;
    private int registeredEnemyCount;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnAndRegisterEnemies();
        }
    }

    public void SpawnAndRegisterEnemies()
    {
        if (spawned)
        {
            return;
        }

        spawned = true;
        completed = false;
        livingEnemies.Clear();
        registeredEnemyCount = 0;

        if (autoFindSpawnPoints || spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = FindObjectsOfType<EnemySpawnPoint>(true);
        }

        SpawnFromPoints();

        if (registerExistingSceneEnemies)
        {
            RegisterExistingEnemies();
        }

        CheckAllEnemiesDefeated();
    }

    private void SpawnFromPoints()
    {
        if (spawnPoints == null)
        {
            return;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            EnemySpawnPoint point = spawnPoints[i];
            if (point == null || !point.isActiveAndEnabled || !point.SpawnOnLevelStart)
            {
                continue;
            }

            if (point.TrySpawn(spawnedEnemyParent, out GameObject enemy))
            {
                RegisterEnemy(enemy);
            }
        }
    }

    private void RegisterExistingEnemies()
    {
        CombatIdentity[] identities = FindObjectsOfType<CombatIdentity>(true);
        for (int i = 0; i < identities.Length; i++)
        {
            CombatIdentity identity = identities[i];
            if (identity == null || identity.Faction != CombatFaction.Enemy)
            {
                continue;
            }

            RegisterEnemy(identity.gameObject);
        }
    }

    private void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        CombatIdentity identity = enemy.GetComponent<CombatIdentity>();
        if (identity == null || identity.Faction != CombatFaction.Enemy)
        {
            return;
        }

        CharacterRuntimeStats stats = enemy.GetComponent<CharacterRuntimeStats>();
        if (stats == null || stats.IsDead || livingEnemies.Contains(stats))
        {
            return;
        }

        livingEnemies.Add(stats);
        registeredEnemyCount++;
        stats.Died += HandleEnemyDied;
    }

    private void HandleEnemyDied(CharacterRuntimeStats stats)
    {
        if (stats != null)
        {
            stats.Died -= HandleEnemyDied;
            livingEnemies.Remove(stats);
        }

        CheckAllEnemiesDefeated();
    }

    private void CheckAllEnemiesDefeated()
    {
        if (completed || registeredEnemyCount <= 0 || livingEnemies.Count > 0)
        {
            return;
        }

        completed = true;
        onAllEnemiesDefeated?.Invoke();

        if (showWinWhenAllEnemiesDefeated)
        {
            RuntimeUIManager uiManager = FindObjectOfType<RuntimeUIManager>(true);
            if (uiManager != null)
            {
                uiManager.ShowWin();
            }
        }
    }
}
