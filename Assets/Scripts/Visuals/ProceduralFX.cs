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
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
            main.startColor = new Color(1f, 1f, 1f, 0.15f); // More transparent, but more particles
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.playOnAwake = false;
            // Prewarm to look like it's been cooking if started late
            // main.prewarm = true; 

            var emission = ps.emission;
            emission.rateOverTime = 20f; // More dense steam

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.2f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.1f;
            noise.frequency = 0.5f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(0.3f, 0.3f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.5f);
            curve.AddKey(1.0f, 2.0f); // Grow as it rises
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
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor = new Color(1f, 0.9f, 0.6f, 0.8f); // Oily yellow
            main.gravityModifier = 1.0f;
            main.playOnAwake = false;
            main.loop = false;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 5, 10) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Assign a default material to avoid Pink Box
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            
            return ps;
        }


}
}
