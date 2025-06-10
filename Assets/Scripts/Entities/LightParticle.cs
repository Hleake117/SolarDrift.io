using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.Rendering.Universal; // For URP
// using UnityEngine.Experimental.Rendering.Universal; // For older versions

public class LightParticle : NetworkBehaviour
{
    [SerializeField] private float minStartSize = 0.02f;
    [SerializeField] private float maxStartSize = 0.04f;
    [SerializeField] private float maxSize = 0.1f;
    [SerializeField] private float growthDuration = 30f;
    [SerializeField] private float movementSpeed = 0.2f;
    [SerializeField] private float basePoints = 5f;
    [SerializeField] private float pointsMultiplier = 1.5f;
    [SerializeField] private Color[] possibleColors = new Color[]
    {
        new Color(1f, 0.5f, 0.5f, 0.8f), // Light red
        new Color(0.5f, 1f, 0.5f, 0.8f), // Light green
        new Color(0.5f, 0.5f, 1f, 0.8f), // Light blue
        new Color(1f, 1f, 0.5f, 0.8f),   // Light yellow
        new Color(1f, 0.5f, 1f, 0.8f),   // Light purple
        new Color(0.5f, 1f, 1f, 0.8f)    // Light cyan
    };

    [SyncVar] private float currentSize;
    [SyncVar] private float targetSize;
    [SyncVar] private float growthProgress;
    [SyncVar] private float points;

    private Vector3 randomDirection;
    private float directionChangeTimer;
    private const float DIRECTION_CHANGE_INTERVAL = 2f;

    private SpriteRenderer spriteRenderer;
    private Light2D pointLight;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializeParticle();
    }

    [Server]
    private void InitializeParticle()
    {
        // Set random initial size
        currentSize = Random.Range(minStartSize, maxStartSize);
        targetSize = maxSize;
        transform.localScale = Vector3.one * currentSize;
        
        // Calculate points based on size
        points = basePoints * (1 + (currentSize / maxSize) * pointsMultiplier);
        
        // Set random direction
        randomDirection = Random.insideUnitCircle.normalized;
        directionChangeTimer = DIRECTION_CHANGE_INTERVAL;

        spriteRenderer = GetComponent<SpriteRenderer>();
        pointLight = GetComponent<Light2D>();
        
        // Set random color
        if (spriteRenderer != null)
        {
            Color particleColor = possibleColors[Random.Range(0, possibleColors.Length)];
            spriteRenderer.color = particleColor;
            
            // Set light color to match particle
            if (pointLight != null)
            {
                pointLight.color = particleColor;
                pointLight.intensity = 0.8f;
                pointLight.pointLightInnerRadius = 0.1f;
                pointLight.pointLightOuterRadius = 1f;
            }
        }
    }

    void Update()
    {
        if (!isServer) return;

        // Update growth
        if (currentSize < targetSize)
        {
            growthProgress += Time.deltaTime / growthDuration;
            currentSize = Mathf.Lerp(currentSize, targetSize, growthProgress);
            transform.localScale = Vector3.one * currentSize;
            
            // Update points as particle grows
            points = basePoints * (1 + (currentSize / maxSize) * pointsMultiplier);
        }

        // Update movement
        directionChangeTimer -= Time.deltaTime;
        if (directionChangeTimer <= 0)
        {
            randomDirection = Random.insideUnitCircle.normalized;
            directionChangeTimer = DIRECTION_CHANGE_INTERVAL;
        }

        // Move particle
        transform.position += randomDirection * movementSpeed * Time.deltaTime;
    }

    public float GetValue()
    {
        return points;
    }

    public void OnAbsorbed()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
} 