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
                float px = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.68f;
                float py = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.68f;
                float z2 = 1.06f*1.06f - px*px - py*py;
                if (z2 < 0) continue;
                float pz = (float)Math.Sqrt(z2);
                
                float baseSize = 0.22f + (float)rand.NextDouble() * 0.15f; 
                float aspect = 1.0f + (float)rand.NextDouble() * 0.8f; 
                
                var (v, ind) = GenerateHighDetailFlake(baseSize, aspect, (float)rand.NextDouble() * 100.0f);
                var mesh = new ToppingMesh
                {
                    Vertices = v,
                    Indices = ind,
                    Position = new System.Numerics.Vector3(px, py, pz),
                    Scale = new System.Numerics.Vector3(1, 1, 1),
                    // More natural tan, slightly higher alpha as shader handles clipping
                    Color = new System.Numerics.Vector4(0.90f, 0.76f, 0.62f, 0.85f), 
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
            int segsH = 12; // More segments for smooth curves
            int segsW = 4;  
            float w = size * aspect;
            float h = size;

            var rand = new Random((int)(seed * 1000));

            for(int i=0; i<=segsH; i++)
            {
                float th = (float)i/segsH;
                float py = (th - 0.5f) * h * 2.0f;
                
                // Base curl (curled by nature, not just animation)
                float curl = (float)Math.Sin(th * Math.PI * 1.5 + seed) * size * 0.6f;
                
                for(int j=0; j<=segsW; j++)
                {
                    float tw = (float)j/segsW;
                    float px = (tw - 0.5f) * w * 2.0f;
                    
                    float x = px;
                    float y = py;
                    float z = curl + (float)Math.Cos(tw * Math.PI + seed) * size * 0.15f;

                    verts.Add(x); verts.Add(y); verts.Add(z);
                    // Approximate normal for lighting
                    var n = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(0, 0, 1));
                    verts.Add(n.X); verts.Add(n.Y); verts.Add(n.Z);
                    // Standard UVs for fragment shader noise clipping
                    verts.Add(tw); verts.Add(th);
                }
            }
            // ... (index generation stays same)
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
