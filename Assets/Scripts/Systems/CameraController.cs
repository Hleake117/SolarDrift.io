using UnityEngine;
using Mirror;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomFactor = 0.5f;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private Vector2 bounds = new Vector2(40f, 40f);
    [SerializeField] private float playerSearchInterval = 0.5f;

    private Transform target;
    private Vector3 currentVelocity;
    private float currentZoom;
    private float targetZoom;
    private Camera cam;
    private float lastPlayerSearchTime;
    private NetworkManager networkManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("CameraController: Initialized as singleton");
        }
        else
        {
            Debug.LogWarning("CameraController: Multiple instances detected, destroying duplicate");
            Destroy(gameObject);
            return;
        }

        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraController: No Camera component found!");
            return;
        }

        currentZoom = cam.orthographicSize;
        targetZoom = currentZoom;
        networkManager = FindObjectOfType<NetworkManager>();
        Debug.Log($"CameraController: Initialized with zoom {currentZoom}");
    }

    private void Start()
    {
        // Try to find the player immediately
        FindLocalPlayer();
    }

    private void FindLocalPlayer()
    {
        if (networkManager == null)
        {
            Debug.LogError("CameraController: NetworkManager not found!");
            return;
        }

        // Look for the local player
        SunController[] suns = FindObjectsOfType<SunController>();
        foreach (SunController sun in suns)
        {
            if (sun.isLocalPlayer)
            {
                SetTarget(sun.transform);
                Debug.Log($"CameraController: Found local player: {sun.gameObject.name}");
                return;
            }
        }
        
        Debug.Log("CameraController: No local player found, will keep searching...");
    }

    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null)
        {
            Debug.LogWarning("CameraController: Attempted to set null target!");
            return;
        }

        target = newTarget;
        Debug.Log($"CameraController: Target set to {target.name}");
        
        // Immediately update position to avoid initial jump
        if (target != null)
        {
            Vector3 targetPosition = target.position;
            targetPosition.z = transform.position.z;
            transform.position = targetPosition;
        }
    }

    private void LateUpdate()
    {
        // If we don't have a target, try to find the player periodically
        if (target == null)
        {
            if (Time.time - lastPlayerSearchTime > playerSearchInterval)
            {
                lastPlayerSearchTime = Time.time;
                FindLocalPlayer();
            }
            return;
        }

        // Calculate target position
        Vector3 targetPosition = target.position;
        targetPosition.z = transform.position.z;

        // Smoothly move camera
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);

        // Calculate zoom based on sun's mass
        SunController sun = target.GetComponent<SunController>();
        if (sun != null)
        {
            float mass = sun.GetMass();
            targetZoom = Mathf.Clamp(mass * zoomFactor, minZoom, maxZoom);
            
            // Smoothly adjust zoom
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomSpeed);
            cam.orthographicSize = currentZoom;
        }

        // Keep camera within bounds
        Vector3 pos = transform.position;
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;

        pos.x = Mathf.Clamp(pos.x, -bounds.x + width/2, bounds.x - width/2);
        pos.y = Mathf.Clamp(pos.y, -bounds.y + height/2, bounds.y - height/2);
        transform.position = pos;
    }
} 