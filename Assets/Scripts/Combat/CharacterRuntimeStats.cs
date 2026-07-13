using System;
using UnityEngine;

/// <summary>
/// Runtime character values copied from CharacterData for a scene instance or prefab instance.
/// Damage, healing, buffs, and UI should read or change this component instead of editing CharacterData assets.
/// </summary>
public sealed class CharacterRuntimeStats : MonoBehaviour
{
    private const float MoveSpeedMetersPerStatPoint = 0.2f;

    [SerializeField] private CharacterData characterData;
    [SerializeField] private bool initializeOnAwake = true;
    [SerializeField] private int maxHealth;
    [SerializeField] private int currentHealth;
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int defense;

    private bool deathNotified;

    public CharacterData Data => characterData;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int AttackDamage => attackDamage;
    public float AttackSpeed => attackSpeed;
    public float MoveSpeed => GetMoveSpeedMetersPerSecond();
    public float MoveSpeedStat => moveSpeed;
    public int Defense => defense;
    public bool IsDead => currentHealth <= 0;

    public event Action<CharacterRuntimeStats> HealthChanged;
    public event Action<CharacterRuntimeStats> Died;

    private void Awake()
    {
        if (initializeOnAwake)
        {
            ApplyData(true);
        }
    }

    /// <summary>
    /// Assigns a data asset and copies its values into this runtime instance.
    /// </summary>
    public void SetCharacterData(CharacterData data, bool resetCurrentHealth = true)
    {
        characterData = data;
        ApplyData(resetCurrentHealth);
    }

    /// <summary>
    /// Copies static data into visible runtime fields. Keep current health when applying buffs if needed.
    /// </summary>
public void ApplyData(bool resetCurrentHealth = true)
    {
        if (characterData == null)
        {
            return;
        }

        maxHealth = characterData.MaxHealth;
        attackDamage = characterData.AttackDamage;
        attackSpeed = characterData.AttackSpeed;
        moveSpeed = characterData.MoveSpeed;
        defense = characterData.Defense;

        currentHealth = resetCurrentHealth ? maxHealth : Mathf.Clamp(currentHealth, 0, maxHealth);
        deathNotified = IsDead;
        HealthChanged?.Invoke(this);
    }

    /// <summary>
    /// Applies damage after defense reduction and returns the final damage dealt.
    /// </summary>
public int TakeDamage(int rawDamage)
    {
        if (IsDead)
        {
            return 0;
        }

        int finalDamage = Mathf.Max(0, rawDamage - defense);
        if (finalDamage <= 0)
        {
            return 0;
        }

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        HealthChanged?.Invoke(this);
        NotifyDeathIfNeeded();
        return finalDamage;
    }

public int Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return 0;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        int healedAmount = currentHealth - previousHealth;
        if (healedAmount > 0)
        {
            HealthChanged?.Invoke(this);
        }

        return healedAmount;
    }

public void ResetHealth()
    {
        currentHealth = maxHealth;
        deathNotified = IsDead;
        HealthChanged?.Invoke(this);
    }

    /// <summary>
    /// Scales this spawned instance without changing the shared CharacterData asset.
    /// Used by drawing summons whose strength depends on drawing energy cost.
    /// </summary>
public void ApplyInstanceMultiplier(float multiplier)
    {
        float safeMultiplier = Mathf.Max(0.01f, multiplier);
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth * safeMultiplier));
        currentHealth = maxHealth;
        attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * safeMultiplier));
        moveSpeed = Mathf.Max(0f, moveSpeed * safeMultiplier);
        attackSpeed = Mathf.Max(0f, attackSpeed / safeMultiplier);
        deathNotified = IsDead;
        HealthChanged?.Invoke(this);
    }

    /// <summary>
    /// Converts the configured Move Speed stat into world meters per second.
    /// Move Speed 10 equals 2 meters per second.
    /// </summary>
    public float GetMoveSpeedMetersPerSecond(float statBonus = 0f)
    {
        return Mathf.Max(0f, moveSpeed + statBonus) * MoveSpeedMetersPerStatPoint;
    }

    private void NotifyDeathIfNeeded()
    {
        if (!IsDead || deathNotified)
        {
            return;
        }

        deathNotified = true;
        Died?.Invoke(this);
    }
}
