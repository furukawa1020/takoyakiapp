using System;

namespace Takoyaki.Core
{
    // Simplified State Interface (No Unity)
    public interface ITakoyakiState
    {
        void Enter(TakoyakiBall ball);
        void Update(TakoyakiBall ball, InputState input, float dt);
        void Exit(TakoyakiBall ball);
    }

    /// <summary>
    /// Pure C# State Machine.
    /// Manages the transition between Raw -> Cooking -> Turning -> Eating.
    /// </summary>
    public class TakoyakiStateMachine
    {
        private ITakoyakiState _currentState;
        private TakoyakiBall _ball;

        public TakoyakiStateMachine(TakoyakiBall ball)
        {
            _ball = ball;
            // distinct from Unity's Start()
            TransitionTo(new StateRaw());
        }

        public void Update(InputState input, float dt)
        {
            _currentState?.Update(_ball, input, dt);
        }

        public void TransitionTo(ITakoyakiState newState)
        {
            _currentState?.Exit(_ball);
            _currentState = newState;
            _currentState.Enter(_ball);
        }
    }

    // --- STATES ---

    public class StateRaw : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiBall ball, InputState input, float dt)
        {
            // Logic: If batter is poured, move to Cooking
            if (ball.BatterLevel > 0.9f)
            {
                // In a real manager, this transition would be triggered by the Pouring logic
            }
        }
    }

    public class StateCooking : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiBall ball, InputState input, float dt)
        {
            // Input: Check for Turn Gesture (Swipe or Tap+Drag)
            if (input.IsSwipe)
            {
                // Simple turn logic for now
                // "Drag Vector" vs "Ball Position"
                // Transition to Turning
            }
        }
    }
}
