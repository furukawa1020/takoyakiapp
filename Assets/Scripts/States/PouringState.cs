using UnityEngine;
using TakoyakiPhysics.Feedback;
using TakoyakiPhysics;

namespace TakoyakiPhysics.States
{
    public class PouringState : TakoyakiState
    {
        public PouringState(TakoyakiController controller) : base(controller) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Start Pouring Physics...");
            AudioManager.Instance.StartPouring();
            
            // Reset values
            Controller.BatterAmount = 0f;
            Controller.CookLevel = 0f;
            Controller.ShapeIntegrity = 0f; 
        }
        
        public override void Exit()
        {
            base.Exit();
            AudioManager.Instance.StopPouring();
        }

        public override void UpdateState()
        {
            // Logic: Tilt phone => Pour batter
            // InputManager.Instance.TiltVector.z could represent pouring angle (pitch)
            
            // Mock logic for "Press to pour" or "Tilt to pour"
            // For now, let's assume holding a touch/button pours
            if (Input.GetMouseButton(0) || Input.touchCount > 0) 
            {
                Controller.BatterAmount += Time.deltaTime * 0.5f; // Fill over 2 seconds
                Debug.Log($"Pouring... Amount: {Controller.BatterAmount:P0}");
            }

            if (Controller.BatterAmount >= 1.0f)
            {
                Controller.BatterAmount = 1.0f;
                Controller.NotifyPourComplete();
                // Auto transition to Cooking for this prototype
                Controller.TransitionToState(new CookingState(Controller));
            }
        }
    }
}
