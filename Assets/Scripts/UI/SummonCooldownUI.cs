using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the drawing summon cooldown on a Filled Image.
/// Attach this directly to GameUI/Bottom/BottomCenter/Time.
/// </summary>
[RequireComponent(typeof(Image))]
public sealed class SummonCooldownUI : MonoBehaviour
{
    [SerializeField] private Image cooldownImage;
    [SerializeField] private PlayerSummonSpawner summonSpawner;

    private void Awake()
    {
        ResolveReferences();
        UpdateFill();
    }

    private void OnEnable()
    {
        ResolveReferences();
        UpdateFill();
    }

    private void Update()
    {
        if (summonSpawner == null)
        {
            ResolveSpawner();
        }

        UpdateFill();
    }

    private void ResolveReferences()
    {
        if (cooldownImage == null)
        {
            cooldownImage = GetComponent<Image>();
        }

        ResolveSpawner();
    }

    private void ResolveSpawner()
    {
        if (summonSpawner != null)
        {
            return;
        }

        GameObject player = GameObject.Find("Player");
        summonSpawner = player != null ? player.GetComponent<PlayerSummonSpawner>() : FindObjectOfType<PlayerSummonSpawner>(true);
    }

    private void UpdateFill()
    {
        if (cooldownImage == null)
        {
            return;
        }

        cooldownImage.fillAmount = summonSpawner != null ? summonSpawner.DrawingSummonCooldownFill : 0f;
    }
}
