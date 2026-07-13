using UnityEngine;

/// <summary>
/// Fixed level marker that creates one enemy at this position when the level starts.
/// </summary>
public sealed class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool spawnOnLevelStart = true;
    [SerializeField] private bool snapToGround = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayHeight = 5f;
    [SerializeField] private float groundRayDistance = 12f;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.35f, 0.1f, 0.9f);
    [SerializeField] private float gizmoRadius = 0.45f;

    public bool SpawnOnLevelStart => spawnOnLevelStart;
    public GameObject EnemyPrefab => enemyPrefab;

    public bool TrySpawn(Transform parent, out GameObject enemy)
    {
        enemy = null;
        if (!spawnOnLevelStart || enemyPrefab == null)
        {
            return false;
        }

        Vector3 spawnPosition = snapToGround ? GetGroundedPosition(transform.position) : transform.position;
        enemy = Instantiate(enemyPrefab, spawnPosition, transform.rotation, parent);
        enemy.name = enemyPrefab.name;
        return true;
    }

    private Vector3 GetGroundedPosition(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * groundRayHeight;
        float distance = groundRayHeight + groundRayDistance;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * groundOffset;
        }

        return position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * (gizmoRadius * 1.8f));
    }
}
