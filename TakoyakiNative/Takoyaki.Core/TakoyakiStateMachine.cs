using System;

namespace Takoyaki.Core
{
    // Simplified State Interface (No Unity)
    // Simplified State Interface
    public interface ITakoyakiState
    {
        void Enter(TakoyakiBall ball);
        void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt);
        void Exit(TakoyakiBall ball);
    }

    public class TakoyakiStateMachine
    {
        private ITakoyakiState _currentState;
        private TakoyakiBall _ball;

        public TakoyakiStateMachine(TakoyakiBall ball)
        {
            _ball = ball;
            TransitionTo(new StateRaw());
        }

        public void Update(InputState input, float dt)
        {
            _currentState?.Update(this, _ball, input, dt);
        }

        public void TransitionTo(ITakoyakiState newState)
        {
            _currentState?.Exit(_ball);
            _currentState = newState;
            _currentState.Enter(_ball);
        }
        
        public ITakoyakiState CurrentState => _currentState;
    }

    // --- STATES ---

    public class StateRaw : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball) 
        {
             ball.BatterLevel = 0f;
             ball.CookLevel = 0f;
        }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt)
        {
            // Logic: Tilt forward (Tilt.Y > 0.3) to pour
            // Tilt.Y is positive when phone top goes DOWN (assuming Landscape/Portrait mapping)
            // Let's assume Tilt.Y > 0.2 is pouring
            if (input.Tilt.Y > 0.2f)
            {
                ball.BatterLevel += dt * 0.5f; // Fill speed
            }

            if (ball.BatterLevel >= 1.0f)
            {
                ball.BatterLevel = 1.0f;
                machine.TransitionTo(new StateCooking());
            }
        }
    }

    public class StateCooking : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt)
        {
            // Cooking happens in HeatSimulation, but we monitor it here
            if (ball.CookLevel > 1.0f)
            {
                // Maybe burn or finishing?
            }
            
            // Input: Swipe to Turn
            if (input.IsSwipe)
            {
                // Initiate turn?
            }
        }
    }
}
