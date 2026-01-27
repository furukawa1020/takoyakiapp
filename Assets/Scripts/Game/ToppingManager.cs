using UnityEngine;
using TakoyakiPhysics.Visuals;

namespace TakoyakiPhysics.Game
{
    public class ToppingManager : MonoBehaviour
    {
        public static ToppingManager Instance { get; private set; }

        public enum ToppingType
        {
            None,
            Octopus,
            Ginger,
            Aonori,
            Bonito,
            Mayo
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleInput();
            }
        }

        private void HandleInput()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                TakoyakiController tako = hit.collider.GetComponent<TakoyakiController>();
                if (tako != null)
                {
                    ApplyTopping(tako);
                }
            }
        }

        private void ApplyTopping(TakoyakiController tako)
        {
            ToppingVisuals visuals = tako.GetComponent<ToppingVisuals>();
            if (visuals == null) return;
            
            if (TakoyakiPhysics.Feedback.AudioManager.Instance != null)
            {
                TakoyakiPhysics.Feedback.AudioManager.Instance.PlayToppingSound();
            }

            // Simple Logic for now: 
            // 1. If Raw and no Octopus -> Add Octopus
            // 2. If Cooking -> Add Ginger/Aonori randomly
            
            if (tako.CookLevel < 0.2f && !visuals.HasOctopus)
            {
                visuals.AddOctopus();
                Debug.Log("Added Octopus!");
            }
            else
            {
                // Topping Cycle: Ginger/Aonori -> Bonito -> Mayo
                // Need to track state better? For now, random/accumulative.
                
                // If we implemented a proper "HasTopping" check in Visuals it would be cleaner.
                // For now, let's just layer them based on clicks or check visual state via assumption?
                // Let's assume user taps multiple times.
                
                // Simple probabilistic progression for demo:
                float r = Random.value;
                if (r < 0.4f)
                {
                   if (Random.value > 0.5f) visuals.AddGinger();
                   else visuals.AddAonori();
                }
                else if (r < 0.7f)
                {
                    visuals.AddBonito();
                    Debug.Log("Added Bonito!");
                }
                else
                {
                    visuals.AddMayo();
                    Debug.Log("Added Mayo!");
                }
            }
        }
    }
}
