using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class SunController : NetworkBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float drag = 0.95f;
    [SerializeField] private float initialMass = 1f;
    [SerializeField] private Vector2 bounds = new Vector2(20f, 20f);
    [SerializeField] private float absorptionRadius = 1f;
    [SerializeField] private float baseSize = 1f;
    [SerializeField] private float sizePerHundredPoints = 0.1f;
    [SerializeField] private float maxSize = 10f;
    [SerializeField] private float maxOrbitingPlanets = 8f;
    [SerializeField] private float orbitSpacing = 1.5f;
    [SerializeField] private float minOrbitDistance = 2f;
    [SerializeField] private Color sunColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private GameObject captureEffectPrefab;
    [SerializeField] private GameObject boostTrailPrefab;
    [SerializeField] private float boostSpeedMultiplier = 2f;
    [SerializeField] private float boostPointCost = 10f;
    [SerializeField] private float boostCooldown = 2f;
    [SerializeField] private float boostDuration = 1f;

    [SyncVar] private Vector3 velocity;
    [SyncVar] private float currentMass;
    [SyncVar] private float points;
    [SyncVar] private float currentSize;
    [SyncVar] private int orbitingPlanetCount;
    [SyncVar] private bool isBoosting;

    private SpawnPointManager spawnManager;
    private CameraController cameraController;
    private PointsDisplay pointsDisplay;
    private BoxCollider2D boxCollider;
    private List<PlanetController> orbitingPlanets = new List<PlanetController>();
    private float nextBoostTime;
    private GameObject currentBoostTrail;

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentMass = initialMass;
        currentSize = baseSize;
        points = 0f;
        transform.localScale = Vector3.one * currentSize;
        
        // Initialize collider
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            UpdateColliderSize();
        }

        // Set initial color
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = sunColor;
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            Debug.Log($"SunController: Local player started: {gameObject.name}");
            
            // Set up spawn position
            spawnManager = FindAnyObjectByType<SpawnPointManager>();
            if (spawnManager != null)
            {
                transform.position = spawnManager.GetNextSpawnPosition();
                Debug.Log($"SunController: Set spawn position to {transform.position}");
            }

            // Set up camera
            SetupCamera();

            // Set up UI
            SetupUI();
        }
    }

    private void SetupCamera()
    {
        // Wait a frame to ensure CameraController is initialized
        StartCoroutine(SetupCameraDelayed());
    }

    private System.Collections.IEnumerator SetupCameraDelayed()
    {
        yield return null; // Wait one frame

        // Try to find the camera controller
        if (CameraController.Instance == null)
        {
            Debug.LogError("SunController: CameraController singleton not found! Will retry...");
            yield return new WaitForSeconds(0.5f);
            
            if (CameraController.Instance == null)
            {
                Debug.LogError("SunController: CameraController still not found after delay!");
                yield break;
            }
        }

        // Set this sun as the camera target
        CameraController.Instance.SetTarget(transform);
        Debug.Log($"SunController: Set as camera target for {gameObject.name}");
    }

    private void SetupUI()
    {
        // Find the PointsDisplay in the scene
        pointsDisplay = FindAnyObjectByType<PointsDisplay>();
        if (pointsDisplay == null)
        {
            Debug.LogError("SunController: PointsDisplay not found in scene!");
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Get mouse position and calculate direction
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 direction = (mousePos - transform.position).normalized;

        // Check for boost input
        if (Input.GetKey(KeyCode.Space) && CanBoost())
        {
            CmdActivateBoost();
        }

        // Apply acceleration based on direction
        float currentSpeed = isBoosting ? maxSpeed * boostSpeedMultiplier : maxSpeed;
        velocity += direction * acceleration * Time.deltaTime;

        // Apply drag
        velocity *= drag;

        // Clamp velocity to max speed
        velocity = Vector3.ClampMagnitude(velocity, currentSpeed);

        // Move the sun
        transform.position += velocity * Time.deltaTime;

        // Keep within bounds
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -bounds.x, bounds.x);
        pos.y = Mathf.Clamp(pos.y, -bounds.y, bounds.y);
        transform.position = pos;

        // Check for particle absorption
        if (isServer)
        {
            CheckForParticleAbsorption();
        }

        // Update UI
        if (GameUI.Instance != null)
        {
            GameUI.Instance.UpdatePoints(points);
        }
    }

    private bool CanBoost()
    {
        return Time.time >= nextBoostTime && points >= boostPointCost && !isBoosting;
    }

    [Command]
    private void CmdActivateBoost()
    {
        if (!CanBoost()) return;

        // Deduct points
        points -= boostPointCost;
        
        // Set boost state
        isBoosting = true;
        nextBoostTime = Time.time + boostCooldown;
        
        // Spawn boost trail
        RpcSpawnBoostTrail();
        
        // Start boost duration timer
        StartCoroutine(EndBoostAfterDuration());
    }

    [ClientRpc]
    private void RpcSpawnBoostTrail()
    {
        if (boostTrailPrefab != null)
        {
            // Destroy existing trail if any
            if (currentBoostTrail != null)
            {
                Destroy(currentBoostTrail);
            }
            
            // Spawn new trail
            currentBoostTrail = Instantiate(boostTrailPrefab, transform.position, Quaternion.identity);
            currentBoostTrail.transform.SetParent(transform);
            
            // Set trail color to match sun
            var trailRenderer = currentBoostTrail.GetComponent<TrailRenderer>();
            if (trailRenderer != null)
            {
                trailRenderer.startColor = sunColor;
                trailRenderer.endColor = new Color(sunColor.r, sunColor.g, sunColor.b, 0f);
            }
        }
    }

    private System.Collections.IEnumerator EndBoostAfterDuration()
    {
        yield return new WaitForSeconds(boostDuration);
        isBoosting = false;
        
        // Clean up boost trail
        if (currentBoostTrail != null)
        {
            Destroy(currentBoostTrail);
            currentBoostTrail = null;
        }
    }

    [Server]
    private void CheckForParticleAbsorption()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, absorptionRadius);
        foreach (Collider2D collider in colliders)
        {
            LightParticle particle = collider.GetComponent<LightParticle>();
            if (particle != null)
            {
                // Absorb the particle
                float particlePoints = particle.GetValue();
                CmdIncreasePoints(particlePoints);
                particle.OnAbsorbed();
            }
        }
    }

    private void UpdateColliderSize()
    {
        if (boxCollider != null)
        {
            // Set collider size relative to the sun's scale
            boxCollider.size = Vector2.one * (currentSize * 1.2f); // Slightly larger than the sun
        }
    }

    [Command]
    private void CmdIncreasePoints(float amount)
    {
        points += amount;
        
        // Calculate new size based on points (grows every 100 points)
        float pointsForGrowth = Mathf.Floor(points / 100f) * 100f;
        float newSize = baseSize + (pointsForGrowth / 100f) * sizePerHundredPoints;
        currentSize = Mathf.Min(newSize, maxSize);
        
        // Update mass based on size
        currentMass = initialMass + (currentSize * 0.5f);
        
        // Update scale
        transform.localScale = Vector3.one * currentSize;
        
        // Update collider size
        UpdateColliderSize();
        
        // Update absorption radius based on size
        absorptionRadius = currentSize * 1.5f;
    }

    public float GetMass() => currentMass;
    public float GetPoints() => points;
    public float GetSize() => currentSize;

    public void AddOrbitingPlanet(PlanetController planet)
    {
        if (!isServer) return;
        
        if (orbitingPlanetCount < maxOrbitingPlanets)
        {
            orbitingPlanets.Add(planet);
            orbitingPlanetCount++;
            
            // Adjust orbit distances to prevent overlap
            for (int i = 0; i < orbitingPlanets.Count; i++)
            {
                float orbitDistance = minOrbitDistance + (i * orbitSpacing);
                orbitingPlanets[i].SetOrbitDistance(orbitDistance);
            }
        }
    }

    public int GetOrbitCount() => orbitingPlanetCount;
    public float GetOrbitSpacing() => orbitSpacing;

    public Color GetSunColor() => sunColor;
}
