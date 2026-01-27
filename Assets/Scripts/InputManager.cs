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
        
        private Quaternion _calibrationOffset = Quaternion.identity;

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
        
        public void Calibrate()
        {
            if (SystemInfo.supportsGyroscope)
            {
                // Set current rotation as the "zero" point
                // We want the inverse of the current rotation to be applied to future readings
                _calibrationOffset = Quaternion.Inverse(Input.gyro.attitude);
                Debug.Log("Gyro Calibrated!");
            }
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
                
                // Apply Calibration
                Quaternion calibratedAttitude = _calibrationOffset * Input.gyro.attitude;
                
                // Remap Unity's Gyro attitude to a useful Vector3 for tilt
                // Just using gravity from the calibrated attitude
                TiltVector = calibratedAttitude * Vector3.down; 
            }
            else
            {
                // Fallback for editor testing OR Virtual Buttons
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical"); 
                
                // Add virtual input
                h += _virtualInput.x;
                v += _virtualInput.y;
                
                h = Mathf.Clamp(h, -1f, 1f);
                v = Mathf.Clamp(v, -1f, 1f);

                // In Editor, Arrow keys simulate tilting the device
                TiltVector = new Vector3(h, -1f, v).normalized; 
            }

            // Acceleration Input
            if (SystemInfo.supportsGyroscope)
            {
                CurrentAcceleration = Input.acceleration * accelSensitivity;
            }
            else
            {
                 // Emulate "Shake/Impact" with Spacebar
                 if (Input.GetKeyDown(KeyCode.Space))
                 {
                     CurrentAcceleration = Vector3.up * 2.0f * accelSensitivity; // Sudden jolt
                 }
                 else
                 {
                     CurrentAcceleration = Vector3.MoveTowards(CurrentAcceleration, Vector3.zero, Time.deltaTime * 5f);
                 }
            }
        }

        public Vector3 GetSimulatedRotation()
        {
             // Returns a vector useful for rotating the pan or pouring simulation
             return TiltVector;
        }

        private Vector2 _virtualInput;
        public void SetVirtualInput(Vector2 input)
        {
            _virtualInput = input;
        }
    }
}
