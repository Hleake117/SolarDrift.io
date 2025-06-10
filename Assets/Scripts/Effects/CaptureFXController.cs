using UnityEngine;

public class CaptureFXController : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogError("CaptureFXController: No ParticleSystem found on GameObject!");
            return;
        }

        try
        {
            mainModule = ps.main;
            emissionModule = ps.emission;
            shapeModule = ps.shape;

            // Set up basic particle system
            mainModule.startSize = 0.3f;
            mainModule.startSpeed = 3f;
            mainModule.startLifetime = 1.5f;
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.gravityModifier = 0f;

            // Set up emission
            emissionModule.rateOverTime = 0;
            emissionModule.SetBurst(0, new ParticleSystem.Burst(0f, 50));

            // Set up shape
            shapeModule.shapeType = ParticleSystemShapeType.Circle;
            shapeModule.radius = 0.6f;
            shapeModule.radiusThickness = 0f;

            // Set up renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingOrder = 1;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CaptureFXController: Error setting up particle system: {e.Message}");
        }
    }

    public void ConfigureEffect(int particleCount, float particleSize, float particleSpeed, float particleLifetime)
    {
        if (ps == null) return;

        try
        {
            // Configure main module
            mainModule.startSize = particleSize;
            mainModule.startSpeed = particleSpeed;
            mainModule.startLifetime = particleLifetime;
            
            // Configure emission
            emissionModule.rateOverTime = 0;
            emissionModule.SetBurst(0, new ParticleSystem.Burst(0f, particleCount));
            
            // Configure shape
            shapeModule.radius = particleSize * 2f;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CaptureFXController: Error configuring effect: {e.Message}");
        }
    }

    public void PlayEffect(Color sunColor)
    {
        if (ps == null)
        {
            Debug.LogError("CaptureFXController: Cannot play effect - ParticleSystem is null!");
            return;
        }

        try
        {
            // Create a glowing version of the sun's color
            Color glowColor = Color.Lerp(sunColor, Color.white, 0.3f);
            mainModule.startColor = new ParticleSystem.MinMaxGradient(glowColor);
            
            // Add simple trail effect using particle system
            var trails = ps.trails;
            trails.enabled = true;
            trails.ratio = 0.5f;
            trails.lifetime = 0.5f;
            trails.minVertexDistance = 0.1f;
            trails.widthOverTrail = 0.5f;
            trails.colorOverTrail = new ParticleSystem.MinMaxGradient(glowColor, Color.clear);
            
            ps.Play();

            // Destroy after the effect is done
            float duration = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(gameObject, duration);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CaptureFXController: Error playing effect: {e.Message}");
            Destroy(gameObject); // Clean up if something goes wrong
        }
    }
} 