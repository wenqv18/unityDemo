using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthBarUI : MonoBehaviour
{
    public GameObject Health;
    public CharacterRuntimeStats character;

    private Image image;

    public void Start()
    {
        ResolveReferences();
    }

    public void Update()
    {
        ResolveReferences();
        if (character == null || image == null || character.MaxHealth <= 0f)
        {
            return;
        }

        image.fillAmount = Mathf.Clamp01((float)character.CurrentHealth / character.MaxHealth);
    }

    private void ResolveReferences()
    {
        
        if (character == null)
        {
            character = GetComponent<CharacterRuntimeStats>();
        }

        if (Health != null && image == null)
        {
            image = Health.GetComponent<Image>();
        }
    }
}