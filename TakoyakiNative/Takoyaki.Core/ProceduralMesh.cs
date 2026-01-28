using System;
using System.Collections.Generic;
using System.Numerics;

namespace Takoyaki.Core
{
    public class ProceduralMesh
    {
        public float[] Vertices = Array.Empty<float>();
        public float[] Normals = Array.Empty<float>();
        public float[] UVs = Array.Empty<float>();
        public short[] Indices = Array.Empty<short>();
        
        // Single array for Interleaved Buffer (Pos, Norm, UV)
        // Format: [Px, Py, Pz, Nx, Ny, Nz, U, V]
        public float[] ToInterleavedArray()
        {
            int vCount = Vertices.Length / 3;
            float[] data = new float[vCount * 8];
            for(int i=0; i<vCount; i++)
            {
                // Pos
                data[i*8+0] = Vertices[i*3+0];
                data[i*8+1] = Vertices[i*3+1];
                data[i*8+2] = Vertices[i*3+2];
                // Norm
                data[i*8+3] = Normals[i*3+0];
                data[i*8+4] = Normals[i*3+1];
                data[i*8+5] = Normals[i*3+2];
                // UV
                data[i*8+6] = UVs[i*2+0];
                data[i*8+7] = UVs[i*2+1];
            }
            return data;
        }

        public static ProceduralMesh GenerateSphere(int resolution)
        {
            // UV Sphere Logic adapted from the Unity script
            List<float> verts = new List<float>();
            List<float> norms = new List<float>();
            List<float> uvs = new List<float>();
            List<short> tris = new List<short>();

            // Resolution roughly equates to segments
            int latSegments = resolution / 2;
            int lonSegments = resolution;

            for (int y = 0; y <= latSegments; y++)
            {
                float v = (float)y / latSegments;
                float lat = (v - 0.5f) * MathF.PI; // -90 to 90
                float cosLat = MathF.Cos(lat);
                float sinLat = MathF.Sin(lat);

                for (int x = 0; x <= lonSegments; x++)
                {
                    float u = (float)x / lonSegments;
                    float lon = u * 2f * MathF.PI;
                    
                    float xPos = MathF.Cos(lon) * cosLat;
                    float zPos = MathF.Sin(lon) * cosLat;
                    float yPos = sinLat;

                    // Radius = 1.0 (Unit Sphere)
                    verts.Add(xPos); verts.Add(yPos); verts.Add(zPos);
                    norms.Add(xPos); norms.Add(yPos); norms.Add(zPos); // Unit sphere normal = position
                    uvs.Add(u); uvs.Add(v);
                }
            }

            int stride = lonSegments + 1;
            for (int y = 0; y < latSegments; y++)
            {
                for (int x = 0; x < lonSegments; x++)
                {
                    short v0 = (short)(y * stride + x);
                    short v1 = (short)(v0 + 1);
                    short v2 = (short)((y + 1) * stride + x);
                    short v3 = (short)(v2 + 1);

                    // Tri 1
                    tris.Add(v0);
                    tris.Add(v2);
                    tris.Add(v1);

                    // Tri 2
                    tris.Add(v1);
                    tris.Add(v2);
                    tris.Add(v3);
                }
            }

            return new ProceduralMesh
            {
                Vertices = verts.ToArray(),
                Normals = norms.ToArray(),
                UVs = uvs.ToArray(),
                Indices = tris.ToArray()
            };
        }
    }
}
