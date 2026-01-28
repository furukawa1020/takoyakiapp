using Android.Opengl;
using Java.Nio;
using System;

namespace Takoyaki.Android
{
    public class ShapingGuidance
    {
        private int _program;
        private int _vao, _vbo;
        private float[] _orthoMatrix = new float[16];
        
        private float[] _quadData = {
            // Pos(X,Y), UV(U,V)
            -0.8f, -0.9f,  0.0f, 0.0f,
             0.8f, -0.9f,  1.0f, 0.0f,
            -0.8f, -0.8f,  0.0f, 1.0f,
             0.8f, -0.8f,  1.0f, 1.0f
        };

        public ShapingGuidance(global::Android.Content.Context context)
        {
            _program = ShaderHelper.LoadProgram(context, "hud.vert", "hud.frag");

            int[] buffers = new int[1];
            GLES30.GlGenBuffers(1, buffers, 0);
            _vbo = buffers[0];

            int[] vaos = new int[1];
            GLES30.GlGenVertexArrays(1, vaos, 0);
            _vao = vaos[0];

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, _quadData.Length * 4, FloatBuffer.Wrap(_quadData), GLES30.GlStaticDraw);

            int stride = 4 * 4;
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 2, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1);
            GLES30.GlVertexAttribPointer(1, 2, GLES30.GlFloat, false, stride, 2 * 4);
            GLES30.GlBindVertexArray(0);

            Matrix.OrthoM(_orthoMatrix, 0, -1, 1, -1, 1, -1, 1);
        }

        public void Draw(float currentGyro, float targetGyro, float mastery, float pulse)
        {
            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            GLES30.GlDisable(GLES30.GlDepthTest);

            GLES30.GlUseProgram(_program);
            
            int uOrtho = GLES30.GlGetUniformLocation(_program, "uOrthoMatrix");
            int uColor = GLES30.GlGetUniformLocation(_program, "uColor");
            int uProgress = GLES30.GlGetUniformLocation(_program, "uProgress");
            int uType = GLES30.GlGetUniformLocation(_program, "uType");

            GLES30.GlUniformMatrix4fv(uOrtho, 1, false, _orthoMatrix, 0);

            // 1. Draw Background Bar
            GLES30.GlUniform1i(uType, 0);
            GLES30.GlUniform4f(uColor, 0.1f, 0.1f, 0.1f, 0.6f);
            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4);

            // 2. Draw Progress Bar (Mastery)
            GLES30.GlUniform1i(uType, 1);
            // Color shifts from Red to Gold
            vec4 col = mastery > 0.8f ? new vec4(1, 0.9f, 0, 0.9f) : new vec4(0, 0.8f, 1.0f, 0.8f);
            GLES30.GlUniform4f(uColor, col.X, col.Y, col.Z, col.W);
            GLES30.GlUniform1f(uProgress, currentGyro / (targetGyro * 1.5f));
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4);

            // 3. Draw Rhythm Pulse Indicator (A dot or glow)
            // Pulse guidance
            float alpha = 0.3f + pulse * 0.4f;
            GLES30.GlUniform1i(uType, 0);
            GLES30.GlUniform4f(uColor, 1, 1, 1, alpha);
            // Dynamic scale for pulse
            // (We could use another VBO or just scale the matrix, let's keep it simple for now)

            GLES30.GlBindVertexArray(0);
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlDisable(GLES30.GlBlend);
        }

        private struct vec4 { public float X, Y, Z, W; public vec4(float x, float y, float z, float w) { X=x;Y=y;Z=z;W=w;} }
    }
}
