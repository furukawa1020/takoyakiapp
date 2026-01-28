using System;
using System.Collections.Generic;
using Android.Opengl;
using Java.Nio;

namespace Takoyaki.Android
{
    public class TakoyakiToppings
    {
        private ToppingMesh _sauceMesh;
        private ToppingMesh _mayoMesh;
        private ToppingMesh _takoMesh; // Added missing field
        private List<ToppingMesh> _aonoriMeshes = new List<ToppingMesh>();
        private List<ToppingMesh> _katsuobushiMeshes = new List<ToppingMesh>();
        
        private int _toppingProgram;
        
        public void Initialize(int program)
        {
            _toppingProgram = program;
        }
        
        public void GenerateToppings()
        {
            // Clear existing
            _aonoriMeshes.Clear();
            _katsuobushiMeshes.Clear();
            
            // Sauce (Blob)
            _takoMesh = CreateTakoMesh();
            _sauceMesh = CreateSauceMesh();
            _mayoMesh = CreateMayoMesh();
            
            // Aonori (Flakes)
            GenerateAonoriMeshes(20); // Increase count
            
            // Katsuobushi (Flakes)
            GenerateKatsuobushiMeshes(12);
        }
        
        public void SetToppingVisible(int stage)
        {
            if (stage >= 0 && _sauceMesh != null) _sauceMesh.Visible = true;
            if (stage >= 1 && _mayoMesh != null) _mayoMesh.Visible = true;
            if (stage >= 2)
            {
                foreach(var m in _aonoriMeshes) m.Visible = true;
            }
            if (stage >= 3)
            {
                foreach(var m in _katsuobushiMeshes) m.Visible = true;
            }
        }
        
        public void Reset()
        {
            _sauceMesh.Visible = false;
            _mayoMesh.Visible = false;
            foreach(var m in _aonoriMeshes) m.Visible = false;
            foreach(var m in _katsuobushiMeshes) m.Visible = false;
        }

