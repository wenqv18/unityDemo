using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the shared gameplay UI scene. It switches between HUD, pause, drawing, inventory,
/// death, and win screens while the active gameplay level remains loaded.
/// </summary>
public sealed class RuntimeUIManager : MonoBehaviour
{
    private enum UIState
    {
        Gameplay,
        Settings,
        Drawing,
        Package,
        Death,
        Win
    }

    public static RuntimeUIManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [Header("UI Roots")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject drawingUI;
    [SerializeField] private GameObject packageUI;
    [SerializeField] private GameObject deathUI;
    [SerializeField] private GameObject winUI;

    [Header("Scene Flow")]
    [SerializeField] private string levelSelectSceneName = "LevelSelectScene";

    [Header("Input")]
    [SerializeField] private KeyCode drawingKey = KeyCode.E;
    [SerializeField] private KeyCode packageKey = KeyCode.B;

    private UIState currentState = UIState.Gameplay;
    private CharacterRuntimeStats boundPlayerStats;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        AddButtonListeners();
        ShowGameplay();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindToCurrentLevel();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindPlayerDeath();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (currentState == UIState.Death || currentState == UIState.Win)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == UIState.Gameplay)
            {
                OpenSettings();
            }
            else
            {
                ShowGameplay();
            }
        }

        if (currentState == UIState.Gameplay && Input.GetKeyDown(drawingKey))
        {
            OpenDrawing();
        }

        if ((currentState == UIState.Gameplay || currentState == UIState.Package) && Input.GetKeyDown(packageKey))
        {
            TogglePackage();
        }
    }

    public void BindToCurrentLevel()
    {
        ResolveReferences();
        BindPlayerDeath();
        BindPlayerHealthBar();
        BindPlayerEnergyUI();
        DisableLegacyLevelControllersAtRuntime();
    }

    public void ShowGameplay()
    {
        currentState = UIState.Gameplay;
        Time.timeScale = 1f;
        SetOnly(gameUI);
    }

    public void OpenSettings()
    {
        currentState = UIState.Settings;
        Time.timeScale = 0f;
        SetOnly(settingsUI);
    }

    public void OpenDrawing()
    {
        currentState = UIState.Drawing;
        Time.timeScale = 0f;
        SetOnly(drawingUI);
    }

    public void ConfirmDrawingAndResume()
    {
        ShowGameplay();
    }

    public void TogglePackage()
    {
        if (currentState == UIState.Package)
        {
            ShowGameplay();
            return;
        }

        OpenPackage();
    }

    public void OpenPackage()
    {
        currentState = UIState.Package;
        Time.timeScale = 0f;
        SetOnly(packageUI);

        if (packageUI != null && !packageUI.activeSelf)
        {
            packageUI.SetActive(true);
        }

        PackageButtonUI packageButtonUI = packageUI != null ? packageUI.GetComponent<PackageButtonUI>() : null;
        if (packageButtonUI != null)
        {
            packageButtonUI.RefreshUI();
        }
    }

    public void ShowDeath()
    {
        currentState = UIState.Death;
        Time.timeScale = 0f;
        SetOnly(deathUI);
    }

    public void ShowWin()
    {
        currentState = UIState.Win;
        Time.timeScale = 0f;
        SetOnly(winUI);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void LeaveToLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;

        string currentLevelSceneName = GetCurrentLevelSceneName();
        string nextSceneName = GetNextLevelSceneName(currentLevelSceneName);
        if (!string.IsNullOrEmpty(nextSceneName) && CanLoadScene(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        SceneManager.LoadScene(levelSelectSceneName);
    }

    private string GetCurrentLevelSceneName()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsNumberedLevelScene(activeScene.name))
        {
            return activeScene.name;
        }

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.IsValid() && scene.isLoaded && scene != gameObject.scene && IsNumberedLevelScene(scene.name))
            {
                return scene.name;
            }
        }

        return null;
    }

    private static string GetNextLevelSceneName(string currentSceneName)
    {
        if (string.IsNullOrEmpty(currentSceneName))
        {
            return null;
        }

        int digitStart = currentSceneName.Length;
        while (digitStart > 0 && char.IsDigit(currentSceneName[digitStart - 1]))
        {
            digitStart--;
        }

        if (digitStart == currentSceneName.Length)
        {
            return null;
        }

        string prefix = currentSceneName.Substring(0, digitStart);
        string digits = currentSceneName.Substring(digitStart);
        int levelNumber;
        if (!int.TryParse(digits, out levelNumber))
        {
            return null;
        }

        return prefix + (levelNumber + 1).ToString(new string('0', digits.Length));
    }

    private static bool IsNumberedLevelScene(string sceneName)
    {
        return !string.IsNullOrEmpty(sceneName)
            && sceneName.StartsWith("Level_")
            && char.IsDigit(sceneName[sceneName.Length - 1]);
    }

    private static bool CanLoadScene(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string buildSceneName = Path.GetFileNameWithoutExtension(scenePath);
            if (buildSceneName == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != gameObject.scene.name)
        {
            BindToCurrentLevel();
            ShowGameplay();
        }
    }

    private void ResolveReferences()
    {
        gameUI = ResolveRoot(gameUI, "GameUI");
        settingsUI = ResolveRoot(settingsUI, "Settings");
        drawingUI = ResolveRoot(drawingUI, "Drawing");
        packageUI = ResolveRoot(packageUI, "Package");
        deathUI = ResolveRoot(deathUI, "DeathUI");
        winUI = ResolveRoot(winUI, "WinUI");
    }

    private GameObject ResolveRoot(GameObject current, string rootName)
    {
        if (current != null)
        {
            return current;
        }

        Scene ownerScene = gameObject.scene;
        GameObject[] roots = ownerScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == rootName)
            {
                return roots[i];
            }
        }

        return null;
    }

    private void AddButtonListeners()
    {
        AddListener(settingsUI, "Continue", ShowGameplay);
        AddListener(settingsUI, "Leave", LeaveToLevelSelect);
        AddListener(deathUI, "Restart", RestartLevel);
        AddListener(deathUI, "Leave", LeaveToLevelSelect);
        AddListener(winUI, "Next", GoToNextLevel);
        AddListener(winUI, "Leave", LeaveToLevelSelect);
        AddListener(drawingUI, "Button", ConfirmDrawingAndResume);
        AddListener(gameUI, "Setting", OpenSettings);
        AddListener(gameUI, "Package", OpenPackage);
    }

    private void AddListener(GameObject root, string buttonObjectName, UnityEngine.Events.UnityAction action)
    {
        if (root == null)
        {
            return;
        }

        Transform target = FindChildRecursive(root.transform, buttonObjectName);
        if (target == null)
        {
            return;
        }

        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void SetOnly(GameObject visibleRoot)
    {
        SetActiveIfAssigned(gameUI, visibleRoot == gameUI);
        SetActiveIfAssigned(settingsUI, visibleRoot == settingsUI);
        SetActiveIfAssigned(drawingUI, visibleRoot == drawingUI);
        SetActiveIfAssigned(packageUI, visibleRoot == packageUI);
        SetActiveIfAssigned(deathUI, visibleRoot == deathUI);
        SetActiveIfAssigned(winUI, visibleRoot == winUI);
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
        {
            target.SetActive(active);
        }
    }

    private void BindPlayerDeath()
    {
        CharacterRuntimeStats playerStats = FindPlayerStats();
        if (boundPlayerStats == playerStats)
        {
            return;
        }

        UnbindPlayerDeath();
        boundPlayerStats = playerStats;
        if (boundPlayerStats != null)
        {
            boundPlayerStats.Died += HandlePlayerDeath;
        }
    }

    private void UnbindPlayerDeath()
    {
        if (boundPlayerStats != null)
        {
            boundPlayerStats.Died -= HandlePlayerDeath;
            boundPlayerStats = null;
        }
    }

    private void HandlePlayerDeath(CharacterRuntimeStats stats)
    {
        ShowDeath();
    }

    private CharacterRuntimeStats FindPlayerStats()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            return null;
        }

        return player.GetComponent<CharacterRuntimeStats>();
    }

