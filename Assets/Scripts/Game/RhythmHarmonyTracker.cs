using UnityEngine;

namespace TakoyakiPhysics.Game
{
    /// <summary>
    /// Rhythm harmony tracker - evaluates player rotation consistency
    /// Rewards steady gyroscope patterns matching target velocity
    /// </summary>
    public class RhythmHarmonyTracker : MonoBehaviour
    {
        private static RhythmHarmonyTracker _singleton;
        public static RhythmHarmonyTracker GetInstance() => _singleton;

        [SerializeField] private float idealRotationSpeed = 6.0f;
        [SerializeField] private float acceptableSpeedRange = 1.5f;
        [SerializeField] private float harmonicGrowthRate = 0.3f;
        [SerializeField] private float harmonicDissipationRate = 0.5f;
        [SerializeField] private float smoothnessImportance = 0.7f;
        [SerializeField] private float skillAccumulationRate = 0.1f;
        [SerializeField] private float skillDeclineRate = 0.05f;
        
        public float HarmonyScore { get; private set; }
        public float SkillLevel { get; private set; }
        public bool AchievedZenState => HarmonyScore > 0.7f;
        
        private float movementConsistency;
        private Vector3 previousGyroSnapshot;
        private float sustainedZenDuration;

        void Awake()
        {
            _singleton = this;
        }

        void Update()
        {
            EvaluatePlayerRhythm();
            ProgressSkillMeter();
            BroadcastFeedbackSignals();
        }

        void EvaluatePlayerRhythm()
        {
            if (!InputManager.Instance) return;

            Vector3 gyroData = InputManager.Instance.CurrentGyroRotation;
            float rotationMagnitude = gyroData.magnitude;
            
            float speedDifference = Mathf.Abs(rotationMagnitude - idealRotationSpeed);
            bool withinTargetRange = speedDifference < acceptableSpeedRange;
            
            Vector3 gyroChange = gyroData - previousGyroSnapshot;
            float instability = gyroChange.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            movementConsistency = Mathf.Clamp01(1.0f - (instability / 10f));
            
            previousGyroSnapshot = gyroData;
            
            float targetAlignment = withinTargetRange ? 1.0f : 0.0f;
            float consistencyContribution = movementConsistency * smoothnessImportance;
            float rhythmQuality = (targetAlignment * (1.0f - smoothnessImportance)) + consistencyContribution;
            
            if (rhythmQuality > 0.5f)
            {
                HarmonyScore = Mathf.MoveTowards(HarmonyScore, rhythmQuality, harmonicGrowthRate * Time.deltaTime);
                sustainedZenDuration += Time.deltaTime;
            }
            else
            {
                HarmonyScore = Mathf.MoveTowards(HarmonyScore, 0f, harmonicDissipationRate * Time.deltaTime);
                sustainedZenDuration = 0f;
            }
            
            HarmonyScore = Mathf.Clamp01(HarmonyScore);
        }

        void ProgressSkillMeter()
        {
            if (HarmonyScore > 0.7f)
            {
                float bonusFromSustaining = Mathf.Min(sustainedZenDuration / 10f, 0.5f);
                SkillLevel += (skillAccumulationRate + bonusFromSustaining) * Time.deltaTime;
            }
            else
            {
                SkillLevel -= skillDeclineRate * Time.deltaTime;
            }
            
            SkillLevel = Mathf.Clamp01(SkillLevel);
        }

        void BroadcastFeedbackSignals()
        {
            var cameraOsc = Visuals.ZenCameraOscillator.GetInstance();
            if (cameraOsc) cameraOsc.UpdateHarmonyLevel(HarmonyScore);
            
            var auraOverlay = Visuals.GoldenAuraOverlay.GetInstance();
            if (auraOverlay) auraOverlay.AdjustAuraStrength(HarmonyScore);
            
            if (AchievedZenState && sustainedZenDuration > 2f)
            {
                var audioMgr = Feedback.AudioManager.Instance;
                if (audioMgr && Mathf.FloorToInt(sustainedZenDuration) % 3 == 0 && Time.frameCount % 180 == 0)
                {
                    audioMgr.PlayPerfect();
                }
            }
        }

        public float GetRotationDeviation()
        {
            if (!InputManager.Instance) return 0f;
            return Mathf.Abs(InputManager.Instance.CurrentGyroRotation.magnitude - idealRotationSpeed);
        }

        public float GetMovementConsistency()
        {
            return movementConsistency;
        }
    }
}
