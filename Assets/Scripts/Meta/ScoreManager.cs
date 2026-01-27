using UnityEngine;

namespace TakoyakiPhysics.Meta
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public float ShapeScore { get; private set; }
        public float CookScore { get; private set; }
        public float TurnScore { get; private set; }
        
        // Total Score (0-100)
        public float TotalScore => (ShapeScore + CookScore + TurnScore) / 3.0f * 100f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void CalculateScore(TakoyakiController takoyaki)
        {
            // 1. Shape: 1.0 is perfect sphere
            ShapeScore = takoyaki.ShapeIntegrity;

            // 2. Cook: 1.0 is perfect, deviation reduces score
            float cookDiff = Mathf.Abs(takoyaki.CookLevel - 1.0f);
            CookScore = Mathf.Clamp01(1.0f - cookDiff);

            // 3. Turn logic would be accumulated during gameplay
            // Placeholder
            TurnScore = 0.8f; 
            
            Debug.Log($"Score Calculated: {TotalScore:F1} (Shape:{ShapeScore:F2}, Cook:{CookScore:F2})");
        }
    }
}
