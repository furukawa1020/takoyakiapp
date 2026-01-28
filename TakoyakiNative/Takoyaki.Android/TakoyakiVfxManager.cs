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
        private readonly ShapingSparkles _sparkles;
        private float _steamIntensity;

        public TakoyakiVfxManager(Context context)
        {
            _steam = new SteamParticles(context);
            _sparkles = new ShapingSparkles(context);
        }

        public void Update(float dt, float cookLevel, float mastery)
        {
            // Steam intensity depends on cook level
            float targetIntensity = 0;
            if (cookLevel > 0.3f)
            {
                targetIntensity = (cookLevel - 0.3f) * 1.5f;
            }
            
            _steamIntensity = MathHelper.Lerp(_steamIntensity, targetIntensity, dt * 2.0f);
            _steam.Update(dt, _steamIntensity);
            
            _sparkles.Update(dt, mastery);
        }

        public void Draw(float[] mvpMatrix)
        {
            _steam.Draw(mvpMatrix);
            _sparkles.Draw(mvpMatrix);
        }
        
        public void TriggerServeSplash()
        {
            // Future: Implementation of specialized particles for serving
        }
    }
}
