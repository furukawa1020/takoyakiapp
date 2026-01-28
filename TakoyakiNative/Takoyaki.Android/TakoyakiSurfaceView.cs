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
        private TakoyakiHaptics _haptics;
        private TakoyakiAudio _audio;
        private TakoyakiSensor _sensor;
        
        public event Action<int>? GameFinished;

        public TakoyakiSurfaceView(Context context) : base(context)
        {
            SetEGLContextClientVersion(3); // OpenGL ES 3.0
            
            _haptics = new TakoyakiHaptics(context);
            _audio = new TakoyakiAudio(context);
            _sensor = new TakoyakiSensor(context);
            
            _renderer = new TakoyakiRenderer(_haptics, _audio, _sensor);
            _renderer.OnGameFinished = (score) => GameFinished?.Invoke(score);
            
            SetRenderer(_renderer);
            RenderMode = Rendermode.Continuously;
        }

        public void ResetGame()
        {
            QueueEvent(new Java.Lang.Runnable(() => {
                _renderer.Reset();
            }));
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

        public bool HandleKeyEvent(KeyEvent e)
        {
            return _renderer.HandleKeyEvent(e);
        }

        public override bool OnTouchEvent(MotionEvent? e)
        {
            if (e == null) return false;
            _renderer.HandleTouch(e);
            return true;
        }
    }

    public class TakoyakiRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        private int _program;
        private int _vao;
        private int _vbo;
        private int _ibo;
        private int _indexCount;

        // Matrix Handles
        private int _uMVPMatrixHandle;
        private int _uModelMatrixHandle;
        
        // Matrices
        private float[] _modelMatrix = new float[16];
        private float[] _viewMatrix = new float[16];
        private float[] _projectionMatrix = new float[16];
        private float[] _mvpMatrix = new float[16];
        
        // Core Logic
        private Takoyaki.Core.TakoyakiBall _ball;
        private Takoyaki.Core.SoftBodySolver _physics;
        private Takoyaki.Core.HeatSimulation _heatDelay;
        private Takoyaki.Core.TakoyakiStateMachine _stateMachine;

        private long _lastTimeNs;


        // Cache for VBO updates
        private float[] _meshData;

        private SteamParticles _steam;

        public Action<int> OnGameFinished;

        public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
        {
            try
            {
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 1 - GL Init");
                GLES30.GlEnable(GLES30.GlDepthTest);
                GLES30.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    
                // 1. Init Core Logic
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 2 - Core Logic");
                _ball = new Takoyaki.Core.TakoyakiBall(0, 2000); 
                _physics = new Takoyaki.Core.SoftBodySolver(_ball);
                _heatDelay = new Takoyaki.Core.HeatSimulation(_ball);
                _stateMachine = new Takoyaki.Core.TakoyakiStateMachine(_ball, _audio);
                
                _stateMachine.OnFinished = (score) => OnGameFinished?.Invoke(score);
    
                // Particles
                _steam = new SteamParticles(global::Android.App.Application.Context);
    
                // 2. Load Shaders
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 3 - Shaders");
                _program = ShaderHelper.LoadProgram(global::Android.App.Application.Context, "takoyaki.vert", "takoyaki.frag");
                GLES30.GlUseProgram(_program);
    
                // Texture Uniforms
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 4 - Textures");
                 int uBatter = GLES30.GlGetUniformLocation(_program, "uBatterTex");
                // ... (abbreviated, rely on structure matching)
                GLES30.GlUniform1i(uBatter, 0);
                GLES30.GlUniform1i(uCooked, 1);
                GLES30.GlUniform1i(uBurnt, 2);
                GLES30.GlUniform1i(uNoise, 3);

                 // Generate & Upload Textures
                LoadTexture(0, Takoyaki.Core.ProceduralTexture.GenerateBatter(512));
                _cookedTex = Takoyaki.Core.ProceduralTexture.GenerateCooked(512); 
                LoadTexture(1, _cookedTex);
                LoadTexture(2, Takoyaki.Core.ProceduralTexture.GenerateBurnt(512));
                LoadTexture(3, Takoyaki.Core.ProceduralTexture.GenerateNoiseMap(512));
    
                // 3. Generate Mesh & Buffers
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 5 - Mesh");
                var mesh = Takoyaki.Core.ProceduralMesh.GenerateSphere(64); 
                _meshData = mesh.ToInterleavedArray();
                short[] indices = mesh.Indices;
                _indexCount = indices.Length;

                // VBOs
                int[] vaos = new int[1];
                GLES30.GlGenVertexArrays(1, vaos, 0);
                _vao = vaos[0];
                GLES30.GlBindVertexArray(_vao);
    
                int[] buffers = new int[2];
                GLES30.GlGenBuffers(2, buffers, 0);
                _vbo = buffers[0];
                _ibo = buffers[1];
                
                // Upload
                 GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
                int bytes = _meshData.Length * 4;
                GLES30.GlBufferData(GLES30.GlArrayBuffer, bytes, Java.Nio.FloatBuffer.Wrap(_meshData), GLES30.GlDynamicDraw);

                // Attribs
                int stride = 8 * 4;
                GLES30.GlEnableVertexAttribArray(0);
                GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
                GLES30.GlEnableVertexAttribArray(1);
                GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, stride, 3 * 4);
                GLES30.GlEnableVertexAttribArray(2);
                GLES30.GlVertexAttribPointer(2, 2, GLES30.GlFloat, false, stride, 6 * 4);

                GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, _ibo);
                GLES30.GlBufferData(GLES30.GlElementArrayBuffer, indices.Length * 2, Java.Nio.ShortBuffer.Wrap(indices), GLES30.GlStaticDraw);
                
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 6 - Init Matrices");
                GLES30.GlBindVertexArray(0);
                Matrix.SetLookAtM(_viewMatrix, 0, 0, 4, 4, 0, 0, 0, 0, 1, 0);
                _lastTimeNs = Java.Lang.JavaSystem.NanoTime();
                
                 global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: FINISHED");
            }
            catch (System.Exception ex)
            {
                 global::Android.Util.Log.Error("TakoyakiCrash", $"CRASH IN ONSURFACECREATED: {ex}");
            }
        }

        private void UpdateMeshVBO()
        {
            if (_ball.DeformedVertices == null || _meshData == null) return;

            var simVerts = _ball.DeformedVertices;
            // Update Positions (Stride 0, 1, 2)
            // Stride is 8 floats
            int vCount = simVerts.Length; // Vector3 array
            // Safety check
            if (vCount * 8 > _meshData.Length) vCount = _meshData.Length / 8;

            for (int i = 0; i < vCount; i++)
            {
                _meshData[i * 8 + 0] = simVerts[i].X;
                _meshData[i * 8 + 1] = simVerts[i].Y;
                _meshData[i * 8 + 2] = simVerts[i].Z;
                // Ideally recalc normals here too
            }

            // Upload to GPU
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            // Re-upload whole buffer (easiest logic, though SubData is better for bandwidth)
            // Or use GlBufferSubData if we want.
            // java.nio.FloatBuffer wrap is cheap.
            GLES30.GlBufferSubData(GLES30.GlArrayBuffer, 0, _meshData.Length * 4, Java.Nio.FloatBuffer.Wrap(_meshData));
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
        }

        private void LoadTexture(int unit, Takoyaki.Core.ProceduralTexture tex)
        {
            int[] textureIds = new int[1];
            GLES30.GlGenTextures(1, textureIds, 0);
            int texId = textureIds[0];

            GLES30.GlActiveTexture(GLES30.GlTexture0 + unit);
            GLES30.GlBindTexture(GLES30.GlTexture2d, texId);

            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMinFilter, GLES30.GlLinearMipmapLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMagFilter, GLES30.GlLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureWrapS, GLES30.GlRepeat);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureWrapT, GLES30.GlRepeat);

            // Upload
            var buffer = Java.Nio.ByteBuffer.Wrap(tex.Pixels);
            GLES30.GlTexImage2D(GLES30.GlTexture2d, 0, GLES30.GlRgba, tex.Width, tex.Height, 0, GLES30.GlRgba, GLES30.GlUnsignedByte, buffer);
            GLES30.GlGenerateMipmap(GLES30.GlTexture2d);
        }

        private Takoyaki.Core.ProceduralTexture _cookedTex;
        private int _cookedTexUnit = 1;

        // ... (LoadTexture logic)

        public void ApplyTopping()
        {
            // Simple state sequence: Sauce -> Mayo -> Aonori
            if (_toppingStage == 0) _cookedTex.DrawSauce();
            else if (_toppingStage == 1) _cookedTex.DrawMayo();
            else if (_toppingStage == 2) _cookedTex.DrawAonori();
            
            _toppingStage++;
            
            // Re-upload
            LoadTexture(_cookedTexUnit, _cookedTex);
        }
        private int _toppingStage = 0;

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            GLES30.GlViewport(0, 0, width, height);
            float ratio = (float)width / height;
            Matrix.FrustumM(_projectionMatrix, 0, -ratio, ratio, -1, 1, 2, 10);
        }

        // Input
        private Takoyaki.Core.InputState _inputState = new Takoyaki.Core.InputState();
        private float _lastX, _lastY;

        public void HandleTouch(MotionEvent e)
        {
            float x = e.GetX();
            float y = e.GetY();

            if (e.Action == MotionEventActions.Down)
            {
                _inputState.IsTap = true;
                _inputState.TapPosition = new System.Numerics.Vector2(x, y);
                _lastX = x; _lastY = y;
            }
            else if (e.Action == MotionEventActions.Move)
            {
                float dx = x - _lastX;
                float dy = y - _lastY;
                if (Math.Abs(dx) > 5f || Math.Abs(dy) > 5f)
                {
                    _inputState.IsSwipe = true;
                    _inputState.SwipeDelta = new System.Numerics.Vector2(dx, dy);
                }
                _lastX = x; _lastY = y;
            }
            else if (e.Action == MotionEventActions.Up)
            {
                _inputState.IsTap = false;
                _inputState.IsSwipe = false;
            }
        }
        
        // Emulation State
        private float _emuTiltX = 0f;
        private float _emuTiltY = 0f;
        private bool _emuTrust = false;

        public bool HandleKeyEvent(KeyEvent e)
        {
            if (e.Action == KeyEventActions.Down)
            {
                switch (e.KeyCode)
                {
                    case Keycode.DpadUp: _emuTiltY = -0.5f; return true;
                    case Keycode.DpadDown: _emuTiltY = 0.5f; return true; // Pouring (Tilt Forward)
                    case Keycode.DpadLeft: _emuTiltX = -0.5f; return true;
                    case Keycode.DpadRight: _emuTiltX = 0.5f; return true;
                    case Keycode.Space: _emuTrust = true; return true; // Serve
                }
            }
            else if (e.Action == KeyEventActions.Up)
            {
                switch (e.KeyCode)
                {
                    case Keycode.DpadUp: 
                    case Keycode.DpadDown: _emuTiltY = 0f; return true;
                    case Keycode.DpadLeft: 
                    case Keycode.DpadRight: _emuTiltX = 0f; return true;
                    case Keycode.Space: _emuTrust = false; return true;
                }
            }
            return false;
        }

        public void OnDrawFrame(IGL10? gl)
        {
            try
            {
                // Time Delta
                long now = Java.Lang.JavaSystem.NanoTime();
                float dt = (now - _lastTimeNs) / 1_000_000_000.0f;
                _lastTimeNs = now;
    
                // 1. Update Core Simulation
                UpdateLogic(dt);
    
                // 2. Render
                GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
    
                GLES30.GlUseProgram(_program);
                GLES30.GlBindVertexArray(_vao);
    
                // Update Dynamic VBO (Physics Jiggle)
                UpdateMeshVBO();
    
                // Calc MVP
                Matrix.MultiplyMM(_mvpMatrix, 0, _viewMatrix, 0, _modelMatrix, 0); 
                Matrix.MultiplyMM(_mvpMatrix, 0, _projectionMatrix, 0, _mvpMatrix, 0); 
    
                GLES30.GlUniformMatrix4fv(_uMVPMatrixHandle, 1, false, _mvpMatrix, 0);
                GLES30.GlUniformMatrix4fv(_uModelMatrixHandle, 1, false, _modelMatrix, 0);
    
                // Update Shader Props
                int uCook = GLES30.GlGetUniformLocation(_program, "uCookLevel");
                GLES30.GlUniform1f(uCook, _ball.CookLevel);
                
                int uBatterLvl = GLES30.GlGetUniformLocation(_program, "uBatterLevel");
                GLES30.GlUniform1f(uBatterLvl, _ball.BatterLevel);
    
                GLES30.GlDrawElements(GLES30.GlTriangles, _indexCount, GLES30.GlUnsignedShort, 0);
                GLES30.GlBindVertexArray(0);
                
                // 3. Draw Particles (After opaque geometry)
                _steam.Update(dt, _ball.CookLevel > 0.3f ? 1.0f : 0.0f); // Steam if cooking
                _steam.Draw(_mvpMatrix);
            }
            catch (System.Exception ex)
            {
                 global::Android.Util.Log.Error("TakoyakiCrash", $"CRASH IN ONDRAWFRAME: {ex}");
            }
        }

        private TakoyakiHaptics _haptics;
        private TakoyakiAudio _audio;
        private TakoyakiSensor _sensor;

        public TakoyakiRenderer(TakoyakiHaptics haptics, TakoyakiAudio audio, TakoyakiSensor sensor)
        {
            _haptics = haptics;
            _audio = audio;
            _sensor = sensor;
        }
        
        // ... (OnSurfaceCreated etc remain same)

        // Inside UpdateLogic
        private void UpdateLogic(float dt)
        {
            // Input from Sensors
            _inputState.Tilt = _sensor.CurrentTilt;
            _inputState.Acceleration = _sensor.CurrentAcceleration;
            
            // Apply Emulation Override
            if (Math.Abs(_emuTiltX) > 0.01f) _inputState.Tilt.X = _emuTiltX;
            if (Math.Abs(_emuTiltY) > 0.01f) _inputState.Tilt.Y = _emuTiltY; // Override for Pouring
            
            if (_emuTrust)
            {
                // Emulate Thrust (Strong negative Z accel)
                _inputState.Acceleration.Z = -15.0f;
            }

            // Physics Gravity Direction
            // 0, -9.8, 0 is default down. 
            // If tilted, gravity effectively changes relative to the "Pan" space if we rotate the world.
            float gScale = 9.8f;
            System.Numerics.Vector3 gravity = new System.Numerics.Vector3(
                _inputState.Tilt.X * gScale, 
                -gScale, 
                _inputState.Tilt.Y * gScale // Y mapping to Z for 3D depth roll
            );

            _physics.Update(dt, System.Numerics.Vector3.Zero, gravity);
            
            // Interaction: Visual Rolling based on Tilt (Simple visual feedback)
            Matrix.SetIdentityM(_modelMatrix, 0); // Reset generic rotation
            
            // Base Rotation from StateMachine (e.g. Flipped)
            System.Numerics.Quaternion q = _ball.Rotation;
            // Convert Quaternion to Matrix? Or just simple Euler check for prototype.
            // If ball.Rotation has 180 flip (X axis PI), apply it.
            // Quaternion to Axis Angle:
            // Since we set it via CreateFromAxisAngle(UnitX, PI)
            if (_stateMachine.CurrentState is Takoyaki.Core.StateTurned || _stateMachine.CurrentState is Takoyaki.Core.StateFinished)
            {
                Matrix.RotateM(_modelMatrix, 0, 180f, 1, 0, 0);
            }

            Matrix.RotateM(_modelMatrix, 0, _inputState.Tilt.X * 45f, 0, 0, 1); // Z-axis roll
            Matrix.RotateM(_modelMatrix, 0, _inputState.Tilt.Y * 45f, 1, 0, 0); // X-axis pitch
            
            // State Machine Update
            _stateMachine.Update(_inputState, dt);

            // Audio & Haptics based on State
            if (_stateMachine.CurrentState is Takoyaki.Core.StateRaw)
            {
                // Pouring Sound logic (Placeholder until Audio supported properly)
                if (_ball.BatterLevel > 0.01f && _ball.BatterLevel < 1.0f)
                {
                    // _audio.PlayPour(); 
                }
            }
            else
            {
                // Cooking
                _heatDelay.Update(dt, 180f);
                _audio.UpdateSizzle(_ball.CookLevel, true); 
            }
            
            // Legacy Swipe/Tap logic
            if (_inputState.IsSwipe)
            {
                 // Add extra rotation on top or handle turning
                 // ...
            }
            {
                // ... Rotation logic ...
                Matrix.RotateM(_modelMatrix, 0, _inputState.SwipeDelta.X * 0.5f, 0, 1, 0);
                
                // Haptic Feedback
                if (Math.Abs(_inputState.SwipeDelta.X) > 10f)
                {
                    _haptics.TriggerRolling();
                }
            }
            
            if (_inputState.IsTap)
            {
                if (_ball.CookLevel > 0.8f) // Cooked enough?
                {
                    ApplyTopping();
                    _haptics.TriggerImpact(0.2f); // Light tap for toppings
                    _audio.PlayTap();
                }
                else
                {
                    _physics.TriggerJiggle(2.0f);
                    _haptics.TriggerImpact(0.5f);
                    _audio.PlayTap(); // Jiggle Sound
                }
                _inputState.IsTap = false; 
            }

            // _heatDelay.Update and Audio removed here, handled in StateMachine block above. 
        }



        public void Reset()
        {
            if (_stateMachine != null) _stateMachine.Reset();
        }
    }
}
