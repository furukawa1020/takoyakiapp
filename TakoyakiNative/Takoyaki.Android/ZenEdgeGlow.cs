using Android.Opengl;
using Java.Nio;
using System;

namespace Takoyaki.Android
{
    public class ZenEdgeGlow
    {
        private int _program;
        private int _vao, _vbo;
        private float[] _orthoMatrix = new float[16];
        
        private float[] _quadData = {
            -1.0f, -1.0f,  0.0f, 0.0f,
             1.0f, -1.0f,  1.0f, 0.0f,
            -1.0f,  1.0f,  0.0f, 1.0f,
             1.0f,  1.0f,  1.0f, 1.0f
        };

        public ZenEdgeGlow(global::Android.Content.Context context)
        {
            // We use hud.vert but a specialized vignette frag
            _program = ShaderHelper.LoadProgram(context, "hud.vert", "zen_glow.frag");

            int[] buffers = new int[1]; GLES30.GlGenBuffers(1, buffers, 0); _vbo = buffers[0];
            int[] vaos = new int[1]; GLES30.GlGenVertexArrays(1, vaos, 0); _vao = vaos[0];

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, _quadData.Length * 4, FloatBuffer.Wrap(_quadData), GLES30.GlStaticDraw);

            int stride = 4 * 4;
            GLES30.GlEnableVertexAttribArray(0); GLES30.GlVertexAttribPointer(0, 2, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1); GLES30.GlVertexAttribPointer(1, 2, GLES30.GlFloat, false, stride, 2 * 4);
            GLES30.GlBindVertexArray(0);

            Matrix.OrthoM(_orthoMatrix, 0, -1, 1, -1, 1, -1, 1);
        }

        public void Draw(float mastery)
        {
            if (mastery <= 0.05f) return;

            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOne); // Additive Glow
            GLES30.GlDisable(GLES30.GlDepthTest);

            GLES30.GlUseProgram(_program);
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_program, "uOrthoMatrix"), 1, false, _orthoMatrix, 0);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_program, "uIntensity"), mastery);
            GLES30.GlUniform4f(GLES30.GlGetUniformLocation(_program, "uColor"), 1.0f, 0.8f, 0.2f, 0.4f);

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4);
            GLES30.GlBindVertexArray(0);

            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlDisable(GLES30.GlBlend);
        }
    }
}
