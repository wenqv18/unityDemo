using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens the standalone MainMenuScene settings panel from a Button.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class MainMenuOpenSettingsButton : MonoBehaviour
{
    [SerializeField] private MainMenuSettingsPanel settingsPanel;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        ResolveReferences();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        ResolveReferences();
        button.onClick.RemoveListener(OpenSettings);
        button.onClick.AddListener(OpenSettings);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OpenSettings);
        }
    }

    private void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.OpenSettings();
        }
    }

    private void ResolveReferences()
    {
        if (settingsPanel != null)
        {
            return;
        }

        MainMenuSettingsPanel[] panels = FindObjectsOfType<MainMenuSettingsPanel>(true);
        if (panels.Length > 0)
        {
            settingsPanel = panels[0];
        }
    }
}
