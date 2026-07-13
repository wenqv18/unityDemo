using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MVC Model Facade: single access point for inventory data and static item definitions.
/// </summary>
public sealed class DataManager : MonoBehaviour
{
    private static DataManager instance;
    private PackageTable packageTable;

    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DataManager>();
            }

            if (instance == null)
            {
                GameObject go = new GameObject(nameof(DataManager));
                instance = go.AddComponent<DataManager>();
            }

            return instance;
        }
    }

    public static DataManager instanceLegacy => Instance;

    public event Action InventoryChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<PackageLocalData.PackageLocalItem> GetPackageLocalData()
    {
        List<PackageLocalData.PackageLocalItem> items = PackageLocalData.Instance.LoadPackage();
        if (items == null)
        {
            PackageLocalData.Instance.items = new List<PackageLocalData.PackageLocalItem>();
            items = PackageLocalData.Instance.items;
        }

        return items;
    }

    public List<PackageLocalData.PackageLocalItem> GetSortPackageLocalData()
    {
        List<PackageLocalData.PackageLocalItem> items = new List<PackageLocalData.PackageLocalItem>(GetPackageLocalData());
        items.Sort(new PackageItemComparer(this));
        return items;
    }

public PackageTable GetPackageTable()
    {
        if (packageTable != null)
        {
            return packageTable;
        }

#if UNITY_EDITOR
        packageTable = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageTable>("Assets/Data/Package/PackageTable.asset");
#endif

        if (packageTable == null)
        {
            packageTable = Resources.Load<PackageTable>("PackageTable");
        }

        return packageTable;
    }

    public PackageTableItem GetPackageItemById(int id)
    {
        PackageTable table = GetPackageTable();
        if (table == null || table.DataList == null)
        {
            return null;
        }

        for (int i = 0; i < table.DataList.Count; i++)
        {
            PackageTableItem item = table.DataList[i];
            if (item != null && item.id == id)
            {
                return item;
            }
        }

        return null;
    }

    public PackageLocalData.PackageLocalItem GetPackageLocalItemByUid(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return null;
        }

        List<PackageLocalData.PackageLocalItem> items = GetPackageLocalData();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].uid == uid)
            {
                return items[i];
            }
        }

        return null;
    }

    public PackageLocalData.PackageLocalItem GetPackageLocalItemById(int id)
    {
        List<PackageLocalData.PackageLocalItem> items = GetPackageLocalData();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].id == id)
            {
                return items[i];
            }
        }

        return null;
    }

    public PackageLocalData.PackageLocalItem GetFirstUsableLocalItem()
    {
        List<PackageLocalData.PackageLocalItem> items = GetSortPackageLocalData();
        for (int i = 0; i < items.Count; i++)
        {
            if (IsVisibleLocalItem(items[i]))
            {
                return items[i];
            }
        }

        return null;
    }

    public bool IsVisibleLocalItem(PackageLocalData.PackageLocalItem item)
    {
        return item != null && item.Number > 0 && GetPackageItemById(item.id) != null;
    }

    public PackageLocalData.PackageLocalItem AddItem(int id, int amount = 1, int level = 1)
    {
        if (amount == 0)
        {
            return GetPackageLocalItemById(id);
        }

        PackageLocalData.PackageLocalItem item = GetPackageLocalItemById(id);
        if (item == null)
        {
            item = new PackageLocalData.PackageLocalItem
            {
                uid = Guid.NewGuid().ToString(),
                id = id,
                Number = Mathf.Max(0, amount),
                level = Mathf.Max(1, level),
                isNew = true
            };
            GetPackageLocalData().Add(item);
        }
        else
        {
            item.Number = Mathf.Max(0, item.Number + amount);
            item.level = Mathf.Max(item.level, level);
        }

        SaveAndNotify();
        return item;
    }

    public bool ChangeItemNumber(string uid, int delta)
    {
        PackageLocalData.PackageLocalItem item = GetPackageLocalItemByUid(uid);
        if (item == null)
        {
            return false;
        }

        item.Number = Mathf.Max(0, item.Number + delta);
        SaveAndNotify();
        return true;
    }

    private void SaveAndNotify()
    {
        PackageLocalData.Instance.SavePackage();
        InventoryChanged?.Invoke();
    }

    private sealed class PackageItemComparer : IComparer<PackageLocalData.PackageLocalItem>
    {
        private readonly DataManager dataManager;

        public PackageItemComparer(DataManager dataManager)
        {
            this.dataManager = dataManager;
        }

        public int Compare(PackageLocalData.PackageLocalItem a, PackageLocalData.PackageLocalItem b)
        {
            PackageTableItem tableA = dataManager.GetPackageItemById(a.id);
            PackageTableItem tableB = dataManager.GetPackageItemById(b.id);
            int typeA = tableA != null ? tableA.type : int.MaxValue;
            int typeB = tableB != null ? tableB.type : int.MaxValue;
            int typeComparison = typeA.CompareTo(typeB);
            if (typeComparison != 0)
            {
                return typeComparison;
            }

            int idComparison = a.id.CompareTo(b.id);
            if (idComparison != 0)
            {
                return idComparison;
            }

            return b.level.CompareTo(a.level);
        }
    }
}
