using System.Numerics;

namespace Takoyaki.Core
{
    /// <summary>
    /// Snapshot of current input frame.
    /// Normalized values from sensors or touch.
    /// </summary>
    public struct InputState
    {
        // Gyro/Accumulated Tilt (X, Z plane usually)
        public Vector2 Tilt; 
        
        // Instantaneous acceleration (for Shake detection)
        public Vector3 Acceleration; 
        
        // User Actions
        public bool IsTap;
        public Vector2 TapPosition; // Screen normalized 0..1
        public bool IsSwipe;
        public Vector2 SwipeDelta;
        
        // Helper to reset ephemeral flags
        public void ClearEvents()
        {
            IsTap = false;
            IsSwipe = false;
        }
    }
}
