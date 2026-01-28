using Android.Opengl;
using Android.Views;
using Takoyaki.Core;
using Javax.Microedition.Khronos.Opengles;
using Javax.Microedition.Khronos.Egl;
using System;
using System.Collections.Generic;

namespace Takoyaki.Android
{
    public class TakoyakiRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        // --- Modules ---
        public TakoyakiAssets Assets { get; } = new TakoyakiAssets();
        public TakoyakiInputHandler Input { get; }
        private readonly TakoyakiHaptics _haptics;
        private readonly TakoyakiAudio _audio;
        private readonly TakoyakiSensor _sensor;
        private TakoyakiToppings _toppings;

        // --- Simulation ---
        private TakoyakiBall _ball;
        private SoftBodySolver _physics;
        private HeatSimulation _heat;
        private TakoyakiStateMachine _stateMachine;
        private InputState _inputState;

        // --- Rendering State ---
        private float[] _modelMatrix = new float[16];
        private float[] _viewMatrix = new float[16];
        private float[] _projectionMatrix = new float[16];
        private float[] _mvpMatrix = new float[16];
        
        private int _vbo, _ibo, _vao;
        private int _indexCount;
        private float[] _meshData;
        
        private long _lastTimeNs;
        private float _totalTime;
        private int _toppingStage = 0;

        // Uniform Handles
        private int _uMVPMatrixHandle;
        private int _uModelMatrixHandle;
        private int _uVPMatrixHandle;
        private int _uTimeHandle;

        public Action<int> OnGameFinished;

        public TakoyakiRenderer(TakoyakiSensor sensor, TakoyakiHaptics haptics, TakoyakiAudio audio)
        {
            _sensor = sensor;
            _haptics = haptics;
            _audio = audio;
            _inputState = new InputState();
            Input = new TakoyakiInputHandler(_sensor, _inputState);
        }

        public void OnSurfaceCreated(IGL10 gl, EGLConfig config)
        {
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            GLES30.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f);

            // 1. Initialize Assets
            Assets.Initialize(global::Android.App.Application.Context);

            // 2. Initialize Core Logic
            var mesh = ProceduralMesh.GenerateSphere(64); 
            _meshData = mesh.ToInterleavedArray();
            _indexCount = mesh.Indices.Length;
            
            int vCount = mesh.Vertices.Length / 3;
            _ball = new TakoyakiBall(0, vCount);
            
            // Sync initial vertices
            for(int i=0; i<vCount; i++) {
                var vec = new System.Numerics.Vector3(mesh.Vertices[i*3], mesh.Vertices[i*3+1], mesh.Vertices[i*3+2]);
                _ball.BaseVertices[i] = vec;
                _ball.DeformedVertices[i] = vec;
            }

            _physics = new SoftBodySolver(_ball);
            _heat = new HeatSimulation(_ball);
            _stateMachine = new TakoyakiStateMachine(_ball, _audio);
            _stateMachine.OnFinished = (score) => OnGameFinished?.Invoke(score);

            // 3. Setup Shaders
            int program = Assets.MainProgram;
            GLES30.GlUseProgram(program);
            
            _uMVPMatrixHandle = GLES30.GlGetUniformLocation(program, "uMVPMatrix");
            _uModelMatrixHandle = GLES30.GlGetUniformLocation(program, "uModelMatrix");
            _uVPMatrixHandle = GLES30.GlGetUniformLocation(program, "uVPMatrix");
            _uTimeHandle = GLES30.GlGetUniformLocation(program, "uTime");

            // 4. VAO/VBO Setup
            int[] vaos = new int[1]; GLES30.GlGenVertexArrays(1, vaos, 0); _vao = vaos[0];
            GLES30.GlBindVertexArray(_vao);
            
