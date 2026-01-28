using System;
using System.Collections.Generic;

namespace Takoyaki.Android
{
    public static class KatsuobushiMeshGenerator
    {
        public static List<ToppingMesh> Create(int count)
        {
            var meshes = new List<ToppingMesh>();
            var rand = new Random(1234);
            for(int i=0; i<count; i++)
            {
                float px = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.65f;
                float py = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.65f;
                float z2 = 1.05f*1.05f - px*px - py*py;
                if (z2 < 0) continue;
                float pz = (float)Math.Sqrt(z2);
                
                // Randomize size and aspect ratio for each flake
                float baseSize = 0.18f + (float)rand.NextDouble() * 0.12f;
                float aspect = 0.8f + (float)rand.NextDouble() * 0.6f;
                
                var (v, ind) = GenerateHighDetailFlake(baseSize, aspect, (float)rand.NextDouble() * 10.0f);
                var mesh = new ToppingMesh
                {
                    Vertices = v,
                    Indices = ind,
                    Position = new System.Numerics.Vector3(px, py, pz),
                    Scale = new System.Numerics.Vector3(1, 1, 1),
                    // Pale tan color with translucency
                    Color = new System.Numerics.Vector4(0.92f, 0.78f, 0.65f, 0.75f),
                    Visible = false,
                    RotationMatrix = ToppingUtils.CalculateRotationToNormal(new System.Numerics.Vector3(px, py, pz))
                };
                ToppingUtils.UploadToGPU(mesh);
                meshes.Add(mesh);
            }
            return meshes;
        }

        private static (float[], short[]) GenerateHighDetailFlake(float size, float aspect, float seed)
        {
            var verts = new List<float>();
            var inds = new List<short>();
            int segsH = 10; // More segments for complex curl
            int segsW = 3;  
            float w = size * aspect;
            float h = size;

            var rand = new Random((int)(seed * 1000));

            for(int i=0; i<=segsH; i++)
            {
                float th = (float)i/segsH;
                float py = (th - 0.5f) * h * 2.0f;
                
                // Spiral/Curling logic
                float curlFreq = 1.2f + (float)rand.NextDouble() * 0.5f;
                float curlAmp = size * (0.4f + (float)rand.NextDouble() * 0.3f);
                float curl = (float)Math.Sin(th * Math.PI * curlFreq + seed) * curlAmp;
                
                for(int j=0; j<=segsW; j++)
                {
                    float tw = (float)j/segsW;
                    float px = (tw - 0.5f) * w * 2.0f;
                    
                    // Edge jitter for "torn" look
                    float jitter = 0;
                    if (i == 0 || i == segsH || j == 0 || j == segsW)
                    {
                        jitter = (float)(rand.NextDouble() - 0.5) * size * 0.15f;
                    }

                    float x = px + jitter;
                    float y = py + jitter;
                    float z = curl + (float)Math.Cos(tw * Math.PI + seed) * size * 0.2f;

                    verts.Add(x); verts.Add(y); verts.Add(z);
                    // Normal (Approximate up)
                    verts.Add(0); verts.Add(0); verts.Add(1);
                    // UV
                    verts.Add(tw); verts.Add(th);
                }
            }
            
            for(int i=0; i<segsH; i++)
            {
                for(int j=0; j<segsW; j++)
                {
                    short b = (short)(i * (segsW + 1) + j);
                    short p1 = b;
                    short p2 = (short)(b + 1);
                    short p3 = (short)(b + segsW + 1);
                    short p4 = (short)(b + segsW + 2);
                    
                    inds.Add(p1); inds.Add(p3); inds.Add(p2);
                    inds.Add(p2); inds.Add(p3); inds.Add(p4);
                }
            }
            return (verts.ToArray(), inds.ToArray());
        }
    }
}
