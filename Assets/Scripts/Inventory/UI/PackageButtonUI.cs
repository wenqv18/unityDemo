using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MVC View/Controller coordinator: builds the inventory UI from DataManager data.
/// </summary>
public sealed class PackageButtonUI : MonoBehaviour
{
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private PackageDetail detailPanel;
    [SerializeField] private Button useButton;
    [SerializeField] private Button closeButton;

    public string ChooseUid { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        AddListeners();
    }

private void OnEnable()
    {
        ResolveReferences();
        AddListeners();
        DataManager.Instance.InventoryChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.InventoryChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        EnsureSelection();
        RefreshScroll();
        RefreshDetail();
    }

    public void SelectItem(string uid)
    {
        if (string.IsNullOrEmpty(uid) || ChooseUid == uid)
        {
            return;
        }

        ChooseUid = uid;
        RefreshScroll();
        RefreshDetail();
    }

public void OnClickUse()
    {
        PackageLocalData.PackageLocalItem item = DataManager.Instance.GetPackageLocalItemByUid(ChooseUid);
        if (!DataManager.Instance.IsVisibleLocalItem(item))
        {
            return;
        }

        if (!PackageItemUseService.TryUse(item))
        {
            return;
        }

        DataManager.Instance.ChangeItemNumber(item.uid, -1);
        RefreshUI();
    }

public void OnClickClose()
    {
        if (RuntimeUIManager.HasInstance)
        {
            RuntimeUIManager.Instance.ShowGameplay();
            return;
        }

        InventoryToggleController toggle = GetComponent<InventoryToggleController>();
        if (toggle != null)
        {
            toggle.CloseInventory();
            return;
        }

        gameObject.SetActive(false);
        Time.timeScale = 1f;
    }

private void RefreshScroll()
    {
        if (contentRoot == null || cellPrefab == null)
        {
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = contentRoot.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        List<PackageLocalData.PackageLocalItem> data = DataManager.Instance.GetSortPackageLocalData();
        for (int i = 0; i < data.Count; i++)
        {
            PackageLocalData.PackageLocalItem item = data[i];
            if (!DataManager.Instance.IsVisibleLocalItem(item))
            {
                continue;
            }

            GameObject cellObject;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                cellObject = UnityEditor.PrefabUtility.InstantiatePrefab(cellPrefab, contentRoot) as GameObject;
                if (cellObject != null)
                {
                    RectTransform cellTransform = cellObject.GetComponent<RectTransform>();
                    if (cellTransform != null)
                    {
                        cellTransform.localScale = Vector3.one;
                        cellTransform.anchoredPosition3D = Vector3.zero;
                    }
                }
            }
            else
#endif
            {
                cellObject = Instantiate(cellPrefab, contentRoot, false);
            }

            if (cellObject == null)
            {
                continue;
            }

            cellObject.name = "Thing_" + item.uid;
            PackageCell cell = cellObject.GetComponent<PackageCell>();
            if (cell == null)
            {
                cell = cellObject.AddComponent<PackageCell>();
            }

            cell.Refresh(item, this);
        }
    }

    private void RefreshDetail()
    {
        if (detailPanel == null)
        {
            return;
        }

        PackageLocalData.PackageLocalItem item = DataManager.Instance.GetPackageLocalItemByUid(ChooseUid);
        if (DataManager.Instance.IsVisibleLocalItem(item))
        {
            detailPanel.Refresh(item);
            return;
        }

        detailPanel.Clear();
    }

    private void EnsureSelection()
    {
        PackageLocalData.PackageLocalItem current = DataManager.Instance.GetPackageLocalItemByUid(ChooseUid);
        if (DataManager.Instance.IsVisibleLocalItem(current))
        {
            return;
        }

        PackageLocalData.PackageLocalItem first = DataManager.Instance.GetFirstUsableLocalItem();
        ChooseUid = first != null ? first.uid : null;
    }

private void ResolveReferences()
    {
        if (contentRoot == null)
        {
            Transform content = transform.Find("Center/CenterLeft/Scroll View/Viewport/Content");
            contentRoot = content != null ? content.GetComponent<RectTransform>() : null;
        }

        if (detailPanel == null)
        {
            Transform detail = transform.Find("Center/CenterRight");
            if (detail != null)
            {
                detailPanel = detail.GetComponent<PackageDetail>();
                if (detailPanel == null)
                {
                    detailPanel = detail.gameObject.AddComponent<PackageDetail>();
                }
            }
        }

        if (useButton == null)
        {
            Transform use = transform.Find("Center/CenterRight/Center/Button");
            useButton = use != null ? use.GetComponent<Button>() : null;
        }

        if (closeButton == null)
        {
            Transform close = transform.Find("Top/Image") ?? transform.Find("Top/image");
            closeButton = close != null ? close.GetComponent<Button>() : null;
        }
    }

    private void AddListeners()
    {
        if (useButton != null)
        {
            useButton.onClick.RemoveListener(OnClickUse);
            useButton.onClick.AddListener(OnClickUse);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnClickClose);
            closeButton.onClick.AddListener(OnClickClose);
        }
    }
}
