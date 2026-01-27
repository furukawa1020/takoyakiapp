using UnityEngine;

namespace TakoyakiPhysics.States
{
    public class CookingState : TakoyakiState
    {
        private float _cookingSpeed = 0.1f; // Speed of cooking

        public CookingState(TakoyakiController controller) : base(controller) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Cooking Started...");
        }

        public override void UpdateState()
        {
            // Simulate heat
            Controller.CookLevel += Time.deltaTime * _cookingSpeed;

            // Visual feedback (Log for now)
            // In real impl, we would update shader properties here
            
            if (Controller.CookLevel > 0.5f && Input.GetKeyDown(KeyCode.Space)) 
            {
                // Placeholder for "Turning" input
                // In reality, this would be a gesture detection
                Controller.TransitionToState(new TurningState(Controller));
            }

            if (Controller.CookLevel > 1.5f)
            {
                Debug.Log("Burnt!");
                // Fail state or just burnt visual
            }
        }
    }
}
