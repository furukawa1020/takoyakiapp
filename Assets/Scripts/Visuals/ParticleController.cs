using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public class ParticleController : MonoBehaviour
    {
        [Header("Effects")]
        public ParticleSystem steamParticles;
        public ParticleSystem oilSplatterParticles;
        public ParticleSystem smokeParticles;

        public void PlaySteam(bool isPlaying)
        {
            if (steamParticles == null) return;
            
            if (isPlaying && !steamParticles.isPlaying) steamParticles.Play();
            else if (!isPlaying && steamParticles.isPlaying) steamParticles.Stop();
        }

        public void PlayOilSplatter(Vector3 position)
        {
            if (oilSplatterParticles == null) return;
            
            oilSplatterParticles.transform.position = position;
            oilSplatterParticles.Play();
        }

        public void PlayBurntSmoke(bool isPlaying)
        {
             if (smokeParticles == null) return;
            
             if (isPlaying && !smokeParticles.isPlaying) smokeParticles.Play();
             else if (!isPlaying && smokeParticles.isPlaying) smokeParticles.Stop();
        }
    }
}
