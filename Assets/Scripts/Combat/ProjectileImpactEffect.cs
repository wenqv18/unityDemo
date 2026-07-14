using UnityEngine;

/// <summary>
/// Optional projectile hit effect. Add this to projectile prefabs that should leave a
/// particle effect behind when the projectile itself is destroyed.
/// </summary>
public sealed class ProjectileImpactEffect : MonoBehaviour
{
    [SerializeField] private GameObject impactEffectObject;
    [SerializeField] private string impactEffectObjectName = "Explosion_Rubble";
    [SerializeField] private string impactSoundResourcePath;
    [SerializeField, Range(0f, 1f)] private float impactSoundVolume = 1f;
    [SerializeField] private float fallbackLifetime = 3f;

    private void Awake()
    {
        ResolveImpactEffect();
        if (impactEffectObject != null)
        {
            impactEffectObject.SetActive(false);
        }
    }

    public void PlayImpactEffect()
    {
        ResolveImpactEffect();
        if (impactEffectObject == null)
        {
            return;
        }

        Transform effectTransform = impactEffectObject.transform;
        effectTransform.SetParent(null, true);
        impactEffectObject.SetActive(true);
        GameSoundPlayer.PlayAt(impactSoundResourcePath, effectTransform.position, impactSoundVolume);

        ParticleSystem[] particleSystems = impactEffectObject.GetComponentsInChildren<ParticleSystem>(true);
        float lifetime = fallbackLifetime;
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            particleSystem.Play(true);
        }

        Destroy(impactEffectObject, lifetime);
        impactEffectObject = null;
    }

    private void ResolveImpactEffect()
    {
        if (impactEffectObject != null || string.IsNullOrEmpty(impactEffectObjectName))
        {
            return;
        }

        Transform effectTransform = FindChildRecursive(transform, impactEffectObjectName);
        if (effectTransform != null)
        {
            impactEffectObject = effectTransform.gameObject;
        }
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
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
