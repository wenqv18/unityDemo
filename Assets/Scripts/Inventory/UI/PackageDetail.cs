using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MVC View: right-side item detail panel.
/// </summary>
public sealed class PackageDetail : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text numberText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private TextMeshProUGUI tmpNameText;
    [SerializeField] private TextMeshProUGUI tmpNumberText;
    [SerializeField] private TextMeshProUGUI tmpDescriptionText;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Refresh(PackageLocalData.PackageLocalItem localItem)
    {
        ResolveReferences();
        if (localItem == null)
        {
            Clear();
            return;
        }

        PackageTableItem tableItem = DataManager.Instance.GetPackageItemById(localItem.id);
        if (tableItem == null)
        {
            Clear();
            return;
        }

        SetText(nameText, tmpNameText, tableItem.itemName);
        SetText(numberText, tmpNumberText, localItem.Number.ToString());
        SetText(descriptionText, tmpDescriptionText, tableItem.description);

        if (iconImage != null)
        {
            Sprite sprite = InventorySpriteLoader.Load(tableItem.iconPath);
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }
    }

    public void Clear()
    {
        SetText(nameText, tmpNameText, string.Empty);
        SetText(numberText, tmpNumberText, string.Empty);
        SetText(descriptionText, tmpDescriptionText, string.Empty);

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            Transform icon = transform.Find("Top/Thing") ?? transform.Find("Top/Image") ?? transform.Find("Thing");
            iconImage = icon != null ? icon.GetComponent<Image>() : null;
        }

        if (nameText == null && tmpNameText == null)
        {
            Transform target = transform.Find("Top/Name") ?? transform.Find("Name");
            CacheText(target, ref nameText, ref tmpNameText);
        }

        if (numberText == null && tmpNumberText == null)
        {
            Transform target = transform.Find("Center/Number/num") ?? transform.Find("Number/num") ?? transform.Find("Number");
            CacheText(target, ref numberText, ref tmpNumberText);
        }

        if (descriptionText == null && tmpDescriptionText == null)
        {
            Transform target = transform.Find("Center/Details") ?? transform.Find("Details") ?? transform.Find("Description");
            CacheText(target, ref descriptionText, ref tmpDescriptionText);
        }
    }

    private static void CacheText(Transform target, ref Text legacyText, ref TextMeshProUGUI tmpText)
    {
        if (target == null)
        {
            return;
        }

        legacyText = target.GetComponent<Text>();
        tmpText = target.GetComponent<TextMeshProUGUI>();
    }

    private static void SetText(Text legacyText, TextMeshProUGUI tmpText, string value)
    {
        if (legacyText != null)
        {
            legacyText.text = value;
        }

        if (tmpText != null)
        {
            tmpText.text = value;
        }
    }
}
