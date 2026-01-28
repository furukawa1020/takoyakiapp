using Android.Content;
using Android.Opengl;
using Android.Views;
using System;

namespace Takoyaki.Android
{
    /// <summary>
    /// The main View of the Takoyaki Soul application.
    /// Acts as a thin bridge between Android's GLSurfaceView and the internal rendering engine.
    /// </summary>
    public class TakoyakiSurfaceView : GLSurfaceView
    {
        private readonly TakoyakiRenderer _renderer;
        private readonly TakoyakiSensor _sensor;
        
        public event Action<int>? GameFinished;

        public TakoyakiSurfaceView(Context context) : base(context)
        {
            // 1. OpenGL Context Setup
            SetEGLContextClientVersion(3); // OpenGL ES 3.0
            
            // 2. Dependency Injection
            var haptics = new TakoyakiHaptics(context);
            var audio = new TakoyakiAudio(context);
            _sensor = new TakoyakiSensor(context);
            
            // 3. Renderer Initialization
            _renderer = new TakoyakiRenderer(_sensor, haptics, audio);
            _renderer.OnGameFinished = (score) => GameFinished?.Invoke(score);
            
            SetRenderer(_renderer);
            RenderMode = Rendermode.Continuously;
        }

        // --- Game Controls (Exposed for UI Activity) ---

        public void ResetGame()
        {
            QueueEvent(new Java.Lang.Runnable(() => _renderer.Reset()));
        }

        public override void OnResume()
        {
            base.OnResume();
            _sensor.Start();
        }

        public override void OnPause()
        {
            base.OnPause();
            _sensor.Stop();
        }

        // --- Input Bridging ---

        public bool HandleKeyEvent(KeyEvent e)
        {
            // Delegate keyboard/d-pad handling to the Input module
            return _renderer.Input.HandleKeyEvent(e);
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null) return false;
            // Delegate touch handling to the Input module
            _renderer.Input.HandleTouch(e);
            return true;
        }

        // --- Emulation API (Support for Debug Buttons) ---

        public void EmulatePour(float duration) { _renderer.Input.EmuTiltY = 0.5f; }
        public void EmulateFlip() { _renderer.ApplyTopping(); } 
        public void EmulateServe(float duration) { _renderer.Input.EmuThrust = true; }
        
        public void CaptureScreenshot(Action<global::Android.Graphics.Bitmap> callback)
        {
             // TODO: Re-implement screenshot capture in the new renderer if needed
        }
    }
}
