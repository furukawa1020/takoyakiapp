using UnityEngine;

namespace TakoyakiPhysics.States
{
    public class TurningState : TakoyakiState
    {
        private float _turnDuration = 0.5f;
        private float _timer;

        public TurningState(TakoyakiController controller) : base(controller) { }

        public override void Enter()
        {
            base.Enter();
            _timer = 0f;
            Debug.Log("Attempting Turn...");
            
            // Check for valid turn gesture (Velocity/Torque)
            // For prototype, we assume success and animate
            
            Controller.ShapeIntegrity += 0.2f; // Improve shape on turn
            if (Controller.ShapeIntegrity > 1.0f) Controller.ShapeIntegrity = 1.0f;
        }

        public override void UpdateState()
        {
            _timer += Time.deltaTime;

            // Rotate the ball physically
            if (Controller.Rb != null)
            {
                Controller.Rb.AddTorque(Vector3.right * 10f); // Spin it
            }

            if (_timer >= _turnDuration)
            {
                // Return to cooking (other side)
                Controller.TransitionToState(new CookingState(Controller));
            }
        }
    }
}
