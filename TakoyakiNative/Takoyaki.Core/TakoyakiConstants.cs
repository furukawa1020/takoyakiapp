namespace Takoyaki.Core
{
    public static class TakoyakiConstants
    {
        // Physics & Soft Body
        public const float WOBBLE_SPEED = 6.0f;
        public const float WOBBLE_STRENGTH = 0.025f;
        public const float JIGGLE_STIFFNESS = 0.1f;
        
        // Rendering - Ball
        public const float BALL_BASE_RADIUS = 1.0f;
        public const float BALL_DISPLACEMENT_STRENGTH = 0.1f;
        
        // Rendering - Toppings
        public const float TOPPING_OFFSET_SAUCE = 1.01f;
        public const float TOPPING_OFFSET_MAYO = 1.025f;
        public const float TOPPING_OFFSET_AONORI = 1.02f;
        public const float TOPPING_OFFSET_KATSUO = 1.04f;
        
        // Color Palettes
        public static readonly System.Numerics.Vector4 COLOR_SAUCE = new System.Numerics.Vector4(0.3f, 0.15f, 0.05f, 0.90f);
        public static readonly System.Numerics.Vector4 COLOR_MAYO = new System.Numerics.Vector4(1.0f, 0.98f, 0.85f, 1.0f);
        public static readonly System.Numerics.Vector4 COLOR_AONORI = new System.Numerics.Vector4(0.1f, 0.5f, 0.1f, 1.0f);
        public static readonly System.Numerics.Vector4 COLOR_KATSUO = new System.Numerics.Vector4(0.75f, 0.55f, 0.40f, 0.9f);
        
        // Interaction
        public const float SHAKE_THRESHOLD = 8.0f;
        public const int SHAKE_COOLDOWN_MS = 500;
        
        // Game States
        public const float COOK_SPEED = 0.12f;
        public const float OVERCOOK_THRESHOLD = 0.85f;
    }
}
