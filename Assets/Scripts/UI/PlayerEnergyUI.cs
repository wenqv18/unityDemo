using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's current energy as a plain number.
/// </summary>
public sealed class PlayerEnergyUI : MonoBehaviour
{
    [SerializeField] private PlayerEnergy targetEnergy;
    [SerializeField] private Text legacyText;
    [SerializeField] private TMP_Text tmpText;

    private bool isSubscribed;

    private void Awake()
    {
        ResolveText();
    }

    private void OnEnable()
    {
        ResolveText();
        Bind(targetEnergy);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(PlayerEnergy energy)
    {
        if (targetEnergy == energy)
        {
            Subscribe();
            Refresh();
            return;
        }

        Unsubscribe();
        targetEnergy = energy;
        Subscribe();
        Refresh();
    }

    private void HandleEnergyChanged(PlayerEnergy energy)
    {
        Refresh();
    }

    private void Refresh()
    {
        string value = targetEnergy != null ? targetEnergy.CurrentEnergy.ToString() : "0";
        if (legacyText != null)
        {
            legacyText.text = value;
        }

        if (tmpText != null)
        {
            tmpText.text = value;
        }
    }

    private void ResolveText()
    {
        if (legacyText == null)
        {
            legacyText = GetComponent<Text>();
        }

        if (tmpText == null)
        {
            tmpText = GetComponent<TMP_Text>();
        }
    }

    private void Subscribe()
    {
        if (targetEnergy == null || isSubscribed)
        {
            return;
        }

        targetEnergy.EnergyChanged += HandleEnergyChanged;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (targetEnergy == null || !isSubscribed)
        {
            return;
        }

        targetEnergy.EnergyChanged -= HandleEnergyChanged;
        isSubscribed = false;
    }
}
