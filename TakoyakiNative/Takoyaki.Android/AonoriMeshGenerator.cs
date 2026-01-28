using System;
using System.Collections.Generic;

namespace Takoyaki.Android
{
    public static class AonoriMeshGenerator
    {
        public static List<ToppingMesh> Create(int count)
        {
            var meshes = new List<ToppingMesh>();
            var rand = new Random(42);
            for(int i=0; i<count; i++)
            {
                float px = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.7f;
                float py = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.7f;
                float z2 = 1.03f*1.03f - px*px - py*py;
                if (z2 < 0) continue;
                float pz = (float)Math.Sqrt(z2);
                
                var (v, ind) = GenerateDiamondQuad(0.04f + (float)rand.NextDouble() * 0.04f);
                var mesh = new ToppingMesh
                {
                    Vertices = v,
                    Indices = ind,
                    Position = new System.Numerics.Vector3(px, py, pz),
                    Scale = new System.Numerics.Vector3(1, 1, 1),
                    Color = new System.Numerics.Vector4(0.1f, 0.35f, 0.1f, 1.0f),
                    Visible = false,
                    RotationMatrix = ToppingUtils.CalculateRotationToNormal(new System.Numerics.Vector3(px, py, pz))
                };
                ToppingUtils.UploadToGPU(mesh);
                meshes.Add(mesh);
            }
            return meshes;
        }

        private static (float[], short[]) GenerateDiamondQuad(float size)
        {
             float w = size * 0.7f;
             float h = size * 1.0f;
             float[] v = {
                 0, -h, 0,  0,0,1, 0.5f, 0,
                 w, 0, 0,   0,0,1, 1, 0.5f,
                 0, h, 0,   0,0,1, 0.5f, 1,
                 -w, 0, 0,  0,0,1, 0, 0.5f
             };
             short[] i = {0,1,2, 0,2,3};
             return (v, i);
        }
    }
}
