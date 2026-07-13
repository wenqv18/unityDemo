using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the number of defeated enemies in the current level.
/// The count resets naturally when the scene is reloaded.
/// </summary>
public sealed class EnemyKillCounterUI : MonoBehaviour
{
    [SerializeField] private Text legacyText;
    [SerializeField] private TextMeshProUGUI tmpText;
    [SerializeField] private float refreshInterval = 0.5f;

    private readonly HashSet<CharacterRuntimeStats> registeredEnemies = new HashSet<CharacterRuntimeStats>();
    private int killCount;
    private float nextRefreshTime;

    private void Awake()
    {
        ResolveTextReferences();
        SetKillCount(0);
    }

    private void OnEnable()
    {
        RegisterExistingEnemies();
    }

    private void OnDisable()
    {
        foreach (CharacterRuntimeStats enemy in registeredEnemies)
        {
            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDied;
            }
        }

        registeredEnemies.Clear();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        RegisterExistingEnemies();
    }

    private void ResolveTextReferences()
    {
        if (legacyText == null)
        {
            legacyText = GetComponent<Text>();
        }

        if (tmpText == null)
        {
            tmpText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void RegisterExistingEnemies()
    {
        CombatIdentity[] identities = FindObjectsOfType<CombatIdentity>(true);
        foreach (CombatIdentity identity in identities)
        {
            if (identity == null || identity.Faction != CombatFaction.Enemy)
            {
                continue;
            }

            CharacterRuntimeStats stats = identity.GetComponent<CharacterRuntimeStats>();
            if (stats == null || registeredEnemies.Contains(stats))
            {
                continue;
            }

            registeredEnemies.Add(stats);
            stats.Died += HandleEnemyDied;
        }
    }

    private void HandleEnemyDied(CharacterRuntimeStats stats)
    {
        if (stats != null)
        {
            stats.Died -= HandleEnemyDied;
            registeredEnemies.Remove(stats);
        }

        SetKillCount(killCount + 1);
    }

    private void SetKillCount(int value)
    {
        killCount = Mathf.Max(0, value);
        string text = killCount.ToString();

        if (legacyText != null)
        {
            legacyText.text = text;
        }

        if (tmpText != null)
        {
            tmpText.text = text;
        }
    }
}
