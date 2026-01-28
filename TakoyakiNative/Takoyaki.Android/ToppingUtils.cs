using System;
using Android.Opengl;
using Takoyaki.Core;

namespace Takoyaki.Android
{
    public static class ToppingUtils
    {
        public static void UploadToGPU(ToppingMesh mesh)
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

        public static float[] CalculateRotationToNormal(System.Numerics.Vector3 position)
        {
            var normal = System.Numerics.Vector3.Normalize(position);
            var defaultUp = new System.Numerics.Vector3(0, 0, 1);
            
            var axis = System.Numerics.Vector3.Cross(defaultUp, normal);
            float angle = (float)Math.Acos(System.Numerics.Vector3.Dot(defaultUp, normal));
            
            float[] rotMat = new float[16];
            Matrix.SetIdentityM(rotMat, 0);
            
            if (axis.LengthSquared() > 0.0001f)
            {
                axis = System.Numerics.Vector3.Normalize(axis);
                Matrix.RotateM(rotMat, 0, angle * 180f / (float)Math.PI, axis.X, axis.Y, axis.Z);
            }
            else if (System.Numerics.Vector3.Dot(defaultUp, normal) < -0.99f)
            {
                 Matrix.RotateM(rotMat, 0, 180f, 1, 0, 0);
            }
            return rotMat;
        }
    }
}
