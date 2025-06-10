using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.Linq;

public class OrbitManager : NetworkBehaviour
{
    [SerializeField] private float baseOrbitRadius = 2f;
    [SerializeField] private float baseOrbitSpeed = 50f;
    [SerializeField] private float orbitSpacing = 0.5f;
    [SerializeField] private float massOrbitMultiplier = 0.1f;

    private SunController sunController;
    private List<Transform> orbitingPlanets = new List<Transform>();
    private Dictionary<Transform, int> planetOrbitIndices = new Dictionary<Transform, int>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        sunController = GetComponent<SunController>();
    }

    [Server]
    public void AddPlanetToOrbit(Transform planet)
    {
        if (!isServer) return;

        // Find the next available orbit index
        int orbitIndex = GetNextAvailableOrbitIndex();
        
        // Add planet to our tracking
        orbitingPlanets.Add(planet);
        planetOrbitIndices[planet] = orbitIndex;

        // Notify clients about the new planet
        RpcAddPlanetToOrbit(planet.gameObject, orbitIndex);
    }

    [Server]
    public void RemovePlanetFromOrbit(Transform planet)
    {
        if (!isServer) return;

        orbitingPlanets.Remove(planet);
        planetOrbitIndices.Remove(planet);

        // Notify clients about the removed planet
        RpcRemovePlanetFromOrbit(planet.gameObject);
    }

    [ClientRpc]
    private void RpcAddPlanetToOrbit(GameObject planet, int orbitIndex)
    {
        Transform planetTransform = planet.transform;
        orbitingPlanets.Add(planetTransform);
        planetOrbitIndices[planetTransform] = orbitIndex;
    }

    [ClientRpc]
    private void RpcRemovePlanetFromOrbit(GameObject planet)
    {
        Transform planetTransform = planet.transform;
        orbitingPlanets.Remove(planetTransform);
        planetOrbitIndices.Remove(planetTransform);
    }

    private int GetNextAvailableOrbitIndex()
    {
        int index = 0;
        while (planetOrbitIndices.Values.Contains(index))
        {
            index++;
        }
        return index;
    }

    void Update()
    {
        if (sunController == null) return;

        float sunMass = sunController.GetMass();
        
        foreach (Transform planet in orbitingPlanets)
        {
            if (planet == null) continue;

            int orbitIndex = planetOrbitIndices[planet];
            
            // Calculate orbit parameters based on mass and index
            float orbitRadius = baseOrbitRadius + (orbitIndex * orbitSpacing) + (sunMass * massOrbitMultiplier);
            float orbitSpeed = baseOrbitSpeed / (orbitIndex + 1); // Slower orbits for outer planets
            
            // Calculate the target position
            float angle = Time.time * orbitSpeed;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * orbitRadius,
                Mathf.Sin(angle) * orbitRadius,
                0
            );
            
            // Smoothly move the planet to its orbit position
            planet.position = Vector3.Lerp(
                planet.position,
                transform.position + offset,
                Time.deltaTime * 5f
            );
        }
    }

    public int GetPlanetCount()
    {
        return orbitingPlanets.Count;
    }

    public float GetOrbitRadius(int orbitIndex)
    {
        float sunMass = sunController != null ? sunController.GetMass() : 1f;
        return baseOrbitRadius + (orbitIndex * orbitSpacing) + (sunMass * massOrbitMultiplier);
    }
}
