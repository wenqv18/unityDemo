using UnityEngine;

/// <summary>
/// Handles the standalone settings panel in MainMenuScene.
/// Esc closes settings and returns to the main menu root.
/// </summary>
public sealed class MainMenuSettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (Input.GetKeyDown(closeKey))
        {
            CloseSettings();
        }
    }

    public void OpenSettings()
    {
        ResolveReferences();
        SetActiveIfAssigned(mainMenuRoot, false);
        gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        ResolveReferences();
        SetActiveIfAssigned(mainMenuRoot, true);
        gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (mainMenuRoot == null)
        {
            GameObject found = GameObject.Find("Canvas");
            if (found == null)
            {
                Transform[] allTransforms = FindObjectsOfType<Transform>(true);
                for (int i = 0; i < allTransforms.Length; i++)
                {
                    if (allTransforms[i].name == "Canvas")
                    {
                        found = allTransforms[i].gameObject;
                        break;
                    }
                }
            }

            mainMenuRoot = found;
        }
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
