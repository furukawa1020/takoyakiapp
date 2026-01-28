using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class TakoyakiShapingLogic
    {
        private PidController _pid;
        public float ShapingProgress { get; private set; } = 1.0f; // 1.0 = Bumpy, 0.0 = Perfectly Round
        
        // Settings for "The Rhythm of the Master"
        private const float TARGET_GYRO_MAG = 6.0f; // Target rotation speed (rad/s)
        private const float SHAPING_SPEED = 0.5f;

        public TakoyakiShapingLogic()
        {
            // Initial tuning for Pixel 8
            _pid = new PidController(0.8f, 0.2f, 0.1f);
        }

        public void Update(float dt, Vector3 angularVelocity)
        {
            float currentMag = angularVelocity.Length();
            
            // We use PID to calculate how close the user is to the "ideal" shaping rhythm
            // The output of PID tells us if we should "smooth" or "roughen"
            float pidOutput = _pid.Update(TARGET_GYRO_MAG, currentMag, dt);
            
            // If user is rotating at a good speed, pidOutput will be positive/stable.
            // If the user is rotating correctly, we reduce ShapingProgress (towards 0 = Round)
            if (currentMag > 2.0f)
            {
                // Shaping pressure: proportional to how well they match the rhythm
                float pressure = Math.Clamp(1.0f - Math.Abs(pidOutput) / TARGET_GYRO_MAG, 0.0f, 1.0f);
                ShapingProgress = Math.Max(0.0f, ShapingProgress - pressure * SHAPING_SPEED * dt);
            }
            else
            {
                // Regrow bumps if not active
                ShapingProgress = Math.Min(1.0f, ShapingProgress + dt * 0.1f);
            }
        }
    }
}
