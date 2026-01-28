using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class TakoyakiShapingLogic
    {
        private PidController _pid;
        public float ShapingProgress { get; private set; } = 1.0f; // 1.0 = Bumpy, 0.0 = Perfectly Round
        public float MasteryLevel { get; private set; } = 0.0f; // 0..1 (How perfect the rhythm is)
        public float RhythmPulse { get; private set; } = 0.0f; // 0..1 (Pulse for visual guidance)
        
        // Settings for "The Rhythm of the Master"
        public const float TARGET_GYRO_MAG = 6.0f; // Target rotation speed (rad/s)
        private const float SHAPING_SPEED = 0.5f;
        private float _pulseTimer = 0.0f;

        public TakoyakiShapingLogic()
        {
            // Initial tuning for Pixel 8
            _pid = new PidController(0.8f, 0.2f, 0.1f);
        }

        public void Update(float dt, Vector3 angularVelocity)
        {
            float currentMag = angularVelocity.Length();
            
            // PID for "Mastery"
            float pidOutput = _pid.Update(TARGET_GYRO_MAG, currentMag, dt);
            
            // Rhythm Pulse (6 rad/s is ~1Hz)
            _pulseTimer += dt * (TARGET_GYRO_MAG / 6.0f);
            RhythmPulse = (float)Math.Sin(_pulseTimer * Math.PI * 2.0) * 0.5f + 0.5f;

            if (currentMag > 2.0f)
            {
                // Harmony: how close we are to the perfect PID target
                float harmony = Math.Clamp(1.0f - Math.Abs(pidOutput) / TARGET_GYRO_MAG, 0.0f, 1.0f);
                
                // Mastery grows if harmony is high
                if (harmony > 0.8f) MasteryLevel = Math.Min(1.0f, MasteryLevel + dt * 0.4f);
                else MasteryLevel = Math.Max(0.0f, MasteryLevel - dt * 0.3f);

                // Shaping pressure: Master's touch is faster
                float pressure = harmony * (1.0f + MasteryLevel); 
                ShapingProgress = Math.Max(0.0f, ShapingProgress - pressure * SHAPING_SPEED * dt);
            }
            else
            {
                MasteryLevel = Math.Max(0.0f, MasteryLevel - dt * 0.5f);
                ShapingProgress = Math.Min(1.0f, ShapingProgress + dt * 0.05f);
            }
        }
    }
}
