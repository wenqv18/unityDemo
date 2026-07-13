using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MVC Model: static item definitions. Fill the generated Resources/PackageTable asset in the Inspector.
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Package Table", fileName = "PackageTable")]
public sealed class PackageTable : ScriptableObject
{
    public List<PackageTableItem> DataList = new List<PackageTableItem>();
}

[System.Serializable]
public sealed class PackageTableItem
{
    public int id;
    public int type;
    public string itemName;
    [TextArea] public string description;
    public string iconPath;
    public bool usable = true;
}
