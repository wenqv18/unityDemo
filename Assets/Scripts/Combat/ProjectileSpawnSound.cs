using UnityEngine;

/// <summary>
/// Optional one-shot sound for projectile prefab instances when they are spawned.
/// </summary>
public sealed class ProjectileSpawnSound : MonoBehaviour
{
    [SerializeField] private string soundResourcePath = GameSoundPlayer.FireArrowPath;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private void Start()
    {
        GameSoundPlayer.PlayAt(soundResourcePath, transform.position, volume);
    }
}
