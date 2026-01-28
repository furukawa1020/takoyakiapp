using System;
using System.Collections.Generic;
using Android.Opengl;

namespace Takoyaki.Android
{
    public static class MayoMeshGenerator
    {
        public static ToppingMesh Create()
        {
            var (vertices, indices) = GenerateSurfaceTube();
            var mesh = new ToppingMesh
            {
                Vertices = vertices,
                Indices = indices,
                Position = new System.Numerics.Vector3(0, 0, 0), 
                Scale = new System.Numerics.Vector3(1f, 1f, 1f),
                Color = new System.Numerics.Vector4(1.0f, 0.98f, 0.85f, 1.0f),
                Visible = false
            };
            
            float[] rot = new float[16];
            Matrix.SetIdentityM(rot, 0);
            Matrix.RotateM(rot, 0, -30f, 1, 0, 0); 
            mesh.RotationMatrix = rot;
            
            ToppingUtils.UploadToGPU(mesh);
            return mesh;
        }

        private static (float[], short[]) GenerateSurfaceTube()
        {
            var verts = new List<float>();
            var inds = new List<short>();
            
            int segments = 40; 
            float tubeRadius = 0.05f; 
            float patternSize = 0.9f; 
            
            for(int i=0; i<=segments; i++) {
                float t = (float)i/segments; 
                float x = (t - 0.5f) * patternSize * 1.8f; 
                float y = (float)Math.Sin(t * Math.PI * 5) * 0.25f; 
                
                float z2 = 1.03f*1.03f - x*x - y*y;
                float z = (float)Math.Sqrt(Math.Max(0, z2));
                
                float tx = 1; 
                float ty = (float)(Math.Cos(t * Math.PI * 5) * 0.25 * Math.PI * 5); 
                var T = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(tx, ty, 0));
                var Pos = new System.Numerics.Vector3(x, y, z);
                var N = System.Numerics.Vector3.Normalize(Pos);
                var B = System.Numerics.Vector3.Cross(T, N); 
                
                for(int j=0; j<8; j++) {
                    float ang = j * 2 * (float)Math.PI / 8;
                    float cr = (float)Math.Cos(ang) * tubeRadius;
                    float sr = (float)Math.Sin(ang) * tubeRadius;
                    var offset = N * cr + B * sr;
                    var p = Pos + offset;
                    verts.Add(p.X); verts.Add(p.Y); verts.Add(p.Z);
                    verts.Add(offset.X); verts.Add(offset.Y); verts.Add(offset.Z); 
                    verts.Add(t); verts.Add((float)j/8);
                }
            }
            
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    short current = (short)(i * 8 + j);
                    short next = (short)(i * 8 + (j + 1) % 8);
                    short currentNext = (short)((i + 1) * 8 + j);
                    short nextNext = (short)((i + 1) * 8 + (j + 1) % 8);
                    inds.Add(current); inds.Add(currentNext); inds.Add(next);
                    inds.Add(next); inds.Add(currentNext); inds.Add(nextNext);
                }
            }
            return (verts.ToArray(), inds.ToArray());
        }
    }
}
