using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the in-level pause menu and switches between the HUD and Settings canvases.
/// Esc pauses the game, hides GameUI, and shows Settings. Continue restores gameplay.
/// </summary>
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject gameUiCanvas;
    [SerializeField] private GameObject settingsCanvas;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private string levelSelectSceneName = "LevelSelectScene";

    private bool isPaused;

private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ResumeGame);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(LeaveToLevelSelect);
        }

        ResumeGame();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ResumeGame);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(LeaveToLevelSelect);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SetCanvasStates(showGameUi: false, showSettings: true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SetCanvasStates(showGameUi: true, showSettings: false);
    }

    public void LeaveToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    private void SetCanvasStates(bool showGameUi, bool showSettings)
    {
        if (gameUiCanvas != null)
        {
            gameUiCanvas.SetActive(showGameUi);
        }

        if (settingsCanvas != null)
        {
            settingsCanvas.SetActive(showSettings);
        }
    }






public void OpenSettingsMenu()
    {
        PauseGame();
    }
}
