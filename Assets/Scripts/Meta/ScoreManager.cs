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

        public void CalculateScore(TakoyakiController[] takoyakis)
        {
            if (takoyakis == null || takoyakis.Length == 0) return;

            float totalShape = 0f;
            float totalCook = 0f;
            float toppingBonus = 0f;

            foreach (var tako in takoyakis)
            {
                // Shape
                totalShape += tako.ShapeIntegrity;

                // Cook Level (1.0 is perfect)
                float cookDiff = Mathf.Abs(tako.CookLevel - 1.0f);
                totalCook += Mathf.Clamp01(1.0f - cookDiff);

                // Toppings
                var toppings = tako.GetComponent<TakoyakiPhysics.Visuals.ToppingVisuals>();
                if (toppings != null)
                {
                    if (toppings.HasOctopus) toppingBonus += 5f;
                    if (toppings.HasGinger) toppingBonus += 2f;
                    if (toppings.HasAonori) toppingBonus += 2f;
                    if (toppings.HasBonito) toppingBonus += 3f;
                    if (toppings.HasMayo) toppingBonus += 3f;
                }
            }

            // Averages (0-1)
            ShapeScore = totalShape / takoyakis.Length;
            CookScore = totalCook / takoyakis.Length;
            
            // Base Score from Quality (0-100)
            float qualityScore = (ShapeScore * 40f) + (CookScore * 60f);
            
            // Add Bonuses
            TurnScore = toppingBonus; // Reuse this field for Bonus for now
            
            float rawScore = qualityScore + toppingBonus;
            
            // Cap at 100? Or go beyond for "S Rank"? Let's cap at 100 for simplicity or 120 for fun.
            // Let's call it calculated.
            
            // Update internal state
            // TotalScore property uses these.
            
            Debug.Log($"Score Calculated: {TotalScore:F1} (AvgShape:{ShapeScore:F2}, AvgCook:{CookScore:F2}, Bonus:{toppingBonus})");
        }
    }
}
