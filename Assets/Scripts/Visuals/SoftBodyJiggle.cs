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
            // This is a very basic simulation.
            // In a real scenario, we might use a Mass-Spring system or just a shader.
            // Using a simple Uniform shake for now based on velocity change.
            
            Vector3 acceleration = InputManager.Instance != null ? InputManager.Instance.CurrentAcceleration : Vector3.zero;
            
            for (int i = 0; i < _displacedVertices.Length; i++)
            {
                // Displacement from rest
                Vector3 displacement = _displacedVertices[i] - _originalVertices[i];
                
                // Spring force back to original
                Vector3 force = -elasticity * displacement;
                
                // Add external acceleration (shake) - simplified
                force += -acceleration * impactForce * 0.01f;

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
