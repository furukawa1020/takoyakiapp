using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class SoftBodySolver
    {
        // Physics Parameters (Tuned for "Juicy" Jiggle)
        public float Stiffness { get; set; } = 3.5f;
        public float Damping { get; set; } = 0.4f;
        public float Mass { get; set; } = 0.8f;
        public float GravityInfluence { get; set; } = 1.0f; // Sagging

        // Per-Vertex State
        private struct VertexState
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 OriginalLocalPos;
        }

        private VertexState[] _vertices;
        private TakoyakiBall _ballRef;

        public SoftBodySolver(TakoyakiBall ball)
        {
            _ballRef = ball;
            int count = ball.BaseVertices.Length;
            _vertices = new VertexState[count];

            for (int i = 0; i < count; i++)
            {
                _vertices[i].OriginalLocalPos = ball.BaseVertices[i];
                _vertices[i].Position = ball.BaseVertices[i];
                _vertices[i].Velocity = Vector3.Zero;
            }
        }

        public void Update(float dt, Vector3 worldAccel, Vector3 worldGravity)
        {
            // Transform World Gravity/Accel to Local Space of the ball
            // (Since vertices are simulated in local space relative to the ball center)
            Quaternion invRot = Quaternion.Inverse(_ballRef.Rotation);
            
            Vector3 localGravity = Vector3.Transform(worldGravity, invRot);
            Vector3 localAccel = Vector3.Transform(worldAccel, invRot); // Inertia force

            float dtRatio = dt * 60f; // Logically normalize to 60fps base if needed, or just use raw dt

            for (int i = 0; i < _vertices.Length; i++)
            {
                // 1. Spring Force (Hooke's Law)
                // Target is the Original Local Position (Memory shape)
                Vector3 displacement = _vertices[i].Position - _vertices[i].OriginalLocalPos;
                Vector3 springForce = -displacement * Stiffness;

                // 2. External Forces
                Vector3 force = springForce;
                
                // Gravity Sag (Vertices droop down in local space)
                force += localGravity * GravityInfluence;

                // Inertia (Opposite to acceleration)
                // If ball accelerates RIGHT, inertia acts LEFT on vertices
                force -= localAccel * Mass; 

                // 3. Integration (Verlet/Euler)
                Vector3 acceleration = force / Mass;
                _vertices[i].Velocity += acceleration * dt;
                
                // Damping
                _vertices[i].Velocity *= MathF.Pow(1.0f - Damping, dtRatio);

                // Update Pos
                _vertices[i].Position += _vertices[i].Velocity * dt;

                // 4. Update the Data Model for Rendering
                _ballRef.DeformedVertices[i] = _vertices[i].Position;
            }
        }

        public void TriggerJiggle(float strength)
        {
            var random = new Random();
            for (int i = 0; i < _vertices.Length; i++)
            {
                // Radial burst + Noise
                Vector3 dir = Vector3.Normalize(_vertices[i].Position); // Center to vertex
                float noise = (float)random.NextDouble() * 1.5f + 0.5f;
                _vertices[i].Velocity += dir * strength * noise;
            }
        }
    }
}
