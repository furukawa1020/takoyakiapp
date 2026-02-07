using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public class ParticleController : MonoBehaviour
    {
        [Header("Effects")]
        public ParticleSystem steamParticles;
        public ParticleSystem oilSplatterParticles;
        public ParticleSystem smokeParticles;

        private TakoyakiController _controller;

        private void Start()
        {
            _controller = GetComponent<TakoyakiController>();

            // Auto-generate if missing
            if (steamParticles == null)
            {
                steamParticles = ProceduralFX.CreateSteamFX(transform);
            }
            if (oilSplatterParticles == null)
            {
                oilSplatterParticles = ProceduralFX.CreateOilSplatterFX(transform);
            }
            // Smoke? We can reuse steam system with black color or create new.
            // For now, let's assume we tint steam if burning? Or use separate.
        }

        private void Update()
        {
            if (_controller == null) return;

            float cook = _controller.CookLevel;
            
            // 1. Steam Logic
            // Steam peaks during active cooking (0.2 to 0.9)
            // Drops off when done (1.0)
            if (steamParticles != null)
            {
                var emission = steamParticles.emission;
                
                if (cook > 0.1f && cook < 1.2f)
                {
                    // Bell curve peeking at 0.6
                    float intensity = Mathf.Sin(cook * Mathf.PI); 
                    emission.rateOverTime = intensity * 30f; // Max 30 particles
                    
                    if (!steamParticles.isPlaying) steamParticles.Play();
                }
                else
                {
                    emission.rateOverTime = Mathf.Lerp(emission.rateOverTime.constant, 0, Time.deltaTime * 2);
                    if (emission.rateOverTime.constant < 0.1f) steamParticles.Stop();
                }
            }

            // 2. Smoke Logic (Burnt)
            if (cook > 1.2f)
            {
                PlayBurntSmoke(true);
            }
        }

        public void PlaySteam(bool isPlaying)
        {
            // Manual control for steam particles
            if (steamParticles == null) return;
            
            if (isPlaying)
            {
                if (!steamParticles.isPlaying)
                {
                    steamParticles.Play();
                }
            }
            else
            {
                if (steamParticles.isPlaying)
                {
                    steamParticles.Stop();
                }
            }
        }

        public void PlayOilSplatter(Vector3 position)
        {
            if (oilSplatterParticles == null) return;
            
            oilSplatterParticles.transform.position = position;
            oilSplatterParticles.Play();
        }

        public void PlayBurntSmoke(bool isPlaying)
        {
             // Simple smoke implementation: Darken steam or separate system?
             // For now, let's just tint steam if we don't have separate smoke
             if (steamParticles != null)
             {
                 var main = steamParticles.main;
                 if (isPlaying) 
                 {
                     main.startColor = new Color(0.1f, 0.1f, 0.1f, 0.5f); // Black smoke
                     var emission = steamParticles.emission;
                     emission.rateOverTime = 50f; // Billowing
                     if (!steamParticles.isPlaying) steamParticles.Play();
                 }
                 else
                 {
                     main.startColor = new Color(1f, 1f, 1f, 0.15f); // Back to steam
                 }
             }
        }
    }
}
