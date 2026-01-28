using Android.Opengl;
using Java.Nio;
using System;

namespace Takoyaki.Android
{
    public class MasterAura
    {
        private int _program;
        private int _vao, _vbo;
        private float[] _quadData = {
            // Pos(X,Y,Z), UV(U,V)
            -1.0f, -1.0f, 0.0f,  0.0f, 0.0f,
             1.0f, -1.0f, 0.0f,  1.0f, 0.0f,
            -1.0f,  1.0f, 0.0f,  0.0f, 1.0f,
             1.0f,  1.0f, 0.0f,  1.0f, 1.0f
        };

        public MasterAura(global::Android.Content.Context context)
        {
            _program = ShaderHelper.LoadProgram(context, "aura.vert", "aura.frag");

            int[] buffers = new int[1]; GLES30.GlGenBuffers(1, buffers, 0); _vbo = buffers[0];
            int[] vaos = new int[1]; GLES30.GlGenVertexArrays(1, vaos, 0); _vao = vaos[0];

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, _quadData.Length * 4, FloatBuffer.Wrap(_quadData), GLES30.GlStaticDraw);

            int stride = 5 * 4;
            GLES30.GlEnableVertexAttribArray(0); GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1); GLES30.GlVertexAttribPointer(1, 2, GLES30.GlFloat, false, stride, 3 * 4);
            GLES30.GlBindVertexArray(0);
        }

        public void Draw(float[] vpMatrix, float[] modelMatrix, float intensity, float time)
        {
            if (intensity <= 0.01f) return;

            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOne); // Additive
            GLES30.GlDepthMask(false);

            GLES30.GlUseProgram(_program);
            
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_program, "uVPMatrix"), 1, false, vpMatrix, 0);
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_program, "uModelMatrix"), 1, false, modelMatrix, 0);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_program, "uScale"), 2.0f); // Size of the aura
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_program, "uTime"), time);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_program, "uIntensity"), intensity);
            
            // Gold color aura
            GLES30.GlUniform4f(GLES30.GlGetUniformLocation(_program, "uColor"), 1.0f, 0.8f, 0.2f, 0.7f);

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawArrays(GLES30.GlTriangleStrip, 0, 4);
            GLES30.GlBindVertexArray(0);

            GLES30.GlDepthMask(true);
            GLES30.GlDisable(GLES30.GlBlend);
        }
    }
}
