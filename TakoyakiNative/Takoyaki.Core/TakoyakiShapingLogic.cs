using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class TakoyakiShapingLogic
    {
        private PidController _pid;
        public float ShapingProgress { get; private set; } = 1.0f; 
        public float MasteryLevel { get; private set; } = 0.0f;
        public float RhythmPulse { get; private set; } = 0.0f;
        public int ComboCount { get; private set; } = 0;
        public bool IsPerfect { get; private set; } = false;
        public bool TriggerHapticTick { get; set; } = false; // Flag for renderer
        
        public const float TARGET_GYRO_MAG = 6.0f;
        private const float SHAPING_SPEED = 0.4f;
        private float _pulseTimer = 0.0f;
        private float _stabilityTimer = 0.0f;

        public TakoyakiShapingLogic()
        {
            _pid = new PidController(1.2f, 0.3f, 0.15f); // Sharper tuning
        }

        public void Update(float dt, Vector3 angularVelocity)
        {
            float currentMag = angularVelocity.Length();
            float pidOutput = _pid.Update(TARGET_GYRO_MAG, currentMag, dt);
            
            _pulseTimer += dt * (TARGET_GYRO_MAG / (float)Math.PI); 
            float lastPulse = RhythmPulse;
            RhythmPulse = (float)Math.Abs(Math.Sin(_pulseTimer)); 

            // Trigger haptic on the "beat" (peak of sine)
            if (RhythmPulse > 0.9f && lastPulse <= 0.9f && currentMag > 1.0f) {
                TriggerHapticTick = true;
            }

            if (currentMag > 2.0f)
            {
                float harmony = Math.Clamp(1.0f - Math.Abs(pidOutput) / TARGET_GYRO_MAG, 0.0f, 1.0f);
                IsPerfect = harmony > 0.85f;

                if (IsPerfect) {
                    _stabilityTimer += dt;
                    if (_stabilityTimer > 0.5f) {
                        ComboCount++;
                        _stabilityTimer = 0.0f;
                    }
                    MasteryLevel = Math.Min(1.0f, MasteryLevel + dt * 0.5f);
                } else {
                    _stabilityTimer = 0.0f;
                    if (harmony < 0.5f) ComboCount = 0;
                    MasteryLevel = Math.Max(0.0f, MasteryLevel - dt * 0.2f);
                }

                float pressure = harmony * (1.0f + MasteryLevel * 2.0f); 
                ShapingProgress = Math.Max(0.0f, ShapingProgress - pressure * SHAPING_SPEED * dt);
            }
            else
            {
                ComboCount = 0;
                MasteryLevel = Math.Max(0.0f, MasteryLevel - dt * 1.0f);
                ShapingProgress = Math.Min(1.0f, ShapingProgress + dt * 0.05f);
            }
        }
    }
}
