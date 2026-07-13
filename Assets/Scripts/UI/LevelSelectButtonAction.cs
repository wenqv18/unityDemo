using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Loads a level scene based on the trailing number in this button's GameObject name.
/// Example: a button named "Level2" opens the scene named "Level_02".
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class LevelSelectButtonAction : MonoBehaviour
{
    private static readonly Regex TrailingNumberPattern = new Regex(@"(\d+)\s*$", RegexOptions.Compiled);

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
    /// Converts the final number in the object name into the project level scene naming convention.
    /// </summary>
    private void HandleClick()
    {
        Match match = TrailingNumberPattern.Match(gameObject.name);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out int levelNumber))
        {
            Debug.LogWarning($"LevelSelectButtonAction could not find a trailing level number in '{gameObject.name}'.", this);
            return;
        }

        string sceneName = $"Level_{levelNumber:00}";
        SceneManager.LoadScene(sceneName);
    }
}
