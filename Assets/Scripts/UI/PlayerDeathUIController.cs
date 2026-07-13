using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows the death UI when the player dies and pauses gameplay.
/// Other in-game UI is hidden so only the death screen remains visible.
/// </summary>
[RequireComponent(typeof(CharacterRuntimeStats))]
public sealed class PlayerDeathUIController : MonoBehaviour
{
    [SerializeField] private CharacterRuntimeStats playerStats;
    [SerializeField] private GameObject deathUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private PauseMenuController pauseMenuController;
    [SerializeField] private string levelSelectSceneName = "LevelSelectScene";

    private bool deathHandled;

    private void Awake()
    {
        ResolveReferences();
        SetDeathUIVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (playerStats != null)
        {
            playerStats.Died += HandlePlayerDeath;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.Died -= HandlePlayerDeath;
        }
    }

    private void ResolveReferences()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<CharacterRuntimeStats>();
        }

        if (deathUI == null)
        {
            deathUI = GameObject.Find("DeathUI");
        }

        if (gameUI == null)
        {
            gameUI = GameObject.Find("GameUI");
        }

        if (settingsUI == null)
        {
            settingsUI = GameObject.Find("Settings");
        }

        if (pauseMenuController == null)
        {
            pauseMenuController = FindObjectOfType<PauseMenuController>(true);
        }
    }

    private void HandlePlayerDeath(CharacterRuntimeStats deadStats)
    {
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
        Time.timeScale = 0f;

        if (pauseMenuController != null)
        {
            pauseMenuController.enabled = false;
        }

        SetActiveIfAssigned(gameUI, false);
        SetActiveIfAssigned(settingsUI, false);
        SetDeathUIVisible(true);
    }

    /// <summary>
    /// Restarts the current level from the death screen.
    /// </summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    /// <summary>
    /// Leaves the current level and returns to level selection.
    /// </summary>
    public void LeaveToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void SetDeathUIVisible(bool visible)
    {
        SetActiveIfAssigned(deathUI, visible);
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
