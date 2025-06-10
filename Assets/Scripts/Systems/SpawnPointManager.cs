using UnityEngine;
using Mirror;

public class SpawnPointManager : NetworkBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    private int currentSpawnIndex = 0;

    public Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // If no spawn points, return random position within bounds
            return new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                0f
            );
        }

        Vector3 position = spawnPoints[currentSpawnIndex].position;
        currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
        return position;
    }
} 