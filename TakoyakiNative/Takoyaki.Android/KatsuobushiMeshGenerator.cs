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
                float px = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.6f;
                float py = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.6f;
                float z2 = 1.05f*1.05f - px*px - py*py;
                if (z2 < 0) continue;
                float pz = (float)Math.Sqrt(z2);
                
                var (v, ind) = GenerateCurledFlake(0.15f + (float)rand.NextDouble() * 0.1f, (float)rand.NextDouble() * 2.0f);
                var mesh = new ToppingMesh
                {
                    Vertices = v,
                    Indices = ind,
                    Position = new System.Numerics.Vector3(px, py, pz),
                    Scale = new System.Numerics.Vector3(1, 1, 1),
                    Color = new System.Numerics.Vector4(0.85f, 0.75f, 0.6f, 1.0f),
                    Visible = false,
                    RotationMatrix = ToppingUtils.CalculateRotationToNormal(new System.Numerics.Vector3(px, py, pz))
                };
                ToppingUtils.UploadToGPU(mesh);
                meshes.Add(mesh);
            }
            return meshes;
        }

         private static (float[], short[]) GenerateCurledFlake(float size, float randomness)
         {
             var verts = new List<float>();
             var inds = new List<short>();
             int segs = 6;
             float w = size * 0.6f;
             
             for(int i=0; i<=segs; i++)
             {
                 float t = (float)i/segs;
                 float y = (t - 0.5f) * size * 2.0f;
                 
                 float x1 = -w;
                 float x2 = w;
                 
                 float curl = (float)Math.Sin(t * Math.PI + randomness) * size * 0.5f;
                 float twist = (float)Math.Cos(t * 2.0 + randomness) * 0.4f;
                 
                 float c = (float)Math.Cos(twist);
                 float s = (float)Math.Sin(twist);
                 
                 float px1 = x1 * c; 
                 float pz1 = x1 * s + curl;
                 float px2 = x2 * c;
                 float pz2 = x2 * s + curl;
                 
                 verts.Add(px1); verts.Add(y); verts.Add(pz1);
                 verts.Add(0); verts.Add(0); verts.Add(1);
                 verts.Add(0); verts.Add(t);
                 
                 verts.Add(px2); verts.Add(y); verts.Add(pz2);
                 verts.Add(0); verts.Add(0); verts.Add(1);
                 verts.Add(1); verts.Add(t);
             }
             
             for(int i=0; i<segs; i++)
             {
                 short baseIdx = (short)(i*2);
                 inds.Add(baseIdx); inds.Add((short)(baseIdx+2)); inds.Add((short)(baseIdx+1));
                 inds.Add((short)(baseIdx+1)); inds.Add((short)(baseIdx+2)); inds.Add((short)(baseIdx+3));
             }
             return (verts.ToArray(), inds.ToArray());
         }
    }
}
