using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class LightParticleManager : NetworkBehaviour
{
    [SerializeField] private GameObject lightParticlePrefab;
    [SerializeField] private int maxParticles = 100;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnAreaSize = 40f;
    [SerializeField] private int initialParticleCount = 50;

    private Queue<GameObject> particlePool;
    private float nextSpawnTime;
    private int currentParticleCount;

    public override void OnStartServer()
    {
        base.OnStartServer();
        InitializePool();
        PopulateInitialParticles();
    }

    [Server]
    private void InitializePool()
    {
        particlePool = new Queue<GameObject>();
        for (int i = 0; i < maxParticles; i++)
        {
            CreateNewParticle();
        }
    }

    [Server]
    private void PopulateInitialParticles()
    {
        for (int i = 0; i < initialParticleCount; i++)
        {
            SpawnParticle();
        }
    }

    [Server]
    private void CreateNewParticle()
    {
        GameObject particle = Instantiate(lightParticlePrefab);
        particle.SetActive(false);
        NetworkServer.Spawn(particle);
        particlePool.Enqueue(particle);
    }

    [Server]
    private void SpawnParticle()
    {
        if (currentParticleCount >= maxParticles) return;
        if (particlePool.Count == 0) return;

        GameObject particle = particlePool.Dequeue();
        if (particle != null)
        {
            // Random position within spawn area
            float randomX = Random.Range(-spawnAreaSize/2, spawnAreaSize/2);
            float randomY = Random.Range(-spawnAreaSize/2, spawnAreaSize/2);
            
            particle.transform.position = new Vector3(randomX, randomY, 0);
            particle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            particle.SetActive(true);
            
            currentParticleCount++;
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (Time.time >= nextSpawnTime && currentParticleCount < maxParticles)
        {
            SpawnParticle();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    [Server]
    public void ReturnToPool(GameObject particle)
    {
        particle.SetActive(false);
        particlePool.Enqueue(particle);
        currentParticleCount--;
    }
} 