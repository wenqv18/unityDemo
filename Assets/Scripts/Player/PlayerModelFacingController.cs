using UnityEngine;

/// <summary>
/// Rotates only the visual model toward the player's movement direction.
/// The Player root keeps its physics rotation locked for stable movement and camera follow.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerModelFacingController : MonoBehaviour
{
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float turnSpeedDegreesPerSecond = 720f;
    [SerializeField] private float movementThreshold = 0.05f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        ResolveReferences();
        if (modelRoot == null || rb == null)
        {
            return;
        }

        Vector3 flatVelocity = rb.velocity;
        flatVelocity.y = 0f;
        if (flatVelocity.sqrMagnitude <= movementThreshold * movementThreshold)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
        modelRoot.rotation = Quaternion.RotateTowards(
            modelRoot.rotation,
            targetRotation,
            turnSpeedDegreesPerSecond * Time.deltaTime);
    }

    private void ResolveReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (modelRoot == null)
        {
            Transform model = transform.Find("Model");
            modelRoot = model != null ? model : transform;
        }
    }
}
