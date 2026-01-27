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
            float dt = Time.deltaTime;
            
            // 1. Calculate Heat Direction
            // Which part of the takoyaki is touching the bottom?
            // "Down" in local space is the part touching the pan
            Vector3 localDown = Controller.transform.InverseTransformDirection(Vector3.down);
            
            // For now, we simulate "Directional Cooking" by just updating the global cook level
            // but effectively "weighting" it by how much the ball is stable.
            // A rapidly spinning ball cooks evenly (slowly). A still ball cooks locally (fast).
            
            float stability = 1.0f - Mathf.Clamp01(Controller.Rb.angularVelocity.magnitude * 0.1f);
            
            // If stable, we cook faster (but locally - simplified to global multiplier here)
            // In a full implementation, we would write into a Texture2D at UV coordinates mapped from localDown.
            float effectiveHeat = _cookingSpeed * (0.5f + 0.5f * stability);

            Controller.CookLevel += effectiveHeat * dt;

            // Simulate "Reaction" to heat (sound/particles)
            AudioManager.Instance.StartSizzle(Controller.CookLevel * 0.5f);

            // Transition Logic (Placeholder input)
            if (Controller.CookLevel > 0.5f && Input.GetKeyDown(KeyCode.Space)) 
            {
                Controller.TransitionToState(new TurningState(Controller));
            }

            if (Controller.CookLevel > 1.8f) // Burnt threshold
            {
                Debug.Log("Burnt!");
                AudioManager.Instance.StopSizzle();
            }
        }
    }
}
