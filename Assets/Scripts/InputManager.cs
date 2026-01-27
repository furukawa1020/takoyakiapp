using UnityEngine;

namespace TakoyakiPhysics
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float gyroSensitivity = 1.0f;
        [SerializeField] private float accelSensitivity = 1.0f;

        public Vector3 CurrentGyroRotation { get; private set; }
        public Vector3 CurrentAcceleration { get; private set; }
        
        // Calculated input for "pouring" or "tilting"
        public Vector3 TiltVector { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        
            EnableGyro();
        }

        private void EnableGyro()
        {
            if (SystemInfo.supportsGyroscope)
            {
                Input.gyro.enabled = true;
            }
            else
            {
                Debug.LogWarning("Gyroscope not supported on this device.");
            }
        }

        private void Update()
        {
            UpdateInput();
        }

        private void UpdateInput()
        {
            // Gyro Input
            if (SystemInfo.supportsGyroscope)
            {
                CurrentGyroRotation = Input.gyro.rotationRate * gyroSensitivity;
                // Basic tilt calculation - can be refined based on device orientation
                TiltVector = Input.gyro.gravity; 
            }
            else
            {
                // Fallback for editor testing
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                TiltVector = new Vector3(h, -1f, v).normalized; // Simulated gravity/tilt
            }

            // Acceleration Input (for flicks/shakes)
            CurrentAcceleration = Input.acceleration * accelSensitivity;
        }

        public Vector3 GetSimulatedRotation()
        {
             // Returns a vector useful for rotating the pan or pouring simulation
             return TiltVector;
        }
    }
}