private void BindPlayerHealthBar()
    {
        GameObject player = GameObject.Find("Player");
        CharacterRuntimeStats playerStats = player != null ? player.GetComponent<CharacterRuntimeStats>() : null;
        PlayerHealthBarUI healthBar = player != null ? player.GetComponent<PlayerHealthBarUI>() : null;
        if (healthBar == null || playerStats == null)
        {
            return;
        }

        healthBar.character = playerStats;
        if (healthBar.Health == null && gameUI != null)
        {
            Transform health = FindChildRecursive(gameUI.transform, "Health");
            if (health != null)
            {
                healthBar.Health = health.gameObject;
            }
        }
    }

    private void BindPlayerEnergyUI()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null || gameUI == null)
        {
            return;
        }

        PlayerEnergy energy = player.GetComponent<PlayerEnergy>();
        if (energy == null)
        {
            energy = player.AddComponent<PlayerEnergy>();
        }

        Transform energyNumber = FindChildRecursive(gameUI.transform, "EnergyNumber");
        if (energyNumber == null)
        {
            return;
        }

        PlayerEnergyUI energyUI = energyNumber.GetComponent<PlayerEnergyUI>();
        if (energyUI == null)
        {
            energyUI = energyNumber.gameObject.AddComponent<PlayerEnergyUI>();
        }

        energyUI.Bind(energy);
    }

    private void DisableLegacyLevelControllersAtRuntime()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PauseMenuController[] pauseControllers = FindObjectsOfType<PauseMenuController>(true);
        for (int i = 0; i < pauseControllers.Length; i++)
        {
            pauseControllers[i].enabled = false;
        }

        DrawingMenuController[] drawingControllers = FindObjectsOfType<DrawingMenuController>(true);
        for (int i = 0; i < drawingControllers.Length; i++)
        {
            drawingControllers[i].enabled = false;
        }

        PlayerDeathUIController[] deathControllers = FindObjectsOfType<PlayerDeathUIController>(true);
        for (int i = 0; i < deathControllers.Length; i++)
        {
            deathControllers[i].enabled = false;
        }

        InventoryToggleController[] inventoryControllers = FindObjectsOfType<InventoryToggleController>(true);
        for (int i = 0; i < inventoryControllers.Length; i++)
        {
            inventoryControllers[i].enabled = false;
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindChildRecursive(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
