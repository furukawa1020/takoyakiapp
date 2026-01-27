using Android.Opengl;
using Java.Nio;
using System;
using System.Collections.Generic;

namespace Takoyaki.Android
{
    public class SteamParticles
    {
        private const int MAX_PARTICLES = 100;
        private float[] _particleData; // x, y, z, alpha
        private Particle[] _particles;
        private int _program;
        private int _vao, _vbo;
        private Random _rand = new Random();

        private struct Particle
        {
            public float X, Y, Z;
            public float Vy; // Velocity Y (Rising)
            public float Life; // 1.0 to 0.0
            public bool Active;
        }

        public SteamParticles(Android.Content.Context context)
        {
            _particles = new Particle[MAX_PARTICLES];
            _particleData = new float[MAX_PARTICLES * 4]; // 4 floats per particle

            // Load Shader
            _program = ShaderHelper.LoadProgram(context, "steam.vert", "steam.frag");

            // Setup GL
            int[] buffers = new int[1];
            GLES30.GlGenBuffers(1, buffers, 0);
            _vbo = buffers[0];

            int[] vaos = new int[1];
            GLES30.GlGenVertexArrays(1, vaos, 0);
            _vao = vaos[0];

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
            
            // Initial null data
            GLES30.GlBufferData(GLES30.GlArrayBuffer, _particleData.Length * 4, null, GLES30.GlDynamicDraw);

            // Attribs: Pos(3) + Alpha(1)
            int stride = 4 * 4;
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            
            GLES30.GlEnableVertexAttribArray(1);
            GLES30.GlVertexAttribPointer(1, 1, GLES30.GlFloat, false, stride, 3 * 4);

            GLES30.GlBindVertexArray(0);
        }

        public void Update(float dt, float intensity)
        {
            // Spawn
            if (intensity > 0.1f)
            {
                int spawnCount = (int)(intensity * 2); // 0-2 per frame
                for(int k=0; k<spawnCount; k++)
                {
                    SpawnOne();
                }
            }

            int activeCount = 0;
            // Update logic
            for (int i = 0; i < MAX_PARTICLES; i++)
            {
                if (_particles[i].Active)
                {
                    _particles[i].Y += _particles[i].Vy * dt;
                    _particles[i].X += (float)(_rand.NextDouble() - 0.5) * 0.5f * dt; // Jitter
                    _particles[i].Life -= dt * 0.8f; // Fade Speed

                    if (_particles[i].Life <= 0)
                    {
                        _particles[i].Active = false;
                    }
                    else
                    {
                        // Fill Buffer Data
                        _particleData[activeCount * 4 + 0] = _particles[i].X;
                        _particleData[activeCount * 4 + 1] = _particles[i].Y;
                        _particleData[activeCount * 4 + 2] = _particles[i].Z;
                        _particleData[activeCount * 4 + 3] = _particles[i].Life;
                        activeCount++;
                    }
                }
            }

            // Upload
            if (activeCount > 0)
            {
                GLES30.GlBindBuffer(GLES30.GlArrayBuffer, _vbo);
                GLES30.GlBufferSubData(GLES30.GlArrayBuffer, 0, activeCount * 4 * 4, FloatBuffer.Wrap(_particleData));
                GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
            }
            _activeCount = activeCount;
        }

        private int _activeCount = 0;

        private void SpawnOne()
        {
            for (int i = 0; i < MAX_PARTICLES; i++)
            {
                if (!_particles[i].Active)
                {
                    _particles[i].Active = true;
                    // Spawn near center top of ball (0, 0.5, 0) approx
                    _particles[i].X = (float)(_rand.NextDouble() - 0.5) * 0.5f;
                    _particles[i].Y = 0.5f; 
                    _particles[i].Z = (float)(_rand.NextDouble() - 0.5) * 0.5f;
                    _particles[i].Vy = 1.0f + (float)_rand.NextDouble();
                    _particles[i].Life = 1.0f;
                    return;
                }
            }
        }

        public void Draw(float[] mvpMatrix)
        {
            if (_activeCount == 0) return;

            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            GLES30.GlDepthMask(false); // Don't write depth

            GLES30.GlUseProgram(_program);
            
            int uMVP = GLES30.GlGetUniformLocation(_program, "uMVPMatrix");
            int uSize = GLES30.GlGetUniformLocation(_program, "uPointSize");

            GLES30.GlUniformMatrix4fv(uMVP, 1, false, mvpMatrix, 0);
            GLES30.GlUniform1f(uSize, 50.0f); // Large particles

            GLES30.GlBindVertexArray(_vao);
            GLES30.GlDrawArrays(GLES30.GlPoints, 0, _activeCount);
            GLES30.GlBindVertexArray(0);

            GLES30.GlDepthMask(true);
            GLES30.GlDisable(GLES30.GlBlend);
        }
    }
}
