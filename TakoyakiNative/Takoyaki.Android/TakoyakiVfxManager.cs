using System;
using Android.Content;
using Android.Opengl;
using Takoyaki.Core;

namespace Takoyaki.Android
{
    /// <summary>
    /// Specialized manager for visual effects (VFX).
    /// Handles particles, steam, and environmental glow.
    /// </summary>
    public class TakoyakiVfxManager
    {
        private readonly SteamParticles _steam;
        private float _steamIntensity;

        public TakoyakiVfxManager(Context context)
        {
            _steam = new SteamParticles(context);
        }

        public void Update(float dt, float cookLevel, float temperature)
        {
            // Steam intensity depends on cook level and heat
            float targetIntensity = 0;
            if (cookLevel > 0.3f)
            {
                targetIntensity = (cookLevel - 0.3f) * 1.5f;
            }
            
            _steamIntensity = MathHelper.Lerp(_steamIntensity, targetIntensity, dt * 2.0f);
            _steam.Update(dt, _steamIntensity);
        }

        public void Draw(float[] mvpMatrix)
        {
            // Additive blending for steam usually handled inside SteamParticles
            _steam.Draw(mvpMatrix);
        }
        
        public void TriggerServeSplash()
        {
            // Future: Implementation of specialized particles for serving
        }
    }
}
