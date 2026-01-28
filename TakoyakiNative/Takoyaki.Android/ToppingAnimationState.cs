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
            // Katsuobushi (bonito flakes) are extremely sensitive to heat
            Vibrancy = Math.Max(Vibrancy, localHeat * 0.8f);
            Vibrancy *= (1.0f - dt * 1.5f); // Slightly slower dampening for more "lingering" dance
            
            float time = (float)(Java.Lang.JavaSystem.NanoTime() / 1_000_000_000.0) + _timeOffset;
            
            // MULTI-FREQUENCY NOISE (Fourier Approximation)
            // This creates the unpredictable, organic movement of real bonito flakes
            float wave1 = (float)Math.Sin(time * 12.0f);
            float wave2 = (float)Math.Sin(time * 23.0f + 1.5f);
            float wave3 = (float)Math.Sin(time * 7.0f * (1.0f + wave1 * 0.1f));
            
            float chaos = (wave1 * 0.5f + wave2 * 0.3f + wave3 * 0.2f);
            
            // Positioning (Bounce/jitter)
            float bounce = Math.Abs(chaos) * Vibrancy * 0.15f;
            CurrentOffset.Y = bounce + (ballWobble * 0.12f);
            
            // ROTATION "DANCE" (Chaotic flapping)
            // Flakes should look like they are curling and uncurling
            CurrentRotation.X = chaos * Vibrancy * 15.0f;
            CurrentRotation.Z = (wave2 - wave1) * Vibrancy * 10.0f;
            CurrentRotation.Y = wave3 * Vibrancy * 5.0f;
            
            // Heat causes shrinking/curling
            HeatReaction = Math.Min(1.0f, HeatReaction + localHeat * dt * 0.25f);
        }
    }
}
