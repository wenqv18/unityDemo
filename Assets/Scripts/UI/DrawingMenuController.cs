using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens the drawing panel with E, pauses gameplay, and returns to gameplay through the drawing button.
/// The confirm button will later become the drawing recognition entry point.
/// </summary>
public sealed class DrawingMenuController : MonoBehaviour
{
    [SerializeField] private GameObject gameUiCanvas;
    [SerializeField] private GameObject settingsCanvas;
    [SerializeField] private GameObject deathUiCanvas;
    [SerializeField] private GameObject winUiCanvas;
    [SerializeField] private GameObject drawingCanvas;
    [SerializeField] private Button confirmButton;
    [SerializeField] private PauseMenuController pauseMenuController;

    private bool isDrawingOpen;

    private void Awake()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmDrawingAndResume);
        }

        SetActiveIfAssigned(drawingCanvas, false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(ConfirmDrawingAndResume);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isDrawingOpen)
        {
            OpenDrawingMenu();
        }
    }

    public void OpenDrawingMenu()
    {
        isDrawingOpen = true;
        Time.timeScale = 0f;

        if (pauseMenuController != null)
        {
            pauseMenuController.enabled = false;
        }

        SetActiveIfAssigned(gameUiCanvas, false);
        SetActiveIfAssigned(settingsCanvas, false);
        SetActiveIfAssigned(deathUiCanvas, false);
        SetActiveIfAssigned(winUiCanvas, false);
        SetActiveIfAssigned(drawingCanvas, true);
    }

    public void ConfirmDrawingAndResume()
    {
        isDrawingOpen = false;
        Time.timeScale = 1f;

        SetActiveIfAssigned(drawingCanvas, false);
        SetActiveIfAssigned(gameUiCanvas, true);

        if (pauseMenuController != null)
        {
            pauseMenuController.enabled = true;
        }
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }
}
