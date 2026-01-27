using UnityEngine;

namespace TakoyakiPhysics.Feedback
{
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

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
        }

        public void TriggerLightImpact()
        {
            // Placeholder: Handheld.Vibrate is binary. 
            // Real implementation would use Android/iOS specific plugins for "Light" impact.
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif
            Debug.Log("[Haptic] Light Impact");
        }

        public void TriggerHeavyImpact()
        {
            #if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
            #endif
            Debug.Log("[Haptic] Heavy Impact");
        }

        public void TriggerContinuous(float intensity, float duration)
        {
            // For "Sizzle" vibration
            Debug.Log($"[Haptic] Continuous: {intensity} for {duration}s");
        }
    }
}
