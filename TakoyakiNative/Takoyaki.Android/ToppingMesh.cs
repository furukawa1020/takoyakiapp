using System;

namespace Takoyaki.Android
{
    /// <summary>
    /// Base class for a 3D mesh-based topping.
    /// Handles OpenGL buffer management and spatial properties.
    /// </summary>
    public class ToppingMesh
    {
        public float[] Vertices;
        public short[] Indices;
        public int VBO, IBO, IndexCount;
        
        public System.Numerics.Vector3 Position;
        public System.Numerics.Vector3 Scale = new System.Numerics.Vector3(1, 1, 1);
        public System.Numerics.Vector4 Color;
        
        public bool Visible;
        public float[] RotationMatrix; // 4x4 matrix for orientation relative to the ball
        
        public ToppingAnimationState Animation = new ToppingAnimationState();
    }
}
