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

        private long _lastTimeNs;

        public void OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
        {
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlClearColor(0.1f, 0.1f, 0.1f, 1.0f);

            // 1. Init Core Logic
            _ball = new Takoyaki.Core.TakoyakiBall(0, 2000); // 2000 dummy vert count
            _physics = new Takoyaki.Core.SoftBodySolver(_ball);
            _heatDelay = new Takoyaki.Core.HeatSimulation(_ball);

            // 2. Load Shaders
            _program = ShaderHelper.LoadProgram(Android.App.Application.Context, "takoyaki.vert", "takoyaki.frag");
            GLES30.GlUseProgram(_program);

            _uMVPMatrixHandle = GLES30.GlGetUniformLocation(_program, "uMVPMatrix");
            _uModelMatrixHandle = GLES30.GlGetUniformLocation(_program, "uModelMatrix");

            // Texture Uniforms
            int uBatter = GLES30.GlGetUniformLocation(_program, "uBatterTex");
            int uCooked = GLES30.GlGetUniformLocation(_program, "uCookedTex");
            int uBurnt = GLES30.GlGetUniformLocation(_program, "uBurntTex");
            int uNoise = GLES30.GlGetUniformLocation(_program, "uNoiseMap");

            GLES30.GlUniform1i(uBatter, 0);
            GLES30.GlUniform1i(uCooked, 1);
            GLES30.GlUniform1i(uBurnt, 2);
            GLES30.GlUniform1i(uNoise, 3);

            // Generate & Upload Textures
            LoadTexture(0, Takoyaki.Core.ProceduralTexture.GenerateBatter(512));
            LoadTexture(1, Takoyaki.Core.ProceduralTexture.GenerateCooked(512));
            LoadTexture(2, Takoyaki.Core.ProceduralTexture.GenerateBurnt(512));
            LoadTexture(3, Takoyaki.Core.ProceduralTexture.GenerateNoiseMap(512));

            // 3. Generate Mesh & Buffers
            var mesh = Takoyaki.Core.ProceduralMesh.GenerateSphere(64); // High res
            float[] meshData = mesh.ToInterleavedArray();
            short[] indices = mesh.Indices;
            _indexCount = indices.Length;

            // Generate VAO
            int[] vaos = new int[1];
            GLES30.GlGenVertexArrays(1, vaos, 0);
            _vao = vaos[0];
            GLES30.GlBindVertexArray(_vao);

            // Generate VBO
            int[] buffers = new int[2];
            GLES30.GlGenBuffers(2, buffers, 0);
            _vbo = buffers[0];
            _ibo = buffers[1];

            // Upload VBO
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            // C# Float[] -> Byte size
            int bytes = meshData.Length * 4;
            GLES30.GlBufferData(GLES30.GlArrayBuffer, bytes, java.nio.FloatBuffer.Wrap(meshData), GLES30.GlStaticDraw);

            // Attributes (Interleaved: Pos(3), Norm(3), UV(2) = Stride 8 floats)
            int stride = 8 * 4;
            
            // Pos
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            
            // Norm
            GLES30.GlEnableVertexAttribArray(1);
            GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, stride, 3 * 4);
            
            // UV
            GLES30.GlEnableVertexAttribArray(2);
            GLES30.GlVertexAttribPointer(2, 2, GLES30.GlFloat, false, stride, 6 * 4);

            // Upload IBO
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, _ibo);
            GLES30.GlBufferData(GLES30.GlElementArrayBuffer, indices.Length * 2, java.nio.ShortBuffer.Wrap(indices), GLES30.GlStaticDraw);

            // Unbind
            GLES30.GlBindVertexArray(0);

            // View Matrix (LookAt)
            Matrix.SetLookAtM(_viewMatrix, 0, 0, 4, 4, 0, 0, 0, 0, 1, 0);
            
            _lastTimeNs = System.nanoTime();
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
            var buffer = java.nio.ByteBuffer.Wrap(tex.Pixels);
            GLES30.GlTexImage2D(GLES30.GlTexture2d, 0, GLES30.GlRgba, tex.Width, tex.Height, 0, GLES30.GlRgba, GLES30.GlUnsignedByte, buffer);
            GLES30.GlGenerateMipmap(GLES30.GlTexture2d);
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            GLES30.GlViewport(0, 0, width, height);
            float ratio = (float)width / height;
            Matrix.FrustumM(_projectionMatrix, 0, -ratio, ratio, -1, 1, 2, 10);
        }

        public void OnDrawFrame(IGL10? gl)
        {
            // Time Delta
            long now = System.nanoTime();
            float dt = (now - _lastTimeNs) / 1_000_000_000.0f;
            _lastTimeNs = now;

            // 1. Update Core
            // In a real app, do this on a separate thread or fixed timestep
            
            // Rotate the ball model
            Matrix.SetIdentityM(_modelMatrix, 0);
            Matrix.RotateM(_modelMatrix, 0, (float)(System.nanoTime() / 1e8), 0, 1, 0); // Spin demo

            // 2. Render
            GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);

            GLES30.GlUseProgram(_program);
            GLES30.GlBindVertexArray(_vao);

            // Calc MVP
            Matrix.MultiplyMM(_mvpMatrix, 0, _viewMatrix, 0, _modelMatrix, 0); // VM
            Matrix.MultiplyMM(_mvpMatrix, 0, _projectionMatrix, 0, _mvpMatrix, 0); // PVM

            GLES30.GlUniformMatrix4fv(_uMVPMatrixHandle, 1, false, _mvpMatrix, 0);
            GLES30.GlUniformMatrix4fv(_uModelMatrixHandle, 1, false, _modelMatrix, 0);

            // Set uniforms (Dummy values for now)
            // GLES30.GlUniform1f(...) for CookLevel, etc.

            GLES30.GlDrawElements(GLES30.GlTriangles, _indexCount, GLES30.GlUnsignedShort, 0);
            GLES30.GlBindVertexArray(0);
        }
    }
}
