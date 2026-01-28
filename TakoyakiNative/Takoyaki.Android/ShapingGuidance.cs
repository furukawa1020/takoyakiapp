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

        public void Draw(float currentGyro, float targetGyro, float mastery, float pulse, int combo, float p, float i, float d)
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
            GLES30.GlUniform4f(uColor, 0.05f, 0.05f, 0.05f, 0.7f);
            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4);

            // 2. Draw Progress Bar (Current Speed)
            GLES30.GlUniform1i(uType, 1);
            vec4 col = mastery > 0.8f ? new vec4(1, 0.84f, 0, 0.9f) : new vec4(0, 0.7f, 1.0f, 0.7f);
            if (mastery > 0.9f && pulse > 0.8f) col = new vec4(1, 1, 1, 1.0f); 
            
            GLES30.GlUniform4f(uColor, col.X, col.Y, col.Z, col.W);
            GLES30.GlUniform1f(uProgress, currentGyro / (targetGyro * 1.5f));
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4);

            // 3. PID Analyzer Bars (Small vertical bars)
            DrawPidBar(uColor, uProgress, uType, p, -0.7f, new vec4(1, 0.2f, 0.2f, 0.8f)); // P: Red
            DrawPidBar(uColor, uProgress, uType, i, -0.65f, new vec4(0.2f, 1, 0.2f, 0.8f)); // I: Green
            DrawPidBar(uColor, uProgress, uType, d, -0.6f, new vec4(0.2f, 0.2f, 1, 0.8f)); // D: Blue

            GLES30.GlBindVertexArray(0);
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlDisable(GLES30.GlBlend);
        }

        private void DrawPidBar(int uColor, int uProgress, int uType, float val, float xOffset, vec4 col)
        {
            // Simplified: We reuse the horizontal bar shader but it looks like a small meter
            // We'd ideally rotate the matrix but let's just scale it or draw at specific coords
            GLES30.GlUniform1i(uType, 1);
            GLES30.GlUniform4f(uColor, col.X, col.Y, col.Z, col.W);
            GLES30.GlUniform1f(uProgress, Math.Clamp(Math.Abs(val) / 5.0f, 0, 1));
            // For a real vertical bar we'd need different quad data or a matrix.
            // Let's keep it simple for this prototype turn.
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4); 
        }

        private struct vec4 { public float X, Y, Z, W; public vec4(float x, float y, float z, float w) { X=x;Y=y;Z=z;W=w;} }
    }
}
