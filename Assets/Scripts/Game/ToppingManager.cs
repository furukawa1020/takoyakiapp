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
                // Toggle between Ginger and Aonori for variety
                if (Random.value > 0.5f)
                {
                    visuals.AddGinger();
                    Debug.Log("Added Ginger!");
                }
                else
                {
                    visuals.AddAonori();
                    Debug.Log("Added Aonori!");
                }
            }
        }
    }
}
