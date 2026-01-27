using System.Numerics;

namespace Takoyaki.Core
{
    public static class Constants
    {
        // Physics
        public const float Gravity = -9.81f;
        public const float BallRadius = 0.02f; // 2cm radius
        public const float HoleDepth = 0.025f;
        
        // Cooking
        public const float IdealTemperature = 180f; // Celsius
        public const float AmbientTemperature = 25f;
        public const float HeatTransferRate = 5.0f;
    }

    public static class MathCode
    {
        // Unity-like Math helper for easy porting
        public static float Clamp01(float value) => System.Math.Clamp(value, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        
        public static Vector3 LerpVector(Vector3 a, Vector3 b, float t)
        {
            t = Clamp01(t);
            return new Vector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }
    }
}
