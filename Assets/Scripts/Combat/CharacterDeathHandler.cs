using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Handles what happens when a character's runtime health reaches zero.
/// Drop spawning, death effects, and score logic can be connected here later without changing damage code.
/// </summary>
[RequireComponent(typeof(CharacterRuntimeStats))]
public sealed class CharacterDeathHandler : MonoBehaviour
{
    [SerializeField] private CharacterRuntimeStats stats;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay;
    [SerializeField] private bool detachDeathVisual = true;
    [SerializeField] private bool hideOriginalModelOnDeath = true;
    [SerializeField] private bool disableCollidersOnDeath = true;
    [SerializeField] private bool disableBehavioursOnDeath = true;
    [SerializeField] private string modelChildName = "Model";
    [SerializeField] private string deathSoundResourcePath;
    [SerializeField, Range(0f, 1f)] private float deathSoundVolume = 1f;
    [SerializeField] private float fallbackDeathVisualDuration = 3.9f;
    [SerializeField] private UnityEvent onDeath;

    private bool handledDeath;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (stats != null)
        {
            stats.Died += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.Died -= HandleDeath;
        }
    }

    private void ResolveReferences()
    {
        if (stats == null)
        {
            stats = GetComponent<CharacterRuntimeStats>();
        }
    }

    private void HandleDeath(CharacterRuntimeStats deadStats)
    {
        if (handledDeath)
        {
            return;
        }

        handledDeath = true;
        onDeath?.Invoke();
        GameSoundPlayer.PlayAt(deathSoundResourcePath, transform.position, deathSoundVolume);

        Transform model = FindModel();
        if (detachDeathVisual)
        {
            SpawnDetachedDeathVisual(model);
        }

        DisableRuntimeObject(model);

        if (destroyOnDeath)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
            return;
        }

        gameObject.SetActive(false);
    }

    private Transform FindModel()
    {
        return string.IsNullOrEmpty(modelChildName) ? null : transform.Find(modelChildName);
    }

    private void DisableRuntimeObject(Transform model)
    {
        if (hideOriginalModelOnDeath && model != null)
        {
            model.gameObject.SetActive(false);
        }

        if (disableCollidersOnDeath)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        if (disableBehavioursOnDeath)
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this || behaviour is CharacterRuntimeStats)
                {
                    continue;
                }

                behaviour.enabled = false;
            }
        }
    }

    private void SpawnDetachedDeathVisual(Transform model)
    {
        if (model == null)
        {
            return;
        }

        GameObject visual = Instantiate(model.gameObject, model.position, model.rotation);
        visual.name = name + "_DeathVisual";
        visual.transform.localScale = model.lossyScale;

        Collider[] visualColliders = visual.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < visualColliders.Length; i++)
        {
            visualColliders[i].enabled = false;
        }

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        CharacterAnimationDriver driver = GetComponent<CharacterAnimationDriver>();
        float lifetime = driver != null ? driver.GetDeathDuration(fallbackDeathVisualDuration) : fallbackDeathVisualDuration;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime("Death", 0.02f);
        }

        Destroy(visual, Mathf.Max(0.1f, lifetime));
    }
}
