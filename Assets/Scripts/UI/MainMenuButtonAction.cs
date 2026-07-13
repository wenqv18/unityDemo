using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles main menu button clicks by reading the button GameObject name.
/// Attach this script to the Start and Leave buttons.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class MainMenuButtonAction : MonoBehaviour
{
    private const string LevelSelectSceneName = "LevelSelectScene";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    /// <summary>
    /// Start opens the level select scene; Leave exits the game.
    /// The object-name check keeps the inspector setup simple for early UI iteration.
    /// </summary>
    private void HandleClick()
    {
        string normalizedName = gameObject.name.Trim().ToLowerInvariant();

        if (normalizedName.Contains("start"))
        {
            SceneManager.LoadScene(LevelSelectSceneName);
            return;
        }

        if (normalizedName.Contains("leave") || normalizedName.Contains("quit") || normalizedName.Contains("exit"))
        {
            QuitGame();
            return;
        }

        Debug.LogWarning($"MainMenuButtonAction has no action mapped for '{gameObject.name}'.", this);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
