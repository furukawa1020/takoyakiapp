using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.5f); // Longer life
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f); // Faster rise
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f); // Much bigger steam clouds
            main.startColor = new Color(1f, 1f, 1f, 0.2f); // Slightly more opaque
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 60f; // 3x Density (Dramatic!)

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f; // Wider spread
            shape.radius = 0.3f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f; // More turbulence
            noise.frequency = 0.8f;

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
            curve.AddKey(0.0f, 0.5f);
            curve.AddKey(1.0f, 3.0f); // Expands significantly
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

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
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.0f); // High velocity splash
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f); // Bigger droplets
            main.startColor = new Color(1f, 0.9f, 0.6f, 0.9f); 
            main.gravityModifier = 2.0f; // Heavy oil
            main.playOnAwake = false;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            // HUGE burst
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 20, 40) }); 

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere; // Spray upwards/outwards
            shape.radius = 0.2f;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            
            return ps;
        }
    }
}
