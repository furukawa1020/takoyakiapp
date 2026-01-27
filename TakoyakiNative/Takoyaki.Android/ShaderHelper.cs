using Android.Content;
using Android.Opengl;
using Android.Util;
using System.IO;
using System.Text;

namespace Takoyaki.Android
{
    public static class ShaderHelper
    {
        public static int LoadProgram(Context context, string vertFile, string fragFile)
        {
            string vertCode = ReadAsset(context, vertFile);
            string fragCode = ReadAsset(context, fragFile);

            int vertexShader = LoadShader(GLES30.GlVertexShader, vertCode);
            int fragmentShader = LoadShader(GLES30.GlFragmentShader, fragCode);

            int program = GLES30.GlCreateProgram();
            GLES30.GlAttachShader(program, vertexShader);
            GLES30.GlAttachShader(program, fragmentShader);
            GLES30.GlLinkProgram(program);

            int[] linkStatus = new int[1];
            GLES30.GlGetProgramiv(program, GLES30.GlLinkStatus, linkStatus, 0);
            if (linkStatus[0] == 0)
            {
                Log.Error("TakoyakiShader", "Link Error: " + GLES30.GlGetProgramInfoLog(program));
                GLES30.GlDeleteProgram(program);
                return 0;
            }

            return program;
        }

        private static int LoadShader(int type, string code)
        {
            int shader = GLES30.GlCreateShader(type);
            GLES30.GlShaderSource(shader, code);
            GLES30.GlCompileShader(shader);

            int[] compiled = new int[1];
            GLES30.GlGetShaderiv(shader, GLES30.GlCompileStatus, compiled, 0);
            if (compiled[0] == 0)
            {
                Log.Error("TakoyakiShader", "Compile Error (" + type + "): " + GLES30.GlGetShaderInfoLog(shader));
                GLES30.GlDeleteShader(shader);
                return 0;
            }
            return shader;
        }

        private static string ReadAsset(Context context, string fileName)
        {
            using (var stream = context.Assets!.Open(fileName))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
