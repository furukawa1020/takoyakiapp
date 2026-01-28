using Android.Opengl;
using Java.Nio;
using System;

namespace Takoyaki.Android
{
    public class ShapingSparkles
    {
        private const int MAX_PARTICLES = 150;
        private float[] _particleData; // x, y, z, alpha
        private Particle[] _particles;
        private int _program;
        private int _vao, _vbo;
        private Random _rand = new Random();
        private int _activeCount = 0;

        private struct Particle
        {
            public float X, Y, Z;
            public float Angle;
            public float Radius;
            public float Speed;
            public float Life;
            public bool Active;
        }

        public ShapingSparkles(global::Android.Content.Context context)
        {
            _particles = new Particle[MAX_PARTICLES];
            _particleData = new float[MAX_PARTICLES * 4];

            _program = ShaderHelper.LoadProgram(context, "sparkle.vert", "sparkle.frag");

            int[] buffers = new int[1];
            GLES30.GlGenBuffers(1, buffers, 0);
            _vbo = buffers[0];

            int[] vaos = new int[1];
            GLES30.GlGenVertexArrays(1, vaos, 0);
            _vao = vaos[0];

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, _particleData.Length * 4, null, GLES30.GlDynamicDraw);

            int stride = 4 * 4;
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1);
            GLES30.GlVertexAttribPointer(1, 1, GLES30.GlFloat, false, stride, 3 * 4);
            GLES30.GlBindVertexArray(0);
        }

        public void Update(float dt, float mastery)
        {
            // Spawn sparkles based on mastery level
            if (mastery > 0.1f)
            {
                int spawnCount = (int)(mastery * 5); 
                for(int k=0; k<spawnCount; k++) SpawnOne();
            }

            int count = 0;
            for (int i = 0; i < MAX_PARTICLES; i++)
            {
                if (_particles[i].Active)
                {
                    _particles[i].Angle += _particles[i].Speed * dt;
                    _particles[i].X = (float)Math.Cos(_particles[i].Angle) * _particles[i].Radius;
                    _particles[i].Z = (float)Math.Sin(_particles[i].Angle) * _particles[i].Radius;
                    _particles[i].Y += (float)(_rand.NextDouble() - 0.5) * 0.1f; // Vertical drift
                    
                    _particles[i].Life -= dt * 1.5f;

                    if (_particles[i].Life <= 0)
                    {
                        _particles[i].Active = false;
                    }
                    else
                    {
                        _particleData[count * 4 + 0] = _particles[i].X;
                        _particleData[count * 4 + 1] = _particles[i].Y;
                        _particleData[count * 4 + 2] = _particles[i].Z;
                        _particleData[count * 4 + 3] = _particles[i].Life;
                        count++;
                    }
                }
            }
            _activeCount = count;

            if (count > 0)
            {
                GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
                GLES30.GlBufferSubData(GLES30.GlArrayBuffer, 0, count * 16, FloatBuffer.Wrap(_particleData));
            }
        }

        private void SpawnOne()
        {
            for (int i = 0; i < MAX_PARTICLES; i++)
            {
                if (!_particles[i].Active)
                {
                    _particles[i].Active = true;
                    _particles[i].Angle = (float)(_rand.NextDouble() * Math.PI * 2.0);
                    _particles[i].Radius = 1.1f + (float)_rand.NextDouble() * 0.2f;
                    _particles[i].Speed = 2.0f + (float)_rand.NextDouble() * 3.0f;
                    _particles[i].X = (float)Math.Cos(_particles[i].Angle) * _particles[i].Radius;
                    _particles[i].Y = (float)(_rand.NextDouble() - 0.5) * 0.5f;
                    _particles[i].Z = (float)Math.Sin(_particles[i].Angle) * _particles[i].Radius;
                    _particles[i].Life = 0.8f + (float)_rand.NextDouble() * 0.4f;
                    return;
                }
            }
        }

        public void Draw(float[] mvpMatrix)
        {
            if (_activeCount == 0) return;

            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOne); // Additive
            GLES30.GlDepthMask(false);

            GLES30.GlUseProgram(_program);
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_program, "uMVPMatrix"), 1, false, mvpMatrix, 0);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_program, "uPointSize"), 30.0f);

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawArrays(GLES30.GlPoints, 0, _activeCount);
            GLES30.GlBindVertexArray(0);

            GLES30.GlDepthMask(true);
            GLES30.GlDisable(GLES30.GlBlend);
        }
    }
}