            int[] buffers = new int[2]; GLES30.GlGenBuffers(2, buffers, 0);
            _vbo = buffers[0]; _ibo = buffers[1];
            
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, _meshData.Length * 4, Java.Nio.FloatBuffer.Wrap(_meshData), GLES30.GlDynamicDraw);
            
            int stride = 8 * 4;
            GLES30.GlEnableVertexAttribArray(0); GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1); GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, stride, 3 * 4);
            GLES30.GlEnableVertexAttribArray(2); GLES30.GlVertexAttribPointer(2, 2, GLES30.GlFloat, false, stride, 6 * 4);

            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, _ibo);
            GLES30.GlBufferData(GLES30.GlElementArrayBuffer, mesh.Indices.Length * 2, Java.Nio.ShortBuffer.Wrap(mesh.Indices), GLES30.GlStaticDraw);
            
            GLES30.GlBindVertexArray(0);

            // 5. Initialize Toppings
            _toppings = new TakoyakiToppings();
            _toppings.Initialize(Assets.ToppingProgram);
            _toppings.GenerateToppings();
            
            _lastTimeNs = Java.Lang.JavaSystem.NanoTime();
        }

        public void OnSurfaceChanged(IGL10 gl, int width, int height)
        {
            GLES30.GlViewport(0, 0, width, height);
            float ratio = (float)width / height;
            Matrix.FrustumM(_projectionMatrix, 0, -ratio, ratio, -1, 1, 2, 10);
            Matrix.SetLookAtM(_viewMatrix, 0, 0f, 0f, 6f, 0f, 0f, 0f, 0f, 1f, 0f);
        }

        public void OnDrawFrame(IGL10 gl)
        {
            long now = Java.Lang.JavaSystem.NanoTime();
            float dt = (now - _lastTimeNs) / 1_000_000_000.0f;
            _lastTimeNs = now;
            _totalTime += dt;

            // 1. Update Input & Logic
            Input.Update(dt);
            UpdateLogic(dt);

            // 2. Render
            GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
            
            // Calculate MVP
            Matrix.MultiplyMM(_mvpMatrix, 0, _viewMatrix, 0, _modelMatrix, 0);
            Matrix.MultiplyMM(_mvpMatrix, 0, _projectionMatrix, 0, _mvpMatrix, 0);
            
            float[] vpMatrix = new float[16];
            Matrix.MultiplyMM(vpMatrix, 0, _projectionMatrix, 0, _viewMatrix, 0);

            // Bind Main Program
            GLES30.GlUseProgram(Assets.MainProgram);
            GLES30.GlUniformMatrix4fv(_uMVPMatrixHandle, 1, false, _mvpMatrix, 0);
            GLES30.GlUniformMatrix4fv(_uModelMatrixHandle, 1, false, _modelMatrix, 0);
            GLES30.GlUniformMatrix4fv(_uVPMatrixHandle, 1, false, vpMatrix, 0);
            GLES30.GlUniform1f(_uTimeHandle, _totalTime);

            // Sync Uniforms
            int uBatterLvl = GLES30.GlGetUniformLocation(Assets.MainProgram, "uBatterLevel");
            GLES30.GlUniform1f(uBatterLvl, _ball.BatterLevel);
            int uCookLvl = GLES30.GlGetUniformLocation(Assets.MainProgram, "uCookLevel");
            GLES30.GlUniform1f(uCookLvl, _ball.CookLevel);
            
            Assets.BindTextures();

            // Render Ball
            UpdateMeshVBO();
            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawElements(GLES30.GlTriangles, _indexCount, GLES30.GlUnsignedShort, 0);
            GLES30.GlBindVertexArray(0);

            // Render Toppings
            _toppings.RenderRecursive(vpMatrix, _modelMatrix, _totalTime);
        }

        private void UpdateLogic(float dt)
        {
            // Shake to apply topping
            float accelMag = _inputState.Acceleration.Length();
            if (accelMag > TakoyakiConstants.SHAKE_THRESHOLD) {
                ApplyTopping();
            }

            // Core Phys & Machine
            _physics.Update(dt, System.Numerics.Vector3.Zero, new System.Numerics.Vector3(_inputState.Tilt.X * 9.8f, -9.8f, _inputState.Tilt.Y * 9.8f));
            _stateMachine.Update(_inputState, dt);
            
            // Model Matrix calculation (simplified here, but robust)
            Matrix.SetIdentityM(_modelMatrix, 0);
            if (_stateMachine.CurrentState is StateTurned) Matrix.RotateM(_modelMatrix, 0, 180f, 1, 0, 0);
            Matrix.RotateM(_modelMatrix, 0, _inputState.Tilt.X * 45f, 0, 0, 1);
            Matrix.RotateM(_modelMatrix, 0, _inputState.Tilt.Y * 45f, 1, 0, 0);
        }

        private void UpdateMeshVBO()
        {
            // Extract jiggling vertices
            for (int i = 0; i < _ball.BaseVertices.Length; i++) {
                _meshData[i * 8 + 0] = _ball.DeformedVertices[i].X;
                _meshData[i * 8 + 1] = _ball.DeformedVertices[i].Y;
                _meshData[i * 8 + 2] = _ball.DeformedVertices[i].Z;
            }
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            GLES30.GlBufferSubData(GLES30.GlArrayBuffer, 0, _meshData.Length * 4, Java.Nio.FloatBuffer.Wrap(_meshData));
        }

        public void ApplyTopping()
        {
            if (_toppingStage < 4) {
                _toppings.SetToppingVisible(_toppingStage++);
                _haptics.TriggerImpact(0.5f);
            }
        }

        public void Reset() {
            _toppingStage = 0;
            _toppings?.Reset();
            _stateMachine?.Reset();
        }
    }
}
