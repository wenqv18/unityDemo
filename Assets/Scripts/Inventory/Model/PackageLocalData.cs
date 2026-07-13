using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MVC Model: runtime inventory data owned by the player and saved in PlayerPrefs.
/// Static item definitions live in PackageTable; this class only stores what the player owns.
/// </summary>
[Serializable]
public sealed class PackageLocalData
{
    private const string SaveKey = "PackageLocalData";
    private static PackageLocalData instance;

    public static PackageLocalData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new PackageLocalData();
            }

            return instance;
        }
    }

    public List<PackageLocalItem> items = new List<PackageLocalItem>();

    [Serializable]
    public sealed class PackageLocalItem
    {
        public string uid;
        public bool isNew;
        public int id;
        public int Number;
        public int level = 1;
    }

    public void SavePackage()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(this));
        PlayerPrefs.Save();
    }

    public List<PackageLocalItem> LoadPackage()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            items = new List<PackageLocalItem>();
            return items;
        }

        try
        {
            PackageLocalData loaded = JsonUtility.FromJson<PackageLocalData>(PlayerPrefs.GetString(SaveKey));
            items = loaded != null && loaded.items != null ? loaded.items : new List<PackageLocalItem>();
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Inventory save data could not be loaded: " + exception.Message);
            items = new List<PackageLocalItem>();
        }

        return items;
    }
}
