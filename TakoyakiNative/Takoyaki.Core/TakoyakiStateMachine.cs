using System;

namespace Takoyaki.Core
{
    // Simplified State Interface (No Unity)
    // Simplified State Interface
    // Simplified State Interface
    public interface ITakoyakiState
    {
        void Enter(TakoyakiBall ball, Takoyaki.Android.TakoyakiAudio audio);
        void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, Takoyaki.Android.TakoyakiAudio audio);
        void Exit(TakoyakiBall ball);
    }

    public class TakoyakiStateMachine
    {
        private ITakoyakiState _currentState;
        private TakoyakiBall _ball;
        private Takoyaki.Android.TakoyakiAudio _audio;

        public TakoyakiStateMachine(TakoyakiBall ball, Takoyaki.Android.TakoyakiAudio audio)
        {
            _ball = ball;
            _audio = audio;
            TransitionTo(new StateRaw());
        }

        public void Update(InputState input, float dt)
        {
            _currentState?.Update(this, _ball, input, dt, _audio);
        }

        public void TransitionTo(ITakoyakiState newState)
        {
            _currentState?.Exit(_ball);
            _currentState = newState;
            _currentState.Enter(_ball, _audio);
        }
        
        public ITakoyakiState CurrentState => _currentState;
    }

    // --- STATES ---

    public class StateRaw : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball, Takoyaki.Android.TakoyakiAudio audio) 
        {
             ball.BatterLevel = 0f;
             ball.CookLevel = 0f;
             ball.Rotation = System.Numerics.Quaternion.Identity;
        }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, Takoyaki.Android.TakoyakiAudio audio)
        {
            if (input.Tilt.Y > 0.2f)
            {
                ball.BatterLevel += dt * 0.8f; // Faster fill for rhythm
            }

            if (ball.BatterLevel >= 1.0f)
            {
                ball.BatterLevel = 1.0f;
                audio.PlayDing(); // RHYTHM CUE
                machine.TransitionTo(new StateCooking());
            }
        }
    }

    public class StateCooking : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball, Takoyaki.Android.TakoyakiAudio audio) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, Takoyaki.Android.TakoyakiAudio audio)
        {
            // Input: Swipe to Turn
            // Simple swipe check
            if (input.IsSwipe && ball.CookLevel > 0.3f) // Min cook constraint
            {
                // Trigger Turn
                audio.PlayTurn(); // RHYTHM CUE
                machine.TransitionTo(new StateTurned());
            }
        }
    }

    public class StateTurned : ITakoyakiState
    {
        private float _timeInState = 0f;

        public void Enter(TakoyakiBall ball, Takoyaki.Android.TakoyakiAudio audio) 
        {
            // Rotate 180 visual (Simple flip)
            // In real physics we would add torque, here we just snap/Lerp for prototype
             ball.Rotation = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, (float)Math.PI);
        }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, Takoyaki.Android.TakoyakiAudio audio)
        {
            _timeInState += dt;
            
            // Allow Serve Gesture (Thrust)
            // Z-Acceleration < -5.0 (Thrust forward)
            if (input.Acceleration.Z < -8.0f && _timeInState > 0.5f)
            {
                 audio.PlayServe(); // RHYTHM CUE
                 machine.TransitionTo(new StateFinished());
            }
        }
    }

    public class StateFinished : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball, Takoyaki.Android.TakoyakiAudio audio) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, Takoyaki.Android.TakoyakiAudio audio)
        {
            // Do nothing. Freeze.
        }
    }
}
