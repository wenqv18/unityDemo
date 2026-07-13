using UnityEngine;

/// <summary>
/// Keeps the camera locked to the player from a top-down 2.5D angle.
/// If no target is assigned, it searches for a GameObject named by targetName.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private string targetName = "Cube";
    [SerializeField] private Vector3 offset = new Vector3(0f, 22f, -17.7f);
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private bool useOrthographic = true;
    [SerializeField] private float orthographicSize = 14f;

    private Camera followCamera;

    private void Awake()
    {
        followCamera = GetComponent<Camera>();
        ConfigureCamera();
        ResolveTargetIfNeeded();
    }

    private void LateUpdate()
    {
        ResolveTargetIfNeeded();
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = desiredPosition;
        transform.LookAt(target.position + lookOffset, Vector3.up);
    }

    private void ConfigureCamera()
    {
        followCamera.orthographic = useOrthographic;
        if (useOrthographic)
        {
            followCamera.orthographicSize = orthographicSize;
        }
    }

    private void ResolveTargetIfNeeded()
    {
        if (target != null || string.IsNullOrWhiteSpace(targetName))
        {
            return;
        }

        GameObject foundTarget = GameObject.Find(targetName);
        if (foundTarget != null)
        {
            target = foundTarget.transform;
        }
    }
}
