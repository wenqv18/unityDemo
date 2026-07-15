using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Converts ground clicks into a shared summon command point.
/// </summary>
public sealed class PlayerSummonCommandInputController : MonoBehaviour
{
    private const string DefaultMarkerVfxResourcePath = "ISEffect/JianTou_02/Prefabs/JianTou_02_Blue";

    [SerializeField] private Camera targetCamera;
    [SerializeField] private int mouseButton = 0;
    [SerializeField] private float raycastDistance = 300f;
    [SerializeField] private float commandRadius = SummonCommandPoint.DefaultMarkerRadius;
    [SerializeField] private bool ignoreInputWhenPaused = true;
    [SerializeField] private bool ignoreInputOverUI = true;
    [Header("Marker VFX")]
    [SerializeField] private GameObject markerVfxPrefab;
    [SerializeField] private string markerVfxResourcePath = DefaultMarkerVfxResourcePath;
    [SerializeField] private float markerVfxYOffset = 1.4f;

    private static bool sceneHooked;
    private GameObject currentMarkerVfx;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallForLoadedScene()
    {
        InstallOnPlayer();
        if (!sceneHooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            sceneHooked = true;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SummonCommandPoint.Clear();
        InstallOnPlayer();
    }

    private static void InstallOnPlayer()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null || player.GetComponent<PlayerSummonSpawner>() == null)
        {
            return;
        }

        if (player.GetComponent<PlayerSummonCommandInputController>() == null)
        {
            player.AddComponent<PlayerSummonCommandInputController>();
        }
    }

    private void Awake()
    {
        ResolveCamera();
        ResolveMarkerVfxPrefab();
    }

    private void OnDestroy()
    {
        if (currentMarkerVfx != null)
        {
            Destroy(currentMarkerVfx);
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(mouseButton)
            || (ignoreInputWhenPaused && Time.timeScale <= 0f)
            || IsPointerOverUI())
        {
            return;
        }

        ResolveCamera();
        if (targetCamera == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, CombatSpatialQuery.GroundMask, QueryTriggerInteraction.Ignore)
            || !CombatSpatialQuery.IsGroundLayer(hit.collider.gameObject.layer))
        {
            return;
        }

        SummonCommandPoint.SetMarker(hit.point, commandRadius);
        ShowMarkerVfx(hit.point);
    }

    private bool IsPointerOverUI()
    {
        return ignoreInputOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void ResolveMarkerVfxPrefab()
    {
        if (markerVfxPrefab == null && !string.IsNullOrEmpty(markerVfxResourcePath))
        {
            markerVfxPrefab = Resources.Load<GameObject>(markerVfxResourcePath);
        }
    }

    private void ShowMarkerVfx(Vector3 markerPosition)
    {
        ResolveMarkerVfxPrefab();
        if (markerVfxPrefab == null)
        {
            return;
        }

        if (currentMarkerVfx != null)
        {
            Destroy(currentMarkerVfx);
        }

        Vector3 vfxPosition = markerPosition + Vector3.up * markerVfxYOffset;
        currentMarkerVfx = Instantiate(markerVfxPrefab, vfxPosition, Quaternion.identity);
    }
}
