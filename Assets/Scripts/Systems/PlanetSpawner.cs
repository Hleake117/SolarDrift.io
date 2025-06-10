using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PlanetSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] planetPrefabs;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int maxPlanets = 20;
    [SerializeField] private Vector2 spawnBounds = new Vector2(40f, 40f);
    [SerializeField] private float minSpawnDistance = 5f;
    
    [Header("Performance Settings")]
    [SerializeField] private float cleanupInterval = 30f;
    [SerializeField] private float maxDistanceFromCenter = 50f;
    
    private float nextSpawnTime;
    private float nextCleanupTime;
    private List<GameObject> activePlanets = new List<GameObject>();
    private Transform centerPoint;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        nextSpawnTime = Time.time + spawnInterval;
        nextCleanupTime = Time.time + cleanupInterval;
        
        // Find center point (usually the first sun or a designated point)
        centerPoint = FindObjectOfType<SunController>()?.transform;
        if (centerPoint == null)
        {
            Debug.LogWarning("PlanetSpawner: No center point found, using world origin");
            centerPoint = new GameObject("CenterPoint").transform;
        }
    }
    
    private void Update()
    {
        if (!isServer) return;
        
        float currentTime = Time.time;
        
        // Clean up destroyed planets
        activePlanets.RemoveAll(planet => planet == null);
        
        // Spawn new planets if needed
        if (currentTime >= nextSpawnTime && activePlanets.Count < maxPlanets)
        {
            SpawnPlanet();
            nextSpawnTime = currentTime + spawnInterval;
        }
        
        // Periodic cleanup of far planets
        if (currentTime >= nextCleanupTime)
        {
            CleanupFarPlanets();
            nextCleanupTime = currentTime + cleanupInterval;
        }
    }
    
    private void SpawnPlanet()
    {
        if (planetPrefabs == null || planetPrefabs.Length == 0)
        {
            Debug.LogError("PlanetSpawner: No planet prefabs assigned!");
            return;
        }
        
        // Find a valid spawn position
        Vector3 spawnPos = GetValidSpawnPosition();
        if (spawnPos == Vector3.zero) return;
        
        // Randomly select a planet prefab
        GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
        
        // Spawn the planet
        GameObject planet = Instantiate(prefab, spawnPos, Quaternion.identity);
        NetworkServer.Spawn(planet);
        activePlanets.Add(planet);
        
        // Log spawn for debugging
        Debug.Log($"PlanetSpawner: Spawned planet at {spawnPos}");
    }
    
    private Vector3 GetValidSpawnPosition()
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate random position within bounds
            float x = Random.Range(-spawnBounds.x, spawnBounds.x);
            float y = Random.Range(-spawnBounds.y, spawnBounds.y);
            Vector3 pos = new Vector3(x, y, 0);
            
            // Check if position is valid
            if (IsValidPosition(pos))
            {
                return pos;
            }
        }
        return Vector3.zero;
    }
    
    private bool IsValidPosition(Vector3 position)
    {
        // Check distance from other planets
        foreach (GameObject planet in activePlanets)
        {
            if (planet != null)
            {
                float distance = Vector3.Distance(position, planet.transform.position);
                if (distance < minSpawnDistance)
                {
                    return false;
                }
            }
        }
        
        // Check distance from center
        if (centerPoint != null)
        {
            float distanceFromCenter = Vector3.Distance(position, centerPoint.position);
            if (distanceFromCenter > maxDistanceFromCenter)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void CleanupFarPlanets()
    {
        if (centerPoint == null) return;
        
        for (int i = activePlanets.Count - 1; i >= 0; i--)
        {
            GameObject planet = activePlanets[i];
            if (planet != null)
            {
                float distance = Vector3.Distance(planet.transform.position, centerPoint.position);
                if (distance > maxDistanceFromCenter)
                {
                    NetworkServer.Destroy(planet);
                    activePlanets.RemoveAt(i);
                }
            }
        }
    }
    
    // Called when a planet is captured by a sun
    public void OnPlanetCaptured(GameObject planet)
    {
        activePlanets.Remove(planet);
    }
} 