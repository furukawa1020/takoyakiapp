using System;
using System.Collections.Generic;
using Android.Opengl;

namespace Takoyaki.Android
{
    public static class SauceMeshGenerator
    {
        public static ToppingMesh Create()
        {
            var (vertices, indices) = GenerateSauceCap();
            var mesh = new ToppingMesh
            {
                Vertices = vertices,
                Indices = indices,
                Position = new System.Numerics.Vector3(0, 0, 0),
                Scale = new System.Numerics.Vector3(1f, 1f, 1f),
                Color = new System.Numerics.Vector4(0.3f, 0.15f, 0.05f, 0.90f),
                Visible = false
            };
            
            float[] rot = new float[16];
            Matrix.SetIdentityM(rot, 0);
            Matrix.RotateM(rot, 0, -20f, 1, 0, 0); 
            mesh.RotationMatrix = rot;

            ToppingUtils.UploadToGPU(mesh);
            return mesh;
        }

        private static (float[], short[]) GenerateSauceCap()
        {
            var verts = new List<float>();
            var inds = new List<short>();
            
            int slices = 32; 
            int rings = 12;  
            float baseAngle = 0.85f; 
            
            // Center
            verts.Add(0); verts.Add(0); verts.Add(1.01f);
            verts.Add(0); verts.Add(0); verts.Add(1);     
            verts.Add(0.5f); verts.Add(0.5f);             
            
            for(int r = 1; r <= rings; r++)
            {
                float t = (float)r / rings;
                for(int s = 0; s < slices; s++)
                {
                    float theta = (float)s / slices * (float)Math.PI * 2;
                    float noise = (float)(Math.Sin(theta * 3) * 0.4 + Math.Cos(theta * 7 + t*5) * 0.3);
                    float angleFunc = baseAngle + (noise * 0.25f * (t * t * t)); 
                    
                    float phi = t * angleFunc;
                    float rad = 1.01f;
                    
                    float x = rad * (float)(Math.Sin(phi) * Math.Cos(theta));
                    float y = rad * (float)(Math.Sin(phi) * Math.Sin(theta));
                    float z = rad * (float)Math.Cos(phi);
                    
                    verts.Add(x); verts.Add(y); verts.Add(z);
                    verts.Add(x); verts.Add(y); verts.Add(z); 
                    verts.Add(0.5f + x*0.5f); verts.Add(0.5f + y*0.5f); 
                }
            }
            
            for(int s=0; s<slices; s++) {
                inds.Add(0);
                inds.Add((short)(s+1));
                inds.Add((short)((s+1)%slices + 1));
            }
            
            for(int r=0; r<rings-1; r++) {
                int ringStart = 1 + r*slices;
                int nextRingStart = ringStart + slices;
                for(int s=0; s<slices; s++) {
                    short p1 = (short)(ringStart + s);
                    short p2 = (short)(ringStart + (s+1)%slices);
                    short p3 = (short)(nextRingStart + s);
                    short p4 = (short)(nextRingStart + (s+1)%slices);
                    
                    inds.Add(p1); inds.Add(p3); inds.Add(p2);
                    inds.Add(p2); inds.Add(p3); inds.Add(p4);
                }
            }
            return (verts.ToArray(), inds.ToArray());
        }
    }
}