        public void RenderRecursive(float[] vpMatrix, float[] parentModel, float time)
        {
            GLES30.GlUseProgram(_toppingProgram);
            
            // Shared Uniforms
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_toppingProgram, "uVPMatrix"), 1, false, vpMatrix, 0);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_toppingProgram, "uTime"), time);
            
            // Toppings are double-sided and alpha blended
            GLES30.GlDisable(GLES30.GlCullFace);
            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            
            // Render in order
            if (_sauceMesh != null && _sauceMesh.Visible) RenderMeshEx(_sauceMesh, parentModel, 0.2f); // Glossy
            if (_mayoMesh != null && _mayoMesh.Visible) RenderMeshEx(_mayoMesh, parentModel, 0.4f);
            
            foreach(var m in _aonoriMeshes) if(m.Visible) RenderMeshEx(m, parentModel, 0.9f); // Matte
            foreach(var m in _katsuobushiMeshes) if(m.Visible) RenderMeshEx(m, parentModel, 0.8f);

            GLES30.GlEnable(GLES30.GlCullFace);
        }

        private void RenderMeshEx(ToppingMesh mesh, float[] parentModel, float roughness)
        {
            float[] localModel = new float[16];
            Matrix.SetIdentityM(localModel, 0);
            
            // Position
            Matrix.TranslateM(localModel, 0, mesh.Position.X, mesh.Position.Y, mesh.Position.Z);
            
            // Rotation
            if (mesh.RotationMatrix != null) {
                float[] temp = new float[16];
                Matrix.MultiplyMM(temp, 0, localModel, 0, mesh.RotationMatrix, 0);
                Array.Copy(temp, localModel, 16);
            }
            
            // Scale
            Matrix.ScaleM(localModel, 0, mesh.Scale.X, mesh.Scale.Y, mesh.Scale.Z);
            
            // Global Model = Parent (Ball) * Local (Topping)
            float[] finalModel = new float[16];
            Matrix.MultiplyMM(finalModel, 0, parentModel, 0, localModel, 0);
            
            // Update Uniforms
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_toppingProgram, "uModelMatrix"), 1, false, finalModel, 0);
            
            // Set Color (uColor is used in topping.frag instead of uToppingColor)
            int uCol = GLES30.GlGetUniformLocation(_toppingProgram, "uColor");
            if (uCol == -1) uCol = GLES30.GlGetUniformLocation(_toppingProgram, "uToppingColor"); // Fallback for old shaders
            GLES30.GlUniform4f(uCol, mesh.Color.X, mesh.Color.Y, mesh.Color.Z, mesh.Color.W);
            
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_toppingProgram, "uRoughness"), roughness);

            // Bind Buffers
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, mesh.VBO);
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, mesh.IBO);
            
            int stride = (3+3+2)*4; // Pos(3), Norm(3), UV(2)
            GLES30.GlEnableVertexAttribArray(0); GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1); GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, stride, 12);
            GLES30.GlEnableVertexAttribArray(2); GLES30.GlVertexAttribPointer(2, 2, GLES30.GlFloat, false, stride, 24);
            
            GLES30.GlDrawElements(GLES30.GlTriangles, mesh.IndexCount, GLES30.GlUnsignedShort, 0);
            
            GLES30.GlDisableVertexAttribArray(0);
            GLES30.GlDisableVertexAttribArray(1);
            GLES30.GlDisableVertexAttribArray(2);
        }
        
        private ToppingMesh CreateSauceMesh()
        {
            // Sauce: A spherical cap that hugs the surface centered at Z+ (0,0,1)
            var (vertices, indices) = GenerateSauceCap();
            
            var mesh = new ToppingMesh
            {
                Vertices = vertices,
                Indices = indices,
                Position = new System.Numerics.Vector3(0, 0, 0), 
                Scale = new System.Numerics.Vector3(1f, 1f, 1f),
                Color = new System.Numerics.Vector4(0.3f, 0.15f, 0.05f, 0.90f), // Slightly more transparent
                Visible = false
            };
            
            float[] rot = new float[16];
            Matrix.SetIdentityM(rot, 0);
            Matrix.RotateM(rot, 0, -20f, 1, 0, 0); // Less tilt, more on top
            mesh.RotationMatrix = rot;

            UploadMeshToGPU(mesh);
            return mesh;
        }

        // ...

        private (float[], short[]) GenerateSauceCap()
        {
            var verts = new List<float>();
            var inds = new List<short>();
            
            int slices = 32; 
            int rings = 12;  
            float baseAngle = 0.85f; // SPREAD IT! (Was 0.55/0.6)
            
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
                    
                    // More organic and dripping
                    float noise = (float)(Math.Sin(theta * 3) * 0.4 + Math.Cos(theta * 7 + t*5) * 0.3);
                    
                    // Non-linear spread to look like gravity pull
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
            
            // Indices
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

        private ToppingMesh CreateMayoMesh()
        {
            // Mayo: Bigger spread
            var (vertices, indices) = GenerateSurfaceTube(); // Logic updated inside GenerateSurfaceTube below
            
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
            
            UploadMeshToGPU(mesh);
            return mesh;
        }

        // Updated loop in GenerateSurfaceTube to look more spread out
         private (float[], short[]) GenerateSurfaceTube()
        {
            var verts = new List<float>();
            var inds = new List<short>();
            
            int segments = 40; // Smoother
            float tubeRadius = 0.05f; // Thicker
            float patternSize = 0.9f; // SPREAD!
            
            for(int i=0; i<=segments; i++) {
                float t = (float)i/segments; 
                
                // More complex swirl
                float x = (t - 0.5f) * patternSize * 1.8f; 
                float y = (float)Math.Sin(t * Math.PI * 5) * 0.25f; // Wider waves
                
                float z2 = 1.03f*1.03f - x*x - y*y;
                float z = (float)Math.Sqrt(Math.Max(0, z2));
                
                // ... (Tangent calc simplified or same) ...
                // Re-using simplified tangent for robustness
                float tx = 1; 
                float ty = (float)(Math.Cos(t * Math.PI * 5) * 0.25 * Math.PI * 5); 
                float tz = 0; 
                var T = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(tx, ty, tz));
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
        
        private void GenerateAonoriMeshes(int count)
        {
             // ... (Keep existing simple quad for Aonori is fine) ...
             // Re-implementing just in case override
            var rnd = new Random();
            for(int i=0; i<count; i++)
            {
                var pos = new System.Numerics.Vector3((float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5));
                pos = System.Numerics.Vector3.Normalize(pos);
                var up = new System.Numerics.Vector3(0, 1, 0);
                var front = new System.Numerics.Vector3(0, 0, 1);
                if(System.Numerics.Vector3.Dot(pos, up) < 0 && System.Numerics.Vector3.Dot(pos, front) < 0) continue;

                pos *= 1.02f; 

                var (verts, inds) = GenerateDiamondQuad(0.05f);
                var mesh = new ToppingMesh
                {
                    Vertices = verts,
                    Indices = inds,
                    Position = pos,
                    Color = new System.Numerics.Vector4(0.1f, 0.5f, 0.1f, 1.0f),
                    Visible = false
                };
                
                mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position);
                float rndAngle = (float)rnd.NextDouble() * 360f;
                Matrix.RotateM(mesh.RotationMatrix, 0, rndAngle, 0, 0, 1);
                
                UploadMeshToGPU(mesh);
                _aonoriMeshes.Add(mesh);
            }
        }

        private void GenerateKatsuobushiMeshes(int count)
        {
             var rnd = new Random();
            for(int i=0; i<count; i++)
            {
                var pos = new System.Numerics.Vector3((float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5));
                pos = System.Numerics.Vector3.Normalize(pos);
                 var up = new System.Numerics.Vector3(0, 1, 0);
                 // Only on top hemisphere
                if(System.Numerics.Vector3.Dot(pos, up) < -0.1f) continue;

                pos *= 1.04f; // Stick out more
                
                // HUGE, TWISTED Katsuobushi
                // Size 0.25f (was 0.08)
                var (verts, inds) = GenerateTwistedStrip(0.25f, 0.12f, rnd);
                
                 var mesh = new ToppingMesh
                {
                    Vertices = verts,
                    Indices = inds,
                    Position = pos,
                    Color = new System.Numerics.Vector4(0.75f, 0.55f, 0.40f, 0.9f), // Lighter, slightly transp
                    Visible = false
                };
                
                 mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position);
                 float rndAngle = (float)rnd.NextDouble() * 360f;
                 Matrix.RotateM(mesh.RotationMatrix, 0, rndAngle, 0, 0, 1);
                 
                 // Random tilt
                 Matrix.RotateM(mesh.RotationMatrix, 0, (float)rnd.NextDouble() * 30f - 15f, 1, 0, 0);

                UploadMeshToGPU(mesh);
                _katsuobushiMeshes.Add(mesh);
            }
        }
        
        // New Helper for Realistic Katsuobushi
        private (float[], short[]) GenerateTwistedStrip(float length, float width, Random rnd)
        {
             var verts = new List<float>();
             var inds = new List<short>();
             
             int segs = 4;
             for(int i=0; i<=segs; i++)
             {
                 float t = (float)i/segs;
                 float y = (t - 0.5f) * length;
                 
                 // Twist and curl
                 float curl = (float)Math.Sin(t * Math.PI) * 0.05f; 
                 float twist = (float)(t * Math.PI * (rnd.NextDouble() + 0.5)); 
                 
                 float x1 = -width/2; 
                 float x2 = width/2;
                 
                 // Apply twist
                 float c = (float)Math.Cos(twist);
                 float s = (float)Math.Sin(twist);
                 
                 // Point A
                 float px1 = x1 * c; 
                 float pz1 = x1 * s + curl;
                 
                 // Point B
                 float px2 = x2 * c;
                 float pz2 = x2 * s + curl;
                 
                 // Add Verts (Pos, Norm(rough), UV)
                 // Normal is rough Z+ rotated
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
                 inds.Add(baseIdx);
                 inds.Add((short)(baseIdx+2));
                 inds.Add((short)(baseIdx+1));
                 
                 inds.Add((short)(baseIdx+1));
                 inds.Add((short)(baseIdx+2));
                 inds.Add((short)(baseIdx+3));
             }
             
             return (verts.ToArray(), inds.ToArray());
        }
        
        // Helpers
        
        private float[] CalculateRotationToNormal(System.Numerics.Vector3 position)
        {
            var normal = System.Numerics.Vector3.Normalize(position);
            // Default mesh is flat on XY, Normal is Z+ (0,0,1)
            var defaultUp = new System.Numerics.Vector3(0, 0, 1);
            
            var axis = System.Numerics.Vector3.Cross(defaultUp, normal);
            float angle = (float)Math.Acos(System.Numerics.Vector3.Dot(defaultUp, normal));
            
            float[] rotMat = new float[16];
            Matrix.SetIdentityM(rotMat, 0);
            
            if (axis.LengthSquared() > 0.0001f)
            {
                axis = System.Numerics.Vector3.Normalize(axis);
                // Convert Axis-Angle to Rotation Matrix
                Matrix.RotateM(rotMat, 0, angle * 180f / (float)Math.PI, axis.X, axis.Y, axis.Z);
            }
            else if (System.Numerics.Vector3.Dot(defaultUp, normal) < -0.99f)
            {
                // 180 deg
                 Matrix.RotateM(rotMat, 0, 180f, 1, 0, 0);
            }
            // else 0 deg (already Identity)
            
            return rotMat;
        }

        private ToppingMesh CreateTakoMesh()
        {
            // Placeholder for Tako mesh generation
            // For now, return a simple mesh or null
            return null;
        }

        // --- Geometry Generators ---



        private (float[], short[]) GenerateDiamondQuad(float size)
        {
             float w = size * 0.7f;
             float h = size * 1.0f;
             // Flat on XY plane, Normal Z
             float[] v = {
                 0, -h, 0,  0,0,1, 0.5f, 0,
                 w, 0, 0,   0,0,1, 1, 0.5f,
                 0, h, 0,   0,0,1, 0.5f, 1,
                 -w, 0, 0,  0,0,1, 0, 0.5f
             };
             short[] i = {0,1,2, 0,2,3};
             return (v, i);
        }

        private void UploadMeshToGPU(ToppingMesh mesh)
        {
            int[] buffers = new int[2];
            GLES30.GlGenBuffers(2, buffers, 0);
            mesh.VBO = buffers[0];
            mesh.IBO = buffers[1];
            
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, mesh.VBO);
            GLES30.GlBufferData(GLES30.GlArrayBuffer, mesh.Vertices.Length * 4, Java.Nio.FloatBuffer.Wrap(mesh.Vertices), GLES30.GlStaticDraw);
            
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, mesh.IBO);
            GLES30.GlBufferData(GLES30.GlElementArrayBuffer, mesh.Indices.Length * 2, Java.Nio.ShortBuffer.Wrap(mesh.Indices), GLES30.GlStaticDraw);
             
            mesh.IndexCount = mesh.Indices.Length;
             
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, 0);
        }
    }
    
    internal class ToppingMesh
    {
        public float[] Vertices;
        public short[] Indices;
        public int VBO, IBO, IndexCount;
        public System.Numerics.Vector3 Position;
        public System.Numerics.Vector3 Scale = new System.Numerics.Vector3(1,1,1);
        public System.Numerics.Vector4 Color;
        public bool Visible;
        public float[] RotationMatrix; // 4x4 matrix
    }
}
