using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space health bar that reads CharacterRuntimeStats and keeps facing the camera.
/// Put this on the health bar root or fill image under a character prefab.
/// </summary>
public sealed class WorldHealthBarUI : MonoBehaviour
{
    [SerializeField] private CharacterRuntimeStats targetStats;
    [SerializeField] private Image fillImage;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool hideWhenFull;

    private void Awake()
    {
        ResolveReferences();
        RefreshFill();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        RefreshFill();
        FaceCamera();
    }

    private void OnValidate()
    {
        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
        }
    }

    private void ResolveReferences()
    {
        if (targetStats == null)
        {
            targetStats = GetComponentInParent<CharacterRuntimeStats>();
        }

        if (fillImage == null)
        {
            fillImage = GetComponentInChildren<Image>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void RefreshFill()
    {
        if (targetStats == null || fillImage == null)
        {
            return;
        }

        float healthPercent = targetStats.MaxHealth > 0
            ? (float)targetStats.CurrentHealth / targetStats.MaxHealth
            : 0f;

        fillImage.fillAmount = Mathf.Clamp01(healthPercent);

        if (hideWhenFull)
        {
            gameObject.SetActive(healthPercent < 0.999f && healthPercent > 0f);
        }
    }

    private void FaceCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 direction = transform.position - targetCamera.transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(direction.normalized, targetCamera.transform.up);
    }
}
