using Android.Views;
using Takoyaki.Core;
using System;

namespace Takoyaki.Android
{
    /// <summary>
    /// Handles all input sources (Touch, Sensors, Keyboard) and maps them to the Core InputState.
    /// </summary>
    public class TakoyakiInputHandler
    {
        private InputState _inputState;
        private readonly TakoyakiSensor _sensor;
        
        // Emulation State (useful for testing on emulators without sensors)
        public float EmuTiltX { get; set; } = 0f;
        public float EmuTiltY { get; set; } = 0f;
        public bool EmuThrust { get; set; } = false;

        private float _lastTouchX, _lastTouchY;

        public TakoyakiInputHandler(TakoyakiSensor sensor, InputState inputState)
        {
            _sensor = sensor;
            _inputState = inputState;
        }

        /// <summary>
        /// Updates the internal InputState based on sensor data and logical overrides.
        /// </summary>
        public void Update(float dt)
        {
            // Sync Sensor to InputState
            _inputState.Tilt = _sensor.CurrentTilt;
            _inputState.Acceleration = _sensor.CurrentAcceleration;

            // Apply Emulation Overrides (Keyboard/DPad)
            if (Math.Abs(EmuTiltX) > 0.01f) _inputState.Tilt.X = EmuTiltX;
            if (Math.Abs(EmuTiltY) > 0.01f) _inputState.Tilt.Y = EmuTiltY;
            
            if (EmuThrust)
            {
                // Emulate Thrust (Strong impulse)
                _inputState.Acceleration.Z = -15.0f;
            }
        }

        /// <summary>
        /// Processes raw MotionEvents into logical Taps and Swipes.
        /// </summary>
        public void HandleTouch(MotionEvent e)
        {
            float x = e.GetX();
            float y = e.GetY();

            switch (e.Action)
            {
                case MotionEventActions.Down:
                    _inputState.IsTap = true;
                    _inputState.TapPosition = new System.Numerics.Vector2(x, y);
                    _lastTouchX = x; 
                    _lastTouchY = y;
                    break;

                case MotionEventActions.Move:
                    float dx = x - _lastTouchX;
                    float dy = y - _lastTouchY;
                    if (Math.Abs(dx) > 5f || Math.Abs(dy) > 5f)
                    {
                        _inputState.IsSwipe = true;
                        _inputState.SwipeDelta = new System.Numerics.Vector2(dx, dy);
                    }
                    _lastTouchX = x; 
                    _lastTouchY = y;
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    _inputState.IsTap = false;
                    _inputState.IsSwipe = false;
                    break;
            }
        }

        /// <summary>
        /// Maps KeyEvents (like DPads or Space) to emulation states.
        /// </summary>
        public bool HandleKeyEvent(KeyEvent e)
        {
            bool isDown = e.Action == KeyEventActions.Down;

            switch (e.KeyCode)
            {
                case Keycode.DpadUp: 
                    EmuTiltY = isDown ? -0.5f : 0f; 
                    return true;
                case Keycode.DpadDown: 
                    EmuTiltY = isDown ? 0.5f : 0f; 
                    return true;
                case Keycode.DpadLeft: 
                    EmuTiltX = isDown ? -0.5f : 0f; 
                    return true;
                case Keycode.DpadRight: 
                    EmuTiltX = isDown ? 0.5f : 0f; 
                    return true;
                case Keycode.Space: 
                    EmuThrust = isDown; 
                    return true;
            }
            return false;
        }
    }
}
