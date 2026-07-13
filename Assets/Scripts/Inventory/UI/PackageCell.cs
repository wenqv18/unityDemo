using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MVC View: one item cell in the inventory grid.
/// </summary>
public sealed class PackageCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text nameText;
    [SerializeField] private Text numberText;
    [SerializeField] private TextMeshProUGUI tmpNameText;
    [SerializeField] private TextMeshProUGUI tmpNumberText;
    [SerializeField] private GameObject selectedVisual;
    [SerializeField] private GameObject hoverVisual;

    private PackageLocalData.PackageLocalItem localData;
    private PackageButtonUI owner;

    private void Awake()
    {
        ResolveReferences();
    }

    public void Refresh(PackageLocalData.PackageLocalItem data, PackageButtonUI parent)
    {
        ResolveReferences();
        localData = data;
        owner = parent;

        PackageTableItem tableItem = DataManager.Instance.GetPackageItemById(data.id);
        SetText(nameText, tmpNameText, tableItem != null ? tableItem.itemName : "Unknown");
        SetText(numberText, tmpNumberText, data.Number.ToString());

        if (iconImage != null)
        {
            Sprite sprite = InventorySpriteLoader.Load(tableItem != null ? tableItem.iconPath : null);
            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        SetSelected(owner != null && owner.ChooseUid == data.uid);
        SetActive(hoverVisual, false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null && localData != null)
        {
            owner.SelectItem(localData.uid);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetActive(hoverVisual, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetActive(hoverVisual, false);
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            Transform icon = transform.Find("Top/Thing") ?? transform.Find("Thing") ?? transform.Find("Top/Image");
            iconImage = icon != null ? icon.GetComponent<Image>() : null;
        }

        if (nameText == null && tmpNameText == null)
        {
            Transform name = transform.Find("Bottom/Name") ?? transform.Find("Name");
            if (name != null)
            {
                nameText = name.GetComponent<Text>();
                tmpNameText = name.GetComponent<TextMeshProUGUI>();
            }
        }

        if (numberText == null && tmpNumberText == null)
        {
            Transform number = transform.Find("Bottom/Number") ?? transform.Find("Number") ?? transform.Find("Bottom/Text");
            if (number != null)
            {
                numberText = number.GetComponent<Text>();
                tmpNumberText = number.GetComponent<TextMeshProUGUI>();
            }
        }

        if (selectedVisual == null)
        {
            Transform selected = transform.Find("Select") ?? transform.Find("BG1");
            selectedVisual = selected != null ? selected.gameObject : null;
        }

        if (hoverVisual == null)
        {
            Transform hover = transform.Find("DeleteSelect") ?? transform.Find("BG2");
            hoverVisual = hover != null ? hover.gameObject : null;
        }
    }

    private void SetSelected(bool selected)
    {
        SetActive(selectedVisual, selected);
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
