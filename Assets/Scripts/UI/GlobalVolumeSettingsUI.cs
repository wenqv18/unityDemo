using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the global game volume from the main menu settings slider.
/// Slider value is 0-1; the visible number displays value * 100.
/// </summary>
public sealed class GlobalVolumeSettingsUI : MonoBehaviour
{
    private const string VolumePrefsKey = "GlobalVolume";
    private const float DefaultVolume = 1f;

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Text numberText;
    [SerializeField] private TMP_Text numberTmpText;
    [SerializeField] private string sliderChildName = "Slider";
    [SerializeField] private string numberChildName = "Number";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedVolumeOnStartup()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(VolumePrefsKey, DefaultVolume);
    }

    private void Awake()
    {
        ResolveReferences();
        InitializeSlider();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
            volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
            HandleVolumeChanged(volumeSlider.value);
        }
    }

    private void OnDisable()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }
    }

    private void ResolveReferences()
    {
        if (volumeSlider == null)
        {
            Transform sliderTransform = FindChildRecursive(transform, sliderChildName);
            volumeSlider = sliderTransform != null ? sliderTransform.GetComponent<Slider>() : GetComponentInChildren<Slider>(true);
        }

        if (numberText == null && numberTmpText == null)
        {
            Transform numberTransform = FindChildRecursive(transform, numberChildName);
            if (numberTransform != null)
            {
                numberText = numberTransform.GetComponent<Text>();
                numberTmpText = numberTransform.GetComponent<TMP_Text>();
            }
        }
    }

    private void InitializeSlider()
    {
        if (volumeSlider == null)
        {
            return;
        }

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.wholeNumbers = false;
        volumeSlider.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePrefsKey, AudioListener.volume)));
        HandleVolumeChanged(volumeSlider.value);
    }

    private void HandleVolumeChanged(float value)
    {
        float volume = Mathf.Clamp01(value);
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat(VolumePrefsKey, volume);
        PlayerPrefs.Save();
        UpdateNumber(volume);
    }

    private void UpdateNumber(float volume)
    {
        string text = Mathf.RoundToInt(volume * 100f).ToString();
        if (numberText != null)
        {
            numberText.text = text;
        }

        if (numberTmpText != null)
        {
            numberTmpText.text = text;
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
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
