using UnityEngine;
using TakoyakiPhysics;

namespace TakoyakiPhysics.Visuals
{
    [RequireComponent(typeof(MeshFilter))]
    public class TakoyakiSoftBody : MonoBehaviour
    {
        [Header("Mass-Spring Settings")]
        [SerializeField] private float intensity = 1f;
        [SerializeField] private float mass = 1f;
        [SerializeField] private float stiffness = 5f; // Spring constant
        [SerializeField] private float damping = 0.75f;
        
        [Header("External Forces")]
        [SerializeField] private float gravityInfluence = 1f; // How much internal gravity affects shape (Sagging)
        [SerializeField] private float inertiaScale = 1f;

        private Mesh _originalMesh;
        private Mesh _workingMesh;
        
        // Structure to hold vertex physics data
        private struct VertexSpring
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 BasePosition; // Position relative to transform
        }

        private VertexSpring[] _physicsVertices;
        private Vector3[] _meshVertices;
        private Vector3 _lastWorldPosition;
        private Quaternion _lastWorldRotation;

        private void Start()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) return;
            
            _originalMesh = mf.sharedMesh;
            if (_originalMesh == null) 
            {
                Debug.LogError("[TakoyakiSoftBody] Mesh is null! Soft body physics cannot run.");
                return;
            }

            _workingMesh = Instantiate(_originalMesh);
            mf.mesh = _workingMesh;

            Vector3[] originalVerts = _originalMesh.vertices;
            _meshVertices = new Vector3[originalVerts.Length];
            _physicsVertices = new VertexSpring[originalVerts.Length];

            for (int i = 0; i < originalVerts.Length; i++)
            {
                _physicsVertices[i].BasePosition = originalVerts[i];
                _physicsVertices[i].Position = originalVerts[i];
                _physicsVertices[i].Velocity = Vector3.zero;
                _meshVertices[i] = originalVerts[i];
            }

            _lastWorldPosition = transform.position;
            _lastWorldRotation = transform.rotation;
        }

        private void Update()
        {
            if (_physicsVertices == null) return; // Safety check
            
            UpdatePhysics(Time.deltaTime);
            
            // Map physics positions back to mesh
            for (int i = 0; i < _physicsVertices.Length; i++)
            {
                _meshVertices[i] = _physicsVertices[i].Position;
            }
            
            _workingMesh.vertices = _meshVertices;
            _workingMesh.RecalculateNormals();
            _workingMesh.RecalculateBounds();
        }

        private void UpdatePhysics(float dt)
        {
            // Calculate Inertia (Object movement in world space)
            Vector3 worldPos = transform.position;
            Quaternion worldRot = transform.rotation;

            Vector3 deltaMove = worldPos - _lastWorldPosition;
             // Simplified angular velocity approximation
            Quaternion deltaRot = Quaternion.Inverse(_lastWorldRotation) * worldRot; 
            
            _lastWorldPosition = worldPos;
            _lastWorldRotation = worldRot;

            // Global "Down" in local space for gravity sag
            Vector3 localGravity = transform.InverseTransformDirection(Physics.gravity).normalized;

            for (int i = 0; i < _physicsVertices.Length; i++)
            {
                // 1. Target Position (where this vertex WANTS to be)
                Vector3 targetParam = _physicsVertices[i].BasePosition;
                
                // 2. Current State
                Vector3 currentPos = _physicsVertices[i].Position;
                Vector3 vel = _physicsVertices[i].Velocity;

                // 3. Inertial Force (Opposite to movement)
                // Transform world movement into local space impact
                Vector3 localInertia = transform.InverseTransformDirection(deltaMove) / dt; 
                
                // 4. Spring Force (Hooke's Law): F = -k * x
                Vector3 displacement = currentPos - targetParam;
                Vector3 springForce = -displacement * stiffness;

                // 5. Total Force
                Vector3 force = springForce;
                
                // Add Gravity Sag (Soft body droops down)
                force += localGravity * gravityInfluence;

                // Add Inertia Reaction (Shake) from Transform movement
                force -= localInertia * inertiaScale * intensity;

                // Add Explicit Acceleration from InputManager (Gyro/Keyboard Shake)
                if (InputManager.Instance != null)
                {
                    Vector3 inputAccel = transform.InverseTransformDirection(InputManager.Instance.CurrentAcceleration);
                    force -= inputAccel * mass * intensity; // F = ma, so a = F/m -> F = ma
                }

                // 6. Integrate (Euler)
                Vector3 acceleration = force / mass;
                vel += acceleration * dt;
                
                // Damping
                vel *= damping;

                // Update Position
                currentPos += vel * dt;

                _physicsVertices[i].Velocity = vel;
                _physicsVertices[i].Position = currentPos;
            }
        }
        
        // For external impacts (e.g., hitting pan)
        public void AddImpact(Vector3 forceVector)
        {
             for (int i = 0; i < _physicsVertices.Length; i++)
             {
                 // Add random variation for organic feel
                 _physicsVertices[i].Velocity += forceVector * Random.Range(0.8f, 1.2f) / mass;
             }
        }
    }
}
