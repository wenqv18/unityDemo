using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lookup table for character data assets. Uses character id as the stable key.
/// </summary>
[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Game Data/Character Database")]
public sealed class CharacterDatabase : ScriptableObject
{
    [SerializeField] private List<CharacterData> characters = new List<CharacterData>();

    private Dictionary<int, CharacterData> lookup;

    public IReadOnlyList<CharacterData> Characters => characters;

    public bool TryGetById(int id, out CharacterData data)
    {
        EnsureLookup();
        return lookup.TryGetValue(id, out data);
    }

    public CharacterData GetById(int id)
    {
        EnsureLookup();
        return lookup.TryGetValue(id, out CharacterData data) ? data : null;
    }

    private void OnValidate()
    {
        lookup = null;
    }

    private void EnsureLookup()
    {
        if (lookup != null)
        {
            return;
        }

        lookup = new Dictionary<int, CharacterData>();
        foreach (CharacterData character in characters)
        {
            if (character == null || lookup.ContainsKey(character.Id))
            {
                continue;
            }

            lookup.Add(character.Id, character);
        }
    }
}
