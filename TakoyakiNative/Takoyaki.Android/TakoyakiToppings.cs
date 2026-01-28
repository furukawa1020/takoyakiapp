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
        
        private int _program;
        
        public void Initialize(int program)
        {
            _program = program;
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
        
        public void Render(float[] viewMatrix, float[] projectionMatrix)
        {
            GLES30.GlUseProgram(_program);
            
            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            GLES30.GlDisable(0x0B44); // GL_CULL_FACE Double-Sided
            
            if (_sauceMesh != null && _sauceMesh.Visible) RenderMesh(_sauceMesh, viewMatrix, projectionMatrix);
            if (_mayoMesh != null && _mayoMesh.Visible) RenderMesh(_mayoMesh, viewMatrix, projectionMatrix);
            
            foreach(var m in _aonoriMeshes)
            {
                if(m.Visible) RenderMesh(m, viewMatrix, projectionMatrix);
            }
            
            foreach(var m in _katsuobushiMeshes)
            {
                if(m.Visible) RenderMesh(m, viewMatrix, projectionMatrix);
            }
            
            GLES30.GlEnable(0x0B44); // GL_CULL_FACE
            GLES30.GlDisable(GLES30.GlBlend);
        }
        
        private void RenderMesh(ToppingMesh mesh, float[] viewMatrix, float[] projectionMatrix)
        {
            float[] modelMatrix = new float[16];
            
            // Apply Transform: Translate -> Rotate -> Scale
            Matrix.SetIdentityM(modelMatrix, 0);
            
            Matrix.TranslateM(modelMatrix, 0, mesh.Position.X, mesh.Position.Y, mesh.Position.Z);
            
            // Apply Rotation Matrix if exists
            if (mesh.RotationMatrix != null)
            {
                float[] temp = new float[16];
                Matrix.MultiplyMM(temp, 0, modelMatrix, 0, mesh.RotationMatrix, 0);
                Array.Copy(temp, modelMatrix, 16);
            }
            
            Matrix.ScaleM(modelMatrix, 0, mesh.Scale.X, mesh.Scale.Y, mesh.Scale.Z);
            
            // MVP
            float[] mvpMatrix = new float[16];
            float[] tempM = new float[16];
            Matrix.MultiplyMM(tempM, 0, viewMatrix, 0, modelMatrix, 0);
            Matrix.MultiplyMM(mvpMatrix, 0, projectionMatrix, 0, tempM, 0);
            
            // Uniforms
            int uMVP = GLES30.GlGetUniformLocation(_program, "uMVPMatrix");
            GLES30.GlUniformMatrix4fv(uMVP, 1, false, mvpMatrix, 0);
            
            int uModel = GLES30.GlGetUniformLocation(_program, "uModelMatrix");
            GLES30.GlUniformMatrix4fv(uModel, 1, false, modelMatrix, 0);
            
            int uColor = GLES30.GlGetUniformLocation(_program, "uToppingColor");
            GLES30.GlUniform4f(uColor, mesh.Color.X, mesh.Color.Y, mesh.Color.Z, mesh.Color.W);
            
            // Draw
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, mesh.VBO);
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, 32, 0); // Pos
            GLES30.GlEnableVertexAttribArray(1);
            GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, 32, 12); // Norm
             
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, mesh.IBO);
            GLES30.GlDrawElements(GLES30.GlTriangles, mesh.IndexCount, GLES30.GlUnsignedShort, 0);
            
            GLES30.GlDisableVertexAttribArray(0);
            GLES30.GlDisableVertexAttribArray(1);
            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, 0);
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, 0);
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
            
            int slices = 32; // Higher res for smoother blob
            int rings = 12;  
            float baseAngle = 0.55f; 
            
            // Center
            verts.Add(0); verts.Add(0); verts.Add(1.01f); // Tighter to surface
            verts.Add(0); verts.Add(0); verts.Add(1);     
            verts.Add(0.5f); verts.Add(0.5f);             
            
            for(int r = 1; r <= rings; r++)
            {
                float t = (float)r / rings;
                for(int s = 0; s < slices; s++)
                {
                    float theta = (float)s / slices * (float)Math.PI * 2;
                    
                    // Organic noise
                    float noise = (float)(Math.Sin(theta * 3) * 0.4 + Math.Cos(theta * 7) * 0.2);
                    
                    // Apply noise mainly to the outer rings to form drips
                    float angleFunc = baseAngle + (noise * 0.15f * (t * t)); 
                    
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
            // Mayo: Wavy lines generated on Z+ surface
            var (vertices, indices) = GenerateSurfaceTube();
            
            var mesh = new ToppingMesh
            {
                Vertices = vertices,
                Indices = indices,
                Position = new System.Numerics.Vector3(0, 0, 0), // Mesh already has surface radius
                Scale = new System.Numerics.Vector3(1f, 1f, 1f),
                Color = new System.Numerics.Vector4(1.0f, 0.98f, 0.85f, 1.0f),
                Visible = false
            };
            
            // Rotate same as sauce or slightly different
            float[] rot = new float[16];
            Matrix.SetIdentityM(rot, 0);
            Matrix.RotateM(rot, 0, -30f, 1, 0, 0); 
            mesh.RotationMatrix = rot;
            
            UploadMeshToGPU(mesh);
            return mesh;
        }
        
        private void GenerateAonoriMeshes(int count)
        {
            var rnd = new Random();
            for(int i=0; i<count; i++)
            {
                // Random point on sphere
                var pos = new System.Numerics.Vector3((float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5));
                pos = System.Numerics.Vector3.Normalize(pos);
                
                // Keep on top/front side
                var up = new System.Numerics.Vector3(0, 1, 0);
                var front = new System.Numerics.Vector3(0, 0, 1);
                if(System.Numerics.Vector3.Dot(pos, up) < 0 && System.Numerics.Vector3.Dot(pos, front) < 0) continue;

                pos *= 1.02f; // Surface radius

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
                // Add random rotation around Z (normal) to avoid uniform look
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
                if(System.Numerics.Vector3.Dot(pos, up) < -0.2f) continue;

                pos *= 1.03f;
                
                var (verts, inds) = GenerateDiamondQuad(0.08f);
                 var mesh = new ToppingMesh
                {
                    Vertices = verts,
                    Indices = inds,
                    Position = pos,
                    Color = new System.Numerics.Vector4(0.6f, 0.4f, 0.3f, 1.0f),
                    Visible = false
                };
                
                 mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position);
                 float rndAngle = (float)rnd.NextDouble() * 360f;
                 Matrix.RotateM(mesh.RotationMatrix, 0, rndAngle, 0, 0, 1);

                UploadMeshToGPU(mesh);
                _katsuobushiMeshes.Add(mesh);
            }
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

        private (float[], short[]) GenerateSauceCap()
        {
            var verts = new List<float>();
            var inds = new List<short>();
            
            int slices = 20; // angular resolution
            int rings = 6;   // radial resolution
            float maxAngle = 0.6f; // Covering ~35 degrees
            
            // Center
            verts.Add(0); verts.Add(0); verts.Add(1.02f); // Slightly above 1.0
            verts.Add(0); verts.Add(0); verts.Add(1);     // Normal
            verts.Add(0.5f); verts.Add(0.5f);             // UV
            
            for(int r = 1; r <= rings; r++)
            {
                float phi = (float)r / rings * maxAngle;
                for(int s = 0; s < slices; s++)
                {
                    float theta = (float)s / slices * (float)Math.PI * 2;
                    
                    // Add irregularity to edge rings
                    float noise = 0;
                    if (r > rings/2) 
                        noise = (float)Math.Sin(theta * 5) * 0.05f * ((float)r/rings);

                    float currentPhi = phi + noise;
                    float rad = 1.02f;
                    
                    // Generate point on sphere cap (around Z axis)
                    float x = rad * (float)(Math.Sin(currentPhi) * Math.Cos(theta));
                    float y = rad * (float)(Math.Sin(currentPhi) * Math.Sin(theta));
                    float z = rad * (float)Math.Cos(currentPhi);
                    
                    verts.Add(x); verts.Add(y); verts.Add(z);
                    verts.Add(x); verts.Add(y); verts.Add(z); // Normal
                    verts.Add(0.5f + x*0.5f); verts.Add(0.5f + y*0.5f); // UV
                }
            }
            
            // Indices
            // Center fan
            for(int s=0; s<slices; s++) {
                inds.Add(0);
                inds.Add((short)(s+1));
                inds.Add((short)((s+1)%slices + 1));
            }
            
            // Rings
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
        
         private (float[], short[]) GenerateSurfaceTube()
        {
            var verts = new List<float>();
            var inds = new List<short>();
            
            // Wavy line on surface (Z+)
            int segments = 20;
            float tubeRadius = 0.04f;
            float patternSize = 0.6f;
            
            for(int i=0; i<=segments; i++) {
                float t = (float)i/segments; // 0 to 1
                float x = (t - 0.5f) * patternSize * 1.5f;
                float y = (float)Math.Sin(t * Math.PI * 4) * 0.15f;
                // Project X,Y onto Sphere Z
                float z2 = 1.03f*1.03f - x*x - y*y;
                float z = (float)Math.Sqrt(Math.Max(0, z2));
                
                // Tangent/Normal calculation
                float tx = 1; 
                float ty = (float)(Math.Cos(t * Math.PI * 4) * 0.15 * Math.PI * 4); // Fixed cast
                float tz = 0; // rough
                var T = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(tx, ty, tz));
                var Pos = new System.Numerics.Vector3(x, y, z);
                var N = System.Numerics.Vector3.Normalize(Pos);
                var B = System.Numerics.Vector3.Cross(T, N); // Binormal
                
                // Generate Ring
                for(int j=0; j<8; j++) {
                    float ang = j * 2 * (float)Math.PI / 8;
                    float cr = (float)Math.Cos(ang) * tubeRadius;
                    float sr = (float)Math.Sin(ang) * tubeRadius;
                    
                    // Ring vertex in local frame (N, B)
                    var offset = N * cr + B * sr;
                    var p = Pos + offset;
                    
                    verts.Add(p.X); verts.Add(p.Y); verts.Add(p.Z);
                    verts.Add(offset.X); verts.Add(offset.Y); verts.Add(offset.Z); // Normal (from center of tube)
                    verts.Add(t); verts.Add((float)j/8);
                }
            }
            
             for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    short current = (short)(i * 6 + j);
                    short next = (short)(i * 6 + (j + 1) % 6);
                    short currentNext = (short)((i + 1) * 6 + j);
                    short nextNext = (short)((i + 1) * 6 + (j + 1) % 6);
                    
                    inds.Add(current); inds.Add(currentNext); inds.Add(next);
                    inds.Add(next); inds.Add(currentNext); inds.Add(nextNext);
                }
            }
            
            return (verts.ToArray(), inds.ToArray());
        }

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
