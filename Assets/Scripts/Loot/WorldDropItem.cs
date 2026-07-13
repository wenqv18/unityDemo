using UnityEngine;

/// <summary>
/// World pickup that grants one inventory item to Player and then removes itself.
/// </summary>
public sealed class WorldDropItem : MonoBehaviour
{
    [SerializeField] private int itemId;
    [SerializeField] private int amount = 1;
    [SerializeField] private string playerName = "Player";
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayHeight = 3f;
    [SerializeField] private float groundRayDistance = 8f;
    [SerializeField] private float groundOffset = 0.08f;
    [SerializeField] private bool logPickup;

    private bool pickedUp;

    public void Initialize(int id, int itemAmount)
    {
        itemId = id;
        amount = Mathf.Max(1, itemAmount);
    }

    private void Start()
    {
        SnapToGround();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryPickup(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryPickup(collision.collider);
    }

    private void TryPickup(Collider other)
    {
        if (pickedUp || other == null || !IsPlayer(other.transform))
        {
            return;
        }

        pickedUp = true;
        DataManager.Instance.AddItem(itemId, amount);
        if (logPickup)
        {
            Debug.Log($"Picked up item {itemId} x{amount}.", this);
        }

        Destroy(gameObject);
    }

    private bool IsPlayer(Transform candidate)
    {
        Transform current = candidate;
        while (current != null)
        {
            if (current.name == playerName || current.GetComponent<PlayerMovementController>() != null)
            {
                return true;
            }

            CombatIdentity identity = current.GetComponent<CombatIdentity>();
            if (identity != null)
            {
                return identity.Faction == CombatFaction.Player;
            }

            current = current.parent;
        }

        return false;
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayHeight + groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + Vector3.up * groundOffset;
        }
    }
}
