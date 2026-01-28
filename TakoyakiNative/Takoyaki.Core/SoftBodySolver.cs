using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class SoftBodySolver
    {
        // Physics Parameters
        public float Stiffness { get; set; } = 3.5f;
        public float Damping { get; set; } = 0.4f;
        public float Mass { get; set; } = 0.8f;
        public float GravityInfluence { get; set; } = 1.0f;

        // Per-Vertex State (Layout must match Rust VertexState)
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct VertexState
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 OriginalLocalPos;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NativePhysicsParams
        {
            public float Stiffness;
            public float Damping;
            public float Mass;
            public float GravityInfluence;
        }

        private VertexState[] _vertices;
        private TakoyakiBall _ballRef;
        private IntPtr _nativeEngine = IntPtr.Zero;

        public SoftBodySolver(TakoyakiBall ball, IntPtr nativeEngine = default)
        {
            _ballRef = ball;
            _nativeEngine = nativeEngine;
            int count = ball.BaseVertices.Length;
            _vertices = new VertexState[count];

            for (int i = 0; i < count; i++)
            {
                _vertices[i].OriginalLocalPos = ball.BaseVertices[i];
                _vertices[i].Position = ball.BaseVertices[i];
                _vertices[i].Velocity = Vector3.Zero;
            }
        }

        public unsafe void Update(float dt, Vector3 worldAccel, Vector3 worldGravity)
        {
            Quaternion invRot = Quaternion.Inverse(_ballRef.Rotation);
            Vector3 localGravity = Vector3.Transform(worldGravity, invRot);
            Vector3 localAccel = Vector3.Transform(worldAccel, invRot);

            if (_nativeEngine != IntPtr.Zero)
            {
                // 🔥 NATIVE RUST PATH: High-Performance Simulation
                var p = new NativePhysicsParams { 
                    Stiffness = Stiffness, 
                    Damping = Damping, 
                    Mass = Mass, 
                    GravityInfluence = GravityInfluence 
                };
                
                fixed (VertexState* pStates = _vertices)
                {
                    TakoyakiShapingLogic.tako_step_physics(_nativeEngine, pStates, _vertices.Length, &p, &localGravity, &localAccel, dt);
                }
                
                // Sync back to ball model
                for (int i = 0; i < _vertices.Length; i++)
                {
                    _ballRef.DeformedVertices[i] = _vertices[i].Position;
                }
                return;
            }

            // Fallback C# Path
            float dtRatio = dt * 60f;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 displacement = _vertices[i].Position - _vertices[i].OriginalLocalPos;
                Vector3 force = -displacement * Stiffness + localGravity * GravityInfluence - localAccel * Mass;
                Vector3 acceleration = force / Mass;
                _vertices[i].Velocity += acceleration * dt;
                _vertices[i].Velocity *= MathF.Pow(1.0f - Damping, dtRatio);
                _vertices[i].Position += _vertices[i].Velocity * dt;

                Vector3 currentDisplacement = _vertices[i].Position - _vertices[i].OriginalLocalPos;
                if (currentDisplacement.LengthSquared() > 0.09f)
                {
                    currentDisplacement = Vector3.Normalize(currentDisplacement) * 0.3f;
                    _vertices[i].Position = _vertices[i].OriginalLocalPos + currentDisplacement;
                    _vertices[i].Velocity *= 0.1f;
                }
                _ballRef.DeformedVertices[i] = _vertices[i].Position;
            }
        }

        public void TriggerJiggle(float strength)
        {
            var random = new Random();
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 dir = Vector3.Normalize(_vertices[i].Position);
                float noise = (float)random.NextDouble() * 1.5f + 0.5f;
                _vertices[i].Velocity += dir * strength * noise;
            }
        }
    }
}
