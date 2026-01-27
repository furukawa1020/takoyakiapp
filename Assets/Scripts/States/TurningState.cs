using UnityEngine;
using TakoyakiPhysics;

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
            
            // DRAMATIC TURN: Jump up!
            if (Controller.Rb != null)
            {
                Controller.Rb.AddForce(Vector3.up * 4.0f, ForceMode.Impulse); 
                Controller.Rb.AddTorque(Vector3.right * 50f, ForceMode.Impulse); // Initial spin kick
            }

            // Trigger Jiggle
            var sb = Controller.GetComponent<TakoyakiPhysics.Visuals.TakoyakiSoftBody>();
            if (sb != null) sb.TriggerJiggle(2.0f);

            Controller.ShapeIntegrity += 0.2f; 
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
