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

        // Emulation Controls (Public API)
        public void EmulatePour(float duration)
        {
            _renderer.SetEmulationTilt(0.5f);
        }

        public void EmulateFlip()
        {
            _renderer.TriggerEmulationSwipe();
        }

        public void EmulateServe(float duration)
        {
            _renderer.SetEmulationThrust(true);
        }

        public void CaptureScreenshot(Action<global::Android.Graphics.Bitmap> callback)
        {
            // _pendingCaptureCallback = callback; // OLD
             _renderer.RequestCapture(callback);
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

        // Screenshot
        private Action<global::Android.Graphics.Bitmap>? _pendingCaptureCallback;
        
        // Shake detection cooldown
        private long _lastShakeTime = 0;
        
        // Initial topping application flag
        private bool _needsInitialToppings = true;
        
        // Topping System (3D Objects)
        private int _billboardProgram;
        private int _billboardVBO;
        private int _sauceTexture;
        private int _mayoTexture;
        private bool _sauceVisible = false;
        private bool _mayoVisible = false;
        private List<System.Numerics.Vector3> _aonoriParticles = new List<System.Numerics.Vector3>();

        public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
        {
            try
            {
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 1 - GL Init");
                GLES30.GlEnable(GLES30.GlDepthTest);
                GLES30.GlDisable(2884); // GL_CULL_FACE
                GLES30.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f);
    
                // 1. Init Core Logic
                // 1. Mesh Generation (Must be first to init physics)
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 2 - Mesh Gen");
                var mesh = Takoyaki.Core.ProceduralMesh.GenerateSphere(64); 
                _meshData = mesh.ToInterleavedArray();
                short[] indices = mesh.Indices;
                _indexCount = indices.Length;

                // 2. Init Core Logic (Physics needs Vertices)
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 3 - Core Logic");
                int vCount = mesh.Vertices.Length / 3;
                _ball = new Takoyaki.Core.TakoyakiBall(0, vCount); 
                _ball.BatterLevel = 1.0f; 
                
                // Copy initial mesh to ball physics state (Float[] -> Vector3[])
                for(int i=0; i<vCount; i++)
                {
                    float x = mesh.Vertices[i*3+0];
                    float y = mesh.Vertices[i*3+1];
                    float z = mesh.Vertices[i*3+2];
                    var vec = new System.Numerics.Vector3(x, y, z);
                    _ball.BaseVertices[i] = vec;
                    _ball.DeformedVertices[i] = vec;
                }

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

                _uMVPMatrixHandle = GLES30.GlGetUniformLocation(_program, "uMVPMatrix");
                _uModelMatrixHandle = GLES30.GlGetUniformLocation(_program, "uModelMatrix");
                
                global::Android.Util.Log.Error("TakoyakiCrash", $"HANDLES: MVP={_uMVPMatrixHandle}, Model={_uModelMatrixHandle}");
                
                int uDisp = GLES30.GlGetUniformLocation(_program, "uDisplacementStrength");
                GLES30.GlUniform1f(uDisp, 0.1f); // Default displacement
    
                // Texture Uniforms
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 4 - Textures");
                int uBatter = GLES30.GlGetUniformLocation(_program, "uBatterTex");
                global::Android.Util.Log.Error("TakoyakiCrash", $"HANDLES: Batter={uBatter}");
                // ... (abbreviated, rely on structure matching)
                GLES30.GlUniform1i(uBatter, 0);

                int uCooked = GLES30.GlGetUniformLocation(_program, "uCookedTex");
                int uBurnt = GLES30.GlGetUniformLocation(_program, "uBurntTex");
                int uNoise = GLES30.GlGetUniformLocation(_program, "uNoiseMapFix");

                GLES30.GlUniform1i(uCooked, 1);
                GLES30.GlUniform1i(uBurnt, 2);
                GLES30.GlUniform1i(uNoise, 3);

                 // Generate & Upload Textures
                LoadTexture(0, Takoyaki.Core.ProceduralTexture.GenerateBatter(64));
                _cookedTex = Takoyaki.Core.ProceduralTexture.GenerateCooked(64); 
                LoadTexture(1, _cookedTex);
                LoadTexture(2, Takoyaki.Core.ProceduralTexture.GenerateBurnt(64));
                LoadTexture(3, Takoyaki.Core.ProceduralTexture.GenerateNoiseMap(64));
    
                // 3. Generate Mesh & Buffers
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 5 - Buffers");
                // Mesh generated earlier for Physics init

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
                
                GLES30.GlBindVertexArray(0);
                Matrix.SetLookAtM(_viewMatrix, 0, 0, 4, 4, 0, 0, 0, 0, 1, 0);
                _lastTimeNs = Java.Lang.JavaSystem.NanoTime();
                
                // Initialize Billboard System for Toppings
                global::Android.Util.Log.Error("TakoyakiCrash", "ONSURFACECREATED: 6 - Billboards");
                InitializeBillboards();
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

            // Screenshot Capture Handling
            if (_pendingCaptureCallback != null)
            {
                var callback = _pendingCaptureCallback;
                _pendingCaptureCallback = null; // Clear first to avoid re-entry

                int w = _width; // Use stored _width from OnSurfaceChanged
                int h = _height;
                int[] pixels = new int[w * h];
                int[] pixelsReversed = new int[w * h];
                
                var buf = Java.Nio.IntBuffer.Wrap(pixels);
                buf.Position(0);
                
                // Read pixels (RGBA)
                GLES30.GlReadPixels(0, 0, w, h, GLES30.GlRgba, GLES30.GlUnsignedByte, buf);

                // Fix upside down (OpenGL origin is bottom-left)
                for (int i = 0; i < h; i++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        pixelsReversed[(h - i - 1) * w + j] = pixels[i * w + j];
                    }
                }
                
                 for (int i = 0; i < pixelsReversed.Length; i++)
                 {
                     int p = pixelsReversed[i];
                     // Input: 0xAABBGGRR (Little Endian read of R,G,B,A)
                     // Target: 0xAARRGGBB
                     int r = (p) & 0xFF;
                     int g = (p >> 8) & 0xFF;
                     int b = (p >> 16) & 0xFF;
                     int a = (p >> 24) & 0xFF;
                     pixelsReversed[i] = (a << 24) | (r << 16) | (g << 8) | b;
                 }

                // Create Bitmap on UI Thread (or just here and pass back)
                var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(w, h, global::Android.Graphics.Bitmap.Config.Argb8888);
                bitmap.SetPixels(pixelsReversed, 0, w, 0, 0, w, h);

                // Invoke callback on UI Thread
                var handler = new global::Android.OS.Handler(global::Android.OS.Looper.MainLooper);
                handler.Post(() => callback(bitmap));
            }
        }

        // Emulation Controls (Public API for Debug Buttons)
        public void SetEmulationTilt(float tiltY)
        {
            _emuTiltY = tiltY;
        }

        public void SetEmulationThrust(bool enabled)
        {
            _emuTrust = enabled;
        }

        public void TriggerEmulationSwipe()
        {
            _inputState.IsSwipe = true;
            _inputState.SwipeDelta = new System.Numerics.Vector2(50f, 0f);
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
            // NEW: Show 3D topping objects instead of texture baking
            if (_toppingStage == 0)
            {
                _sauceVisible = true;
                global::Android.Util.Log.Debug("TakoyakiTopping", "Sauce applied!");
            }
            else if (_toppingStage == 1)
            {
                _mayoVisible = true;
                global::Android.Util.Log.Debug("TakoyakiTopping", "Mayo applied!");
            }
            else if (_toppingStage == 2)
            {
                GenerateAonoriParticles(15);
                global::Android.Util.Log.Debug("TakoyakiTopping", $"Aonori applied! {_aonoriParticles.Count} particles");
            }
            
            _toppingStage++;
        }
        
        private void GenerateAonoriParticles(int count)
        {
            var random = new System.Random();
            for (int i = 0; i < count; i++)
            {
                // Random position on sphere surface (using spherical coordinates)
                float theta = (float)(random.NextDouble() * System.Math.PI * 2);
                float phi = (float)(random.NextDouble() * System.Math.PI);
                float radius = 1.0f; // Ball radius
                
                float x = radius * (float)System.Math.Sin(phi) * (float)System.Math.Cos(theta);
                float y = radius * (float)System.Math.Sin(phi) * (float)System.Math.Sin(theta);
                float z = radius * (float)System.Math.Cos(phi);
                
                _aonoriParticles.Add(new System.Numerics.Vector3(x, y, z));
            }
        }
        private int _toppingStage = 0;

        private int _width;
        private int _height;

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            _width = width;
            _height = height;
            global::Android.Util.Log.Error("TakoyakiCrash", $"ONSURFACECHANGED: w={width}, h={height}");
            GLES30.GlViewport(0, 0, width, height);
            float ratio = (float)width / height;
            Matrix.FrustumM(_projectionMatrix, 0, -ratio, ratio, -1, 1, 2, 10);
            
            // Camera Setup - Eye level with sphere
            // Eye: (0, 0, 6), Center: (0, 0, 0), Up: (0, 1, 0)
            Matrix.SetLookAtM(_viewMatrix, 0, 0f, 0f, 6f, 0f, 0f, 0f, 0f, 1f, 0f);
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
        
        public void RequestCapture(Action<global::Android.Graphics.Bitmap> callback)
        {
            _pendingCaptureCallback = callback;
        }

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
                if (_ball == null || _physics == null || _stateMachine == null) return;
                
                // FIRST FRAME: Apply initial toppings for visual appeal
                if (_needsInitialToppings)
                {
                    global::Android.Util.Log.Debug("TakoyakiInit", "Applying initial toppings!");
                    ApplyTopping(); // Sauce
                    ApplyTopping(); // Mayo
                    ApplyTopping(); // Aonori
                    _needsInitialToppings = false;
                }
                
                UpdateLogic(dt);
    
                // 2. Render
                GLES30.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f); // Dark gray
                GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
    
                GLES30.GlUseProgram(_program);
                
                // BRUTE FORCE: Bind everything explicitly
                GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
                GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, 32, 0); // Pos
                GLES30.GlEnableVertexAttribArray(0);
                GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, 32, 12); // Norm
                GLES30.GlEnableVertexAttribArray(1);
                GLES30.GlVertexAttribPointer(2, 2, GLES30.GlFloat, false, 32, 24); // UV
                GLES30.GlEnableVertexAttribArray(2);
                
                // GLES30.GlBindVertexArray(_vao);
    
                // Update Dynamic VBO (Physics Jiggle)
                UpdateMeshVBO();
    
                // Calc MVP (Android Matrix.MultiplyMM: result = lhs * rhs)
                // MVP = Projection * View * Model
                float[] tempMatrix = new float[16];
                Matrix.MultiplyMM(tempMatrix, 0, _viewMatrix, 0, _modelMatrix, 0); // View * Model
                Matrix.MultiplyMM(_mvpMatrix, 0, _projectionMatrix, 0, tempMatrix, 0); // Proj * (View * Model) 
    
                GLES30.GlUniformMatrix4fv(_uMVPMatrixHandle, 1, false, _mvpMatrix, 0);
                GLES30.GlUniformMatrix4fv(_uModelMatrixHandle, 1, false, _modelMatrix, 0);
    
                // Update Shader Props
                int uCook = GLES30.GlGetUniformLocation(_program, "uCookLevel");
                GLES30.GlUniform1f(uCook, _ball.CookLevel);
                
                int uBatterLvl = GLES30.GlGetUniformLocation(_program, "uBatterLevel");
                GLES30.GlUniform1f(uBatterLvl, _ball.BatterLevel);

                // Lighting & View
                int uLight = GLES30.GlGetUniformLocation(_program, "uLightPos");
                GLES30.GlUniform3f(uLight, 5.0f, 5.0f, 5.0f);

                int uView = GLES30.GlGetUniformLocation(_program, "uViewPos");
                GLES30.GlUniform3f(uView, 0.0f, 0.0f, 6.0f);

                int uFresnel = GLES30.GlGetUniformLocation(_program, "uOilFresnel");
                GLES30.GlUniform1f(uFresnel, 1.0f);
    
                GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, _ibo);
                GLES30.GlDrawElements(GLES30.GlTriangles, _indexCount, GLES30.GlUnsignedShort, 0);
                
                GLES30.GlDisableVertexAttribArray(0);
                GLES30.GlDisableVertexAttribArray(1);
                GLES30.GlDisableVertexAttribArray(2);
                GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
                GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, 0);
                // GLES30.GlBindVertexArray(0);
                
                // 3. Draw Particles (After opaque geometry)
                _steam.Update(dt, _ball.CookLevel > 0.3f ? 1.0f : 0.0f); // Steam if cooking
                _steam.Draw(_mvpMatrix);
                
                // 4. Draw Toppings (Billboards & Particles)
                RenderToppings();
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

            // Simple Collision/Ground check
            _ball.IsInHole = true; // Always in hole for MVP

            _heatDelay.Update(dt, 200.0f);
            
            // Debug Log every ~60 frames
            if ((int)(dt * 1000) % 60 == 0) {
                 global::Android.Util.Log.Debug("TakoyakiHeat", $"CookLvl: {_ball.CookLevel:F3} IsInHole:{_ball.IsInHole}");
            }

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

            // SHAKE SHAKE MODE: Shake to apply toppings!
            // Detect shake via acceleration magnitude
            float accelMagnitude = System.MathF.Sqrt(
                _inputState.Acceleration.X * _inputState.Acceleration.X +
                _inputState.Acceleration.Y * _inputState.Acceleration.Y +
                _inputState.Acceleration.Z * _inputState.Acceleration.Z
            );
            
            // Threshold: ~8 m/s² (easier shake, gravity is ~9.8)
            if (accelMagnitude > 8.0f)
            {
                // Cooldown to avoid spam (check if enough time passed)
                long now = Java.Lang.JavaSystem.CurrentTimeMillis();
                if (now - _lastShakeTime > 500) // 500ms cooldown
                {
                    global::Android.Util.Log.Debug("TakoyakiShake", $"SHAKE! Accel={accelMagnitude:F2} Stage={_toppingStage}");
                    ApplyTopping();
                    _haptics.TriggerImpact(1.0f); // Haptic feedback!
                    _lastShakeTime = now;
                }
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
        
        // ===== TOPPING BILLBOARD SYSTEM =====
        
        private void InitializeBillboards()
        {
            // Load billboard shader
            _billboardProgram = ShaderHelper.LoadProgram(global::Android.App.Application.Context, "billboard.vert", "billboard.frag");
            
            // Create quad VBO (2 triangles, 4 vertices)
            // Layout: position (vec2), texcoord (vec2)
            float[] quadData = new float[] {
                // Corner,  TexCoord
                -1, -1,     0, 0,  // Bottom-left
                 1, -1,     1, 0,  // Bottom-right
                 1,  1,     1, 1,  // Top-right
                -1,  1,     0, 1   // Top-left
            };
            
            int[] buffers = new int[1];
            GLES30.GlGenBuffers(1, buffers, 0);
            _billboardVBO = buffers[0];
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _billboardVBO);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, quadData.Length * 4, Java.Nio.FloatBuffer.Wrap(quadData), GLES30.GlStaticDraw);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
            
            // Generate topping textures
            _sauceTexture = GenerateSauceTexture();
            _mayoTexture = GenerateMayoTexture();
            
            global::Android.Util.Log.Debug("TakoyakiBillboard", "Billboards initialized!");
        }
        
        private int GenerateSauceTexture()
        {
            int size = 64;
            byte[] pixels = new byte[size * size * 4];
            
            // Simple brown circle for MVP
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
                    
                    int idx = (y * size + x) * 4;
                    if (dist < 0.8f)
                    {
                        pixels[idx + 0] = 80;  // R (brown)
                        pixels[idx + 1] = 40;  // G
                        pixels[idx + 2] = 20;  // B
                        pixels[idx + 3] = (byte)(255 * (1.0f - dist)); // Alpha (fade out)
                    }
                    else
                    {
                        pixels[idx + 3] = 0; // Transparent
                    }
                }
            }
            
            return CreateTexture(pixels, size, size);
        }
        
        private int GenerateMayoTexture()
        {
            int size = 64;
            byte[] pixels = new byte[size * size * 4];
            
            // Simple white circle for MVP
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
                    
                    int idx = (y * size + x) * 4;
                    if (dist < 0.6f)
                    {
                        pixels[idx + 0] = 255; // R (white)
                        pixels[idx + 1] = 255; // G
                        pixels[idx + 2] = 200; // B (slightly yellow)
                        pixels[idx + 3] = (byte)(255 * (1.0f - dist)); // Alpha
                    }
                    else
                    {
                        pixels[idx + 3] = 0; // Transparent
                    }
                }
            }
            
            return CreateTexture(pixels, size, size);
        }
        
        private int CreateTexture(byte[] pixels, int width, int height)
        {
            int[] textures = new int[1];
            GLES30.GlGenTextures(1, textures, 0);
            int texId = textures[0];
            
            GLES30.GlBindTexture(GLES30.GlTexture2d, texId);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMinFilter, GLES30.GlLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMagFilter, GLES30.GlLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureWrapS, GLES30.GlClampToEdge);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureWrapT, GLES30.GlClampToEdge);
            
            var buffer = Java.Nio.ByteBuffer.Wrap(pixels);
            GLES30.GlTexImage2D(GLES30.GlTexture2d, 0, GLES30.GlRgba, width, height, 0, GLES30.GlRgba, GLES30.GlUnsignedByte, buffer);
            
            return texId;
        }
        
        private void RenderToppings()
        {
            // Enable blending for transparency
            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            GLES30.GlDisable(GLES30.GlDepthTest); // Render on top
            
            GLES30.GlUseProgram(_billboardProgram);
            
            // Bind VBO
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _billboardVBO);
            GLES30.GlEnableVertexAttribArray(0); // aCorner
            GLES30.GlVertexAttribPointer(0, 2, GLES30.GlFloat, false, 16, 0);
            GLES30.GlEnableVertexAttribArray(1); // aTexCoord
            GLES30.GlVertexAttribPointer(1, 2, GLES30.GlFloat, false, 16, 8);
            
            // Get uniform locations
            int uMVP = GLES30.GlGetUniformLocation(_billboardProgram, "uMVPMatrix");
            int uPos = GLES30.GlGetUniformLocation(_billboardProgram, "uBillboardPos");
            int uSize = GLES30.GlGetUniformLocation(_billboardProgram, "uBillboardSize");
            int uTex = GLES30.GlGetUniformLocation(_billboardProgram, "uTexture");
            int uColor = GLES30.GlGetUniformLocation(_billboardProgram, "uColor");
            
            GLES30.GlUniformMatrix4fv(uMVP, 1, false, _mvpMatrix, 0);
            GLES30.GlUniform1i(uTex, 0); // Texture unit 0
            
            // Render Sauce
            if (_sauceVisible)
            {
                GLES30.GlActiveTexture(GLES30.GlTexture0);
                GLES30.GlBindTexture(GLES30.GlTexture2d, _sauceTexture);
                GLES30.GlUniform3f(uPos, -0.3f, 0.3f, 0.0f); // Position on ball
                GLES30.GlUniform2f(uSize, 0.8f, 0.8f); // Size
                GLES30.GlUniform4f(uColor, 1.0f, 1.0f, 1.0f, 1.0f); // White tint
                GLES30.GlDrawArrays(GLES30.GlTriangleFan, 0, 4);
            }
            
            // Render Mayo
            if (_mayoVisible)
            {
                GLES30.GlActiveTexture(GLES30.GlTexture0);
                GLES30.GlBindTexture(GLES30.GlTexture2d, _mayoTexture);
                GLES30.GlUniform3f(uPos, 0.3f, -0.2f, 0.0f); // Different position
                GLES30.GlUniform2f(uSize, 0.6f, 0.6f);
                GLES30.GlUniform4f(uColor, 1.0f, 1.0f, 1.0f, 1.0f);
                GLES30.GlDrawArrays(GLES30.GlTriangleFan, 0, 4);
            }
            
            // Render Aonori Particles (as small green squares)
            if (_aonoriParticles.Count > 0)
            {
                // Use a simple green texture (or reuse mayo texture with green tint)
                GLES30.GlActiveTexture(GLES30.GlTexture0);
                GLES30.GlBindTexture(GLES30.GlTexture2d, _mayoTexture); // Reuse
                
                foreach (var particle in _aonoriParticles)
                {
                    GLES30.GlUniform3f(uPos, particle.X, particle.Y, particle.Z);
                    GLES30.GlUniform2f(uSize, 0.1f, 0.1f); // Small
                    GLES30.GlUniform4f(uColor, 0.2f, 0.8f, 0.2f, 1.0f); // Green
                    GLES30.GlDrawArrays(GLES30.GlTriangleFan, 0, 4);
                }
            }
            
            // Cleanup
            GLES30.GlDisableVertexAttribArray(0);
            GLES30.GlDisableVertexAttribArray(1);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlDisable(GLES30.GlBlend);
        }
    }
}
