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
            // Center on top-front of the ball
            _sauceMesh = CreateSauceMesh();
            
            // Mayo (Tube)
            _mayoMesh = CreateMayoMesh();
            
            // Aonori (Flakes)
            GenerateAonoriMeshes(15);
            
            // Katsuobushi (Flakes)
            GenerateKatsuobushiMeshes(10);
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
            GLES30.GlDisable(GLES30.GlCullFace); // Double-Sided
            
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
            
            GLES30.GlEnable(GLES30.GlCullFace);
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
            // Sauce on top-front
            // Just a simple flattened sphere / blob
            var (vertices, indices) = GenerateFormattedBlob();
            
            var mesh = new ToppingMesh
            {
                Vertices = vertices,
                Indices = indices,
                Position = new System.Numerics.Vector3(0, 0.4f, 0.85f), // Front-Top
                Scale = new System.Numerics.Vector3(1.2f, 1.2f, 1.0f),  // Slightly larger
                Color = new System.Numerics.Vector4(0.3f, 0.15f, 0.05f, 0.95f),
                Visible = false
            };
            // Orient sauce to match position normal approx
            mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position);

            UploadMeshToGPU(mesh);
            return mesh;
        }

        private ToppingMesh CreateMayoMesh()
        {
            var (vertices, indices) = GenerateFormattedTube();
            
            var mesh = new ToppingMesh
            {
                Vertices = vertices,
                Indices = indices,
                Position = new System.Numerics.Vector3(0.1f, 0.2f, 0.9f), // Front
                Scale = new System.Numerics.Vector3(1f, 1f, 1f),
                Color = new System.Numerics.Vector4(1.0f, 0.98f, 0.85f, 1.0f),
                Visible = false
            };
            
            // Orient mayo 
            mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position);
            
            UploadMeshToGPU(mesh);
            return mesh;
        }
        
        private void GenerateAonoriMeshes(int count)
        {
            var rnd = new Random();
            for(int i=0; i<count; i++)
            {
                // Random spherical coord
                // Concentrate on top hemisphere (phi < PI/2)
                double theta = rnd.NextDouble() * Math.PI * 2;
                double phi = rnd.NextDouble() * (Math.PI / 1.5); // Top ~120 deg
                
                float radius = 1.02f;
                float x = radius * (float)(Math.Sin(phi) * Math.Cos(theta));
                float y = radius * (float)(Math.Sin(phi) * Math.Sin(theta)); // Y is up? No, Y is usually up in OpenGL... wait.
                // In this app:
                // Camera at (0, 4, 4), looking at (0,0,0). Up is (0,1,0)?
                // Previous code logic suggested Y is up.
                // Let's stick to standard Y-up.
                
                float z = radius * (float)Math.Cos(phi); 
                
                // Oops, standard spherical coord: Y is up -> phi from Y axis?
                // x = r sin(phi) cos(theta)
                // y = r cos(phi)   <-- Y is UP
                // z = r sin(phi) sin(theta)
                
                // Let's use simple Vector3 logic
                 y = radius * (float)Math.Cos(phi); // Y is Up
                 float r_xz = radius * (float)Math.Sin(phi);
                 x = r_xz * (float)Math.Cos(theta);
                 z = r_xz * (float)Math.Sin(theta);

                // Wait, previous code used:
                // x = r sin(phi) cos(theta)
                // y = r sin(phi) sin(theta)
                // z = r cos(phi)
                // This implies Z is UP. Or Z is Forward/Back.
                // Camera (0, 4, 4) -> Looking down and back.
                // If Z is up, then (0,4,4) is very weird. 
                // Let's assume sphere is centered at 0.
                
                // Use random point on sphere logic
                var pos = new System.Numerics.Vector3((float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5));
                pos = System.Numerics.Vector3.Normalize(pos) * 1.03f;
                
                // Only on "Front/Top" side visible to camera?
                // Camera (0, 4, 4). Normalized dir (0, 0.7, 0.7).
                // Let's filter simple dot product
                var camDir = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(0, 4, 4));
                if(System.Numerics.Vector3.Dot(System.Numerics.Vector3.Normalize(pos), camDir) < 0.2f) continue;

                var (verts, inds) = GenerateDiamondQuad(0.06f);
                var mesh = new ToppingMesh
                {
                    Vertices = verts,
                    Indices = inds,
                    Position = pos,
                    Color = new System.Numerics.Vector4(0.1f, 0.5f, 0.1f, 1.0f),
                    Visible = false
                };
                
                mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position);
                
                UploadMeshToGPU(mesh);
                _aonoriMeshes.Add(mesh);
            }
        }
        
        private void GenerateKatsuobushiMeshes(int count)
        {
            // Similar to Aonori but brown and larger
             var rnd = new Random();
            for(int i=0; i<count; i++)
            {
                var pos = new System.Numerics.Vector3((float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5), (float)(rnd.NextDouble()-0.5));
                pos = System.Numerics.Vector3.Normalize(pos) * 1.04f;
                 var camDir = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(0, 4, 4));
                if(System.Numerics.Vector3.Dot(System.Numerics.Vector3.Normalize(pos), camDir) < 0.2f) continue;
                
                var (verts, inds) = GenerateDiamondQuad(0.09f); // Larger
                 var mesh = new ToppingMesh
                {
                    Vertices = verts,
                    Indices = inds,
                    Position = pos,
                    Color = new System.Numerics.Vector4(0.6f, 0.4f, 0.3f, 1.0f),
                    Visible = false
                };
                
                 mesh.RotationMatrix = CalculateRotationToNormal(mesh.Position); // Align
                 // Add random spin around normal later if needed
                 
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

        private (float[], short[]) GenerateFormattedBlob()
        {
             // Simple Hemisphere centered at 0,0,0, pointing Z+
             // Vertices logic... simplified
             // A flat circle on Z=0, and a popped up center at Z=Height
             var verts = new List<float>();
             var inds = new List<short>();
             
             // Center top
             verts.Add(0); verts.Add(0); verts.Add(0.15f); // Pos
             verts.Add(0); verts.Add(0); verts.Add(1);    // Norm
             verts.Add(0.5f); verts.Add(0.5f);            // UV
             
             int distinct = 8;
             float radius = 0.4f;
             
             // Ring
             for(int i=0; i<distinct; i++) {
                 float ang = i * 2 * (float)Math.PI / distinct;
                 float x = (float)Math.Cos(ang) * radius;
                 float y = (float)Math.Sin(ang) * radius;
                 verts.Add(x); verts.Add(y); verts.Add(0); // slightly off surface
                 verts.Add(0); verts.Add(0); verts.Add(1); // Normal Z+
                 verts.Add(0.5f + x); verts.Add(0.5f + y);
             }
             
             // Indices (Fan)
             for(int i=0; i<distinct; i++) {
                 inds.Add(0);
                 inds.Add((short)(i+1));
                 inds.Add((short)((i+1)%distinct + 1));
             }
             
             return (verts.ToArray(), inds.ToArray());
        }
        
         private (float[], short[]) GenerateFormattedTube()
        {
            // Wavy line tube
            // Path: Along X axis, varying Y
            var verts = new List<float>();
            var inds = new List<short>();
            
            int segments = 10;
            float length = 0.8f;
            float tubeR = 0.06f;
            
            for(int i=0; i<=segments; i++) {
                float t = (float)i/segments;
                float x = (t - 0.5f) * length;
                float y = (float)Math.Sin(t * Math.PI * 3) * 0.15f; 
                float z = 0; 
                
                // Ring
                for(int j=0; j<6; j++) {
                    float ang = j * 2 * (float)Math.PI / 6;
                    float rx = 0; // tube ring plane is YZ mostly? No, path is generally X. Ring in YZ.
                    float ry = (float)Math.Cos(ang) * tubeR;
                    float rz = (float)Math.Sin(ang) * tubeR;
                    
                    verts.Add(x); verts.Add(y + ry); verts.Add(z + rz);
                    verts.Add(0); verts.Add(ry); verts.Add(rz); // Norm
                    verts.Add(t); verts.Add((float)j/6);
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
