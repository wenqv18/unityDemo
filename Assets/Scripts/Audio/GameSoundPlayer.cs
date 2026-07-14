using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small shared helper for one-shot gameplay sounds stored under Resources.
/// </summary>
public static class GameSoundPlayer
{
    public const string SummonBeginPath = "Sounds/Sound_Begin";
    public const string FireballExplosionPath = "Sounds/Sound_FireballExplosion";
    public const string FireArrowPath = "Sounds/Sound_FireArrow";
    public const string DeathPath = "Sounds/Sound_Death";
    public const string SwordAttackPath = "Sounds/Sound_AttackSword";

    private static readonly Dictionary<string, AudioClip> ClipCache = new Dictionary<string, AudioClip>();

    public static void PlayAt(string resourcePath, Vector3 position, float volume = 1f)
    {
        AudioClip clip = LoadClip(resourcePath);
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
    }

    private static AudioClip LoadClip(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (ClipCache.TryGetValue(resourcePath, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning($"Audio clip not found in Resources: {resourcePath}");
            return null;
        }

        ClipCache[resourcePath] = clip;
        return clip;
    }
}
