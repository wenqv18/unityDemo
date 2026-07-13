using UnityEngine;

/// <summary>
/// MVC Controller: opens and closes the inventory panel and coordinates pause state.
/// </summary>
public sealed class InventoryToggleController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.B;
    [SerializeField] private bool pauseWhenOpen = true;

    public static bool IsInventoryOpen { get; private set; }

    private void Awake()
    {
        if (inventoryRoot == null)
        {
            inventoryRoot = gameObject;
        }
    }

    private void Start()
    {
        SetInventoryVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventory();
        }
        else if (IsInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    public void ToggleInventory()
    {
        if (IsInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

public void OpenInventory()
    {
        if (IsInventoryOpen)
        {
            return;
        }

        IsInventoryOpen = true;
        SetInventoryVisible(true);
        if (pauseWhenOpen)
        {
            ApplyPause(true);
        }
    }

    public void CloseInventory()
    {
        if (!IsInventoryOpen)
        {
            return;
        }

        IsInventoryOpen = false;
        SetInventoryVisible(false);
        if (pauseWhenOpen)
        {
            ApplyPause(false);
        }
    }



private void ApplyPause(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }

    private void SetInventoryVisible(bool visible)
    {
        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        if (IsInventoryOpen)
        {
            IsInventoryOpen = false;
            if (pauseWhenOpen)
            {
                ApplyPause(false);
            }
        }
    }
}
