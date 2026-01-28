using System;

namespace Takoyaki.Core
{
    public interface ITakoyakiAudio
    {
        void PlayDing();
        void PlayTurn();
        void PlayServe();
    }
    
    // ... rest of file
    // Simplified State Interface (No Unity)
    // Simplified State Interface
    // Simplified State Interface
    public interface ITakoyakiState
    {
        void Enter(TakoyakiBall ball, ITakoyakiAudio audio);
        void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, ITakoyakiAudio audio);
        void Exit(TakoyakiBall ball);
    }

    public class TakoyakiStateMachine
    {
        private ITakoyakiState _currentState = null!;
        private TakoyakiBall _ball;
        private ITakoyakiAudio _audio;
        
        public Action<int>? OnFinished;

        public TakoyakiStateMachine(TakoyakiBall ball, ITakoyakiAudio audio)
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
            
            if (newState is StateFinished)
            {
                // Calculate Score
                // Perfect: CookLevel 1.0, ShapingQuality 1.0
                int cookScore = 100 - (int)(Math.Abs(_ball.CookLevel - 1.0f) * 60);
                int shapeScore = (int)(_ball.ShapingQuality * 40);
                
                int totalScore = cookScore + shapeScore;
                if (_ball.CookLevel < 0.5f) totalScore -= 50; // Raw penalty
                
                totalScore = Math.Clamp(totalScore, 0, 100);
                OnFinished?.Invoke(totalScore);
            }
        }
        
        public void Reset()
        {
            TransitionTo(new StateRaw());
        }
        
        public ITakoyakiState CurrentState => _currentState;
    }

    // --- STATES ---

    public class StateRaw : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball, ITakoyakiAudio audio) 
        {
             ball.BatterLevel = 1.0f; // Start Full for MVP
             ball.CookLevel = 1.0f; // Fully Cooked (Golden Brown!)
             ball.Rotation = System.Numerics.Quaternion.Identity;
        }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, ITakoyakiAudio audio)
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
        public void Enter(TakoyakiBall ball, ITakoyakiAudio audio) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, ITakoyakiAudio audio)
        {
            // Input: Swipe to Turn
            if (input.IsSwipe && ball.CookLevel > 0.3f) 
            {
                audio.PlayTurn(); // RHYTHM CUE
                machine.TransitionTo(new StateTurned());
            }
        }
    }

    public class StateTurned : ITakoyakiState
    {
        private float _timeInState = 0f;

        public void Enter(TakoyakiBall ball, ITakoyakiAudio audio) 
        {
             ball.Rotation = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX, (float)Math.PI);
        }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, ITakoyakiAudio audio)
        {
            _timeInState += dt;
            // Thrift forward (Negative Z accel)
            if (input.Acceleration.Z < -8.0f && _timeInState > 0.5f)
            {
                 audio.PlayServe(); // RHYTHM CUE
                 machine.TransitionTo(new StateFinished());
            }
        }
    }

    public class StateFinished : ITakoyakiState
    {
        public void Enter(TakoyakiBall ball, ITakoyakiAudio audio) { }
        public void Exit(TakoyakiBall ball) { }

        public void Update(TakoyakiStateMachine machine, TakoyakiBall ball, InputState input, float dt, ITakoyakiAudio audio)
        {
            // Freeze
        }
    }
}
