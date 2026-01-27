using System.Numerics;

namespace Takoyaki.Core
{
    /// <summary>
    /// Represents the physical and simulated state of a single Takoyaki ball.
    /// Pure C# - No Unity dependencies.
    /// </summary>
    public class TakoyakiBall
    {
        public int Id { get; private set; }
        
        // Physics State
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        
        // Soft Body State (Simplified for Core, Rendering will expand)
        public Vector3[] DeformedVertices; 
        public Vector3[] BaseVertices; // Perfect sphere reference
        
        // Cooking State
        public float CookLevel; // 0.0 (Raw) -> 1.0 (Perfect) -> 2.0 (Burnt)
        public float BatterLevel; // 0.0 (Empty) -> 1.0 (Full)
        public float Temperature; // Celsius
        
        // Directional Cooking (Heatmap)
        // 6 directions: Up, Down, Left, Right, Forward, Back
        public float[] SurfaceCookLevels = new float[6]; 

        // Gameplay State
        public bool IsInHole; // True if sitting in a pan hole
        public int CurrentHoleIndex; // -1 if undefined

        public TakoyakiBall(int id, int vertexCount)
        {
            Id = id;
            Rotation = Quaternion.Identity;
            BaseVertices = new Vector3[vertexCount];
            DeformedVertices = new Vector3[vertexCount];
        }
    }
}
