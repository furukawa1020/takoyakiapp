using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    /// <summary>
    /// Zen breathing camera - FOV oscillation synchronized with takoyaki rhythm
    /// Custom wave pattern implementation for meditative experience
    /// </summary>
    public class ZenCameraOscillator : MonoBehaviour
    {
        private static ZenCameraOscillator _singleton;
        public static ZenCameraOscillator GetInstance() => _singleton;

        [SerializeField] private float defaultFieldOfView = 60f;
        [SerializeField] private float oscillationAmplitude = 5f;
        [SerializeField] private float cyclesPerSecond = 1.2f;
        [SerializeField] private float zenBoostMultiplier = 3f;
        
        private Camera mainCam;
        private float waveAccumulator;
        private float harmonyLevel;

        void Awake()
        {
            _singleton = this;
            mainCam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (!mainCam) return;

            waveAccumulator += Time.deltaTime * cyclesPerSecond * Mathf.PI * 2f;
            float waveValue = Mathf.Sin(waveAccumulator);
            float amplitudeWithZen = oscillationAmplitude + (harmonyLevel * zenBoostMultiplier);
            
            mainCam.fieldOfView = defaultFieldOfView + (waveValue * amplitudeWithZen);
        }

        public void UpdateHarmonyLevel(float level)
        {
            harmonyLevel = Mathf.Clamp01(level);
        }

        public void SynchronizePhase(float externalPhase)
        {
            waveAccumulator = externalPhase;
        }
    }
}
