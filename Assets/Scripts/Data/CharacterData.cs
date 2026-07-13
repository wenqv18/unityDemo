using UnityEngine;

/// <summary>
/// Static character configuration used by players, summons, and enemies.
/// Runtime combat state should be stored on scene instances instead of modifying this asset.
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Game Data/Character Data")]
public sealed class CharacterData : ScriptableObject
{
    [SerializeField] private int id;
    [SerializeField] private string displayName;
    [SerializeField] private GameObject prefab;
    [SerializeField] private int maxHealth;
    [SerializeField] private int attackDamage;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int defense;

    public int Id => id;
    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int AttackDamage => attackDamage;
    public float AttackSpeed => attackSpeed;
    public float MoveSpeed => moveSpeed;
    public int Defense => defense;
}
