using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    [RequireComponent(typeof(MeshFilter))]
    public class SoftBodyJiggle : MonoBehaviour
    {
        [Header("Jiggle Settings")]
        [SerializeField] private float elasticity = 0.1f;
        [SerializeField] private float damping = 0.1f;
        [SerializeField] private float impactForce = 1.0f;

        private Mesh _originalMesh;
        private Mesh _workingMesh;
        private Vector3[] _originalVertices;
        private Vector3[] _displacedVertices;
        private Vector3[] _vertexVelocities;

        private void Start()
        {
            _originalMesh = GetComponent<MeshFilter>().sharedMesh;
            
            // Create a clone for manipulation
            _workingMesh = Instantiate(_originalMesh);
            GetComponent<MeshFilter>().mesh = _workingMesh;

            _originalVertices = _workingMesh.vertices;
            _displacedVertices = new Vector3[_originalVertices.Length];
            _vertexVelocities = new Vector3[_originalVertices.Length];

            System.Array.Copy(_originalVertices, _displacedVertices, _originalVertices.Length);
        }

        private void Update()
        {
            // Simple spring simulation per vertex (concept only for proto)
            // Ideally, we only simulate generic "wobble" via a few control points or shader
            // For CPU vertex manipulation:
            
            UpdateSpringPhysics();
            
            _workingMesh.vertices = _displacedVertices;
            _workingMesh.RecalculateNormals();
        }

        private void UpdateSpringPhysics()
        {
            // Improved "Organic" Jiggle using Perlin Noise + Velocity
            
            float time = Time.time * 5.0f;
            Vector3 acceleration = InputManager.Instance != null ? InputManager.Instance.CurrentAcceleration : Vector3.zero;
            
            for (int i = 0; i < _displacedVertices.Length; i++)
            {
                // 1. Spring Force (Return to rest)
                Vector3 displacement = _displacedVertices[i] - _originalVertices[i];
                Vector3 force = -elasticity * displacement;
                
                // 2. Input Force (Shake/Tilt)
                // We project the acceleration onto the vertex normal to make it puff out/in
                // or just apply raw direction.
                force += -acceleration * impactForce * 0.002f;

                // 3. Perlin Noise for "Boiling/Sizzling" effect
                // Using vertex position as scale for noise
                float noise = Mathf.PerlinNoise(_originalVertices[i].x * 10f + time, _originalVertices[i].z * 10f + time) - 0.5f;
                force += _originalMesh.normals[i] * noise * 0.05f * impactForce;

                // Integrate
                _vertexVelocities[i] += force;
                _vertexVelocities[i] *= (1.0f - damping);
                _displacedVertices[i] += _vertexVelocities[i];
            }
        }
        
        public void ApplyImpulse(Vector3 force)
        {
             for (int i = 0; i < _vertexVelocities.Length; i++)
             {
                 _vertexVelocities[i] += force * Random.Range(0.5f, 1.0f); // Add noise
             }
        }
    }
}
