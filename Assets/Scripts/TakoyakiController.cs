using UnityEngine;

namespace TakoyakiPhysics
{
    public class TakoyakiController : MonoBehaviour
    {
        [Header("Status")]
        public float CookLevel = 0f; // 0: Raw, 1: Perfectly Cooked, >1: Burnt
        public float ShapeIntegrity = 0f; // 0: Liquid, 1: Perfect Sphere
        public float BatterAmount = 0f; // 0 to 1 (Full)

        public event System.Action OnPourComplete;
        public event System.Action OnBurn;
        public event System.Action OnCookedPerfect;

        [Header("References")]
        public Rigidbody Rb;
        public Renderer MeshRenderer;
        
        private TakoyakiState _currentState;

        private void Start()
        {
            if (Rb == null) Rb = GetComponent<Rigidbody>();
            if (MeshRenderer == null) MeshRenderer = GetComponent<Renderer>();
            
            // Initial State default
            TransitionToState(new States.PouringState(this));
        }

        private void Update()
        {
            _currentState?.UpdateState();
            
            // Global Cooking Logic (happens if heat is applied)
            // This might be better inside specific states, but could be global if the pan is always hot.
        }

        public void TransitionToState(TakoyakiState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
            
            Debug.Log($"Takoyaki State: {newState.GetType().Name}");
        }

        public void NotifyPourComplete() => OnPourComplete?.Invoke();
        public void NotifyBurn() => OnBurn?.Invoke();
        public void NotifyCookedPerfect() => OnCookedPerfect?.Invoke();

        // Helper to apply forces based on InputManager
        public void ApplyGravityTilt()
        {
            if (InputManager.Instance != null && Rb != null)
            {
                // Simple physics simulation based on tilt
                Vector3 tilt = InputManager.Instance.TiltVector;
                Physics.gravity = tilt * 9.81f; // Modify global gravity or apply force
            }
        }
    }
}
