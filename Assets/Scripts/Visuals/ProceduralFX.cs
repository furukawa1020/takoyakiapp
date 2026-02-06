using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    /// <summary>
    /// Procedural particle effects for takoyaki cooking
    /// Supports object pooling for better performance
    /// </summary>
    public static class ProceduralFX
    {
        public static ParticleSystem CreateSteamFX(Transform parent)
        {
            GameObject obj = new GameObject("FX_Steam");
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.Euler(-90, 0, 0); // Upwards

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            
            // Fix Pink Box
            // Fix Pink Box
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default")); 
            
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.0f); // Even longer life
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.5f); // Varied speed for depth
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.0f); // Bigger steam clouds
            main.startColor = new Color(1f, 1f, 1f, 0.3f); // More visible
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 200; // Increased to prevent premature culling with 80 emission rate

            var emission = ps.emission;
            emission.rateOverTime = 80f; // Higher density for more dramatic effect

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 20f; // More focused steam column
            shape.radius = 0.25f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.7f; // Even more turbulence for realism
            noise.frequency = 1.0f;
            noise.scrollSpeed = 0.5f; // Add movement to noise
            noise.damping = true;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.4f, 0.2f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.3f);
            curve.AddKey(0.5f, 1.5f);
            curve.AddKey(1.0f, 3.5f); // Expands even more significantly
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            
            // Add velocity over lifetime for upward movement
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);

            return ps;
        }

        public static ParticleSystem CreateOilSplatterFX(Transform parent)
        {
            GameObject obj = new GameObject("FX_OilSplatter");
            obj.transform.SetParent(parent);
            obj.transform.localPosition = Vector3.zero;

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6.0f); // More dramatic splash
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f); // Varied droplet sizes
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.5f, 0.9f),
                new Color(1f, 0.85f, 0.4f, 1f)
            ); // Golden oil color variation
            main.gravityModifier = 2.5f; // Heavier for realistic drop
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 50; // Performance limit

            var emission = ps.emission;
            emission.rateOverTime = 0;
            // Bigger burst for more impact
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 25, 50) }); 

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere; // Spray upwards/outwards
            shape.radius = 0.25f;
            shape.radiusThickness = 0.5f; // More concentrated at edges
            
            // Add color over lifetime for fade effect
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0.0f), 
                    new GradientColorKey(new Color(1f, 0.85f, 0.4f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = grad;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            
            return ps;
        }


}
}
