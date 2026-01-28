using System;
using System.Collections.Generic;
using Android.Opengl;
using Java.Nio;

namespace Takoyaki.Android
{
    /// <summary>
    /// Manager for all takoyaki toppings.
    /// Orchestrates generation and synchronized rendering with the main ball.
    /// </summary>
    public class TakoyakiToppings
    {
        private ToppingMesh _sauceMesh;
        private ToppingMesh _mayoMesh;
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
            
            // Generate through specialized providers
            _sauceMesh = SauceMeshGenerator.Create();
            _mayoMesh = MayoMeshGenerator.Create();
            _aonoriMeshes = AonoriMeshGenerator.Create(24);
            _katsuobushiMeshes = KatsuobushiMeshGenerator.Create(16);
        }
        
        public void SetToppingVisible(int stage)
        {
            // Cumulative visibility based on game progress
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
            if (_sauceMesh != null) { _sauceMesh.Visible = false; _sauceMesh.Animation = new ToppingAnimationState(); }
            if (_mayoMesh != null) { _mayoMesh.Visible = false; _mayoMesh.Animation = new ToppingAnimationState(); }
            foreach(var m in _aonoriMeshes) { m.Visible = false; m.Animation = new ToppingAnimationState(); }
            foreach(var m in _katsuobushiMeshes) { m.Visible = false; m.Animation = new ToppingAnimationState(); }
        }

        public void UpdateAnimations(float dt, float heat, float wobble)
        {
            if (_sauceMesh != null && _sauceMesh.Visible) _sauceMesh.Animation.Update(dt, heat, wobble);
            if (_mayoMesh != null && _mayoMesh.Visible) _mayoMesh.Animation.Update(dt, heat, wobble);
            foreach(var m in _aonoriMeshes) if(m.Visible) m.Animation.Update(dt, heat, wobble);
            foreach(var m in _katsuobushiMeshes) if(m.Visible) m.Animation.Update(dt, heat, wobble);
        }

        public void RenderRecursive(float[] vpMatrix, float[] parentModel, float time)
        {
            GLES30.GlUseProgram(_toppingProgram);
            
            // Set Global Uniforms
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_toppingProgram, "uVPMatrix"), 1, false, vpMatrix, 0);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_toppingProgram, "uTime"), time);
            
            // Shared State
            GLES30.GlDisable(2884); // GL_CULL_FACE (Double sided)
            GLES30.GlEnable(GLES30.GlBlend);
            GLES30.GlBlendFunc(GLES30.GlSrcAlpha, GLES30.GlOneMinusSrcAlpha);
            
            // 1. Sauce (Bottom layer)
            if (_sauceMesh != null && _sauceMesh.Visible) RenderMeshEx(_sauceMesh, parentModel, 0.2f); 
            
            // 2. Mayo
            if (_mayoMesh != null && _mayoMesh.Visible) RenderMeshEx(_mayoMesh, parentModel, 0.4f);
            
            // 3. Aonori (Matte)
            foreach(var m in _aonoriMeshes) if(m.Visible) RenderMeshEx(m, parentModel, 0.9f); 
            
            // 4. Katsuobushi (Matte / Rough)
            foreach(var m in _katsuobushiMeshes) if(m.Visible) RenderMeshEx(m, parentModel, 0.8f);

            GLES30.GlEnable(2884); 
        }

        private void RenderMeshEx(ToppingMesh mesh, float[] parentModel, float roughness)
        {
            float[] localModel = new float[16];
            Matrix.SetIdentityM(localModel, 0);
            
            // Base Position + Procedural Animation Offset
            Matrix.TranslateM(localModel, 0, 
                mesh.Position.X + mesh.Animation.CurrentOffset.X, 
                mesh.Position.Y + mesh.Animation.CurrentOffset.Y, 
                mesh.Position.Z + mesh.Animation.CurrentOffset.Z);
            
            // Base Rotation
            if (mesh.RotationMatrix != null) {
                float[] temp = new float[16];
                Matrix.MultiplyMM(temp, 0, localModel, 0, mesh.RotationMatrix, 0);
                Array.Copy(temp, localModel, 16);
            }
            
            // Procedural Animation Rotation (The "Dance")
            Matrix.RotateM(localModel, 0, mesh.Animation.CurrentRotation.X, 1, 0, 0);
            Matrix.RotateM(localModel, 0, mesh.Animation.CurrentRotation.Y, 0, 1, 0);
            Matrix.RotateM(localModel, 0, mesh.Animation.CurrentRotation.Z, 0, 0, 1);
            
            // Scale (including heat shrinking)
            float shrink = 1.0f - (mesh.Animation.HeatReaction * 0.2f);
            Matrix.ScaleM(localModel, 0, mesh.Scale.X * shrink, mesh.Scale.Y * shrink, mesh.Scale.Z * shrink);
            
            float[] finalModel = new float[16];
            Matrix.MultiplyMM(finalModel, 0, parentModel, 0, localModel, 0);
            
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_toppingProgram, "uModelMatrix"), 1, false, finalModel, 0);
            GLES30.GlUniform4f(GLES30.GlGetUniformLocation(_toppingProgram, "uColor"), mesh.Color.X, mesh.Color.Y, mesh.Color.Z, mesh.Color.W);
            GLES30.GlUniform1f(GLES30.GlGetUniformLocation(_toppingProgram, "uRoughness"), roughness);

            GLES30.GlBindBuffer(GLES30.GlArrayBuffer, mesh.VBO);
            GLES30.GlBindBuffer(GLES30.GlElementArrayBuffer, mesh.IBO);
            
            int stride = (3+3+2)*4; 
            GLES30.GlEnableVertexAttribArray(0); GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, stride, 0);
            GLES30.GlEnableVertexAttribArray(1); GLES30.GlVertexAttribPointer(1, 3, GLES30.GlFloat, false, stride, 12);
            GLES30.GlEnableVertexAttribArray(2); GLES30.GlVertexAttribPointer(2, 2, GLES30.GlFloat, false, stride, 24);
            
            GLES30.GlDrawElements(GLES30.GlTriangles, mesh.IndexCount, GLES30.GlUnsignedShort, 0);
            
            GLES30.GlDisableVertexAttribArray(0);
            GLES30.GlDisableVertexAttribArray(1);
            GLES30.GlDisableVertexAttribArray(2);
        }
    }
}
