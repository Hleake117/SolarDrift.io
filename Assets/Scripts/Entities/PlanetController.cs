using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Collections;

public class PlanetController : NetworkBehaviour
{
    [SerializeField] private float minSize = 0.5f;
    [SerializeField] private float maxSize = 2f;
    [SerializeField] private float captureRadius = 3f;
    [SerializeField] private float orbitSpeed = 30f;
    [SerializeField] private float minOrbitDistance = 2f;
    [SerializeField] private float maxOrbitDistance = 8f;
    [SerializeField] private GameObject captureEffectPrefab;
    [SerializeField] private float captureEffectDuration = 1f;
    [SerializeField] private float minColorBrightness = 0.6f;
    [SerializeField] private float maxColorBrightness = 1f;
    [SerializeField] private int captureParticleCount = 50;
    [SerializeField] private float captureParticleSize = 0.3f;
    [SerializeField] private float captureParticleSpeed = 3f;
    [SerializeField] private float captureParticleLifetime = 1.5f;
    
    [SyncVar] private float currentSize;
    [SyncVar] private Vector3 orbitCenter;
    [SyncVar] private float orbitDistance;
    [SyncVar] private float orbitAngle;
    [SyncVar] private bool isOrbiting;
    [SyncVar] private float orbitIndex;
    [SyncVar] private Color orbitColor;
    
    private SunController orbitingSun;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        // Randomize planet size
        currentSize = Random.Range(minSize, maxSize);
        transform.localScale = Vector3.one * currentSize;
        
        // Generate random orbit color
        orbitColor = GenerateRandomColor();
        
        // Initialize components
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 0.5f;
        }
    }
    
    private void Update()
    {
        if (!isOrbiting) return;
        
        // Update orbit position
        orbitAngle += orbitSpeed * Time.deltaTime;
        float x = orbitCenter.x + Mathf.Cos(orbitAngle) * orbitDistance;
        float y = orbitCenter.y + Mathf.Sin(orbitAngle) * orbitDistance;
        transform.position = new Vector3(x, y, 0);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isServer) return;
        
        SunController sun = other.GetComponent<SunController>();
        if (sun != null && !isOrbiting)
        {
            // Check if sun is big enough to capture this planet (double the size)
            float sunSize = sun.GetSize();
            if (sunSize >= currentSize * 2f)
            {
                CmdEnterOrbit(sun);
            }
        }
    }
    
    [Command]
    private void CmdEnterOrbit(SunController sun)
    {
        if (isOrbiting) return;
        
        orbitingSun = sun;
        isOrbiting = true;
        
        // Calculate orbit parameters
        orbitCenter = sun.transform.position;
        orbitDistance = Random.Range(minOrbitDistance, maxOrbitDistance);
        orbitAngle = Random.Range(0f, 360f);
        orbitIndex = sun.GetOrbitCount();
        
        // Disable physics
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector2.zero;
        }
        
        // Notify spawner that this planet is captured
        PlanetSpawner spawner = FindObjectOfType<PlanetSpawner>();
        if (spawner != null)
        {
            spawner.OnPlanetCaptured(gameObject);
        }
        
        // Play capture effect
        RpcPlayCaptureEffect(sun.GetSunColor());
        
        // Notify the sun
        sun.AddOrbitingPlanet(this);
    }
    
    private Color GenerateRandomColor()
    {
        // Generate a random hue
        float hue = Random.value;
        
        // Convert to RGB
        Color color = Color.HSVToRGB(hue, 1f, 1f);
        
        // Adjust brightness
        float brightness = Random.Range(minColorBrightness, maxColorBrightness);
        color.r *= brightness;
        color.g *= brightness;
        color.b *= brightness;
        
        return color;
    }
    
    [ClientRpc]
    private void RpcPlayCaptureEffect(Color sunColor)
    {
        // Spawn and play capture effect
        if (captureEffectPrefab != null)
        {
            try
            {
                GameObject effect = Instantiate(captureEffectPrefab, transform.position, Quaternion.identity);
                var fxController = effect.GetComponent<CaptureFXController>();
                if (fxController != null)
                {
                    // Configure enhanced particle effect
                    fxController.ConfigureEffect(
                        captureParticleCount,
                        captureParticleSize,
                        captureParticleSpeed,
                        captureParticleLifetime
                    );
                    fxController.PlayEffect(sunColor);
                }
                else
                {
                    Debug.LogError("PlanetController: CaptureEffectPrefab is missing CaptureFXController component!");
                    Destroy(effect);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlanetController: Error spawning capture effect: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("PlanetController: No capture effect prefab assigned!");
        }
        
        // Change planet color to match orbit color
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = orbitColor;
        }
    }
    
    public float GetSize() => currentSize;
    public float GetOrbitIndex() => orbitIndex;
    
    public void SetOrbitDistance(float distance)
    {
        orbitDistance = distance;
    }
} 