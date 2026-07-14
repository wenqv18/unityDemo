using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simple scene-local back button for LevelSelectScene. It is intentionally
/// independent from the runtime UI manager.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class LevelSelectBackButton : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        BindClick();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        BindClick();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(LoadMainMenu);
        }
    }

    private void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void BindClick()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(LoadMainMenu);
        button.onClick.AddListener(LoadMainMenu);
    }
}
