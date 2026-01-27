using Android.Content;
using Android.Opengl;
using Android.Util;
using Android.Views;
using Javax.Microedition.Khronos.Opengles;

namespace Takoyaki.Android
{
    public class TakoyakiSurfaceView : GLSurfaceView
    {
        private TakoyakiRenderer _renderer;

        public TakoyakiSurfaceView(Context context) : base(context)
        {
            SetEGLContextClientVersion(3); // OpenGL ES 3.0
            _renderer = new TakoyakiRenderer();
            SetRenderer(_renderer);
            RenderMode = Rendermode.Continuously; // Game Loop
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null) return false;
            // TODO: Pass touch to Core InputState
            return true;
        }
    }

    public class TakoyakiRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        // Core Logic Reference
        // private GameLoop _gameLoop;

        public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
        {
            // Init GL settings
            GLES30.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f); // Dark background (Iron Pan vibe)
            GLES30.GlEnable(GLES30.GlDepthTest);
            
            // Log for debugging
            Log.Debug("TakoyakiRenderer", "Surface Created. GL Version: " + GLES30.GlGetString(GLES30.GlVersion));
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            GLES30.GlViewport(0, 0, width, height);
            // Update Core Camera Aspect Ratio
        }

        public void OnDrawFrame(IGL10? gl)
        {
            // 1. Update Physics/Logic (Core)
            // _gameLoop.Update(dt);

            // 2. Clear Screen
            GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);

            // 3. Render (Draw Calls)
            // Use ShaderProgram -> Bind VAO -> Draw
        }
    }
}
