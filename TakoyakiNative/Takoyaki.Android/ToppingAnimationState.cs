using System;
using System.Numerics;

namespace Takoyaki.Android
{
    /// <summary>
    /// Tracks the dynamic animation state for a single topping instance.
    /// Allows for fine-grained control over how each flake "dances" or "reacts".
    /// </summary>
    public class ToppingAnimationState
    {
        public Vector3 CurrentOffset;
        public Vector3 CurrentRotation;
        public float Vibrancy; // 0..1 (How much it's dancing)
        public float HeatReaction; // 0..1 (How much it's curled by heat)
        
        private float _timeOffset;
        private static Random _rand = new Random();

        public ToppingAnimationState()
        {
            _timeOffset = (float)_rand.NextDouble() * 100f;
        }

        public void Update(float dt, float localHeat, float ballWobble)
        {
            // Dancing logic: High heat + random jitter
            Vibrancy = Math.Max(Vibrancy, localHeat * 0.5f);
            Vibrancy *= (1.0f - dt * 2.0f); // Dampen
            
            float time = (float)(Java.Lang.JavaSystem.NanoTime() / 1_000_000_000.0) + _timeOffset;
            
            // Random jitter for flakes
            float jitter = (float)Math.Sin(time * 15.0f) * Vibrancy * 0.1f;
            CurrentOffset.Y = jitter + (ballWobble * 0.1f); 
            
            // Rotation "dance"
            CurrentRotation.X = (float)Math.Sin(time * 8.0f) * Vibrancy * 5.0f;
            CurrentRotation.Z = (float)Math.Cos(time * 6.0f) * Vibrancy * 5.0f;
            
            // Heat causes shrinking/curling
            HeatReaction = Math.Min(1.0f, HeatReaction + localHeat * dt * 0.1f);
        }
    }
}
