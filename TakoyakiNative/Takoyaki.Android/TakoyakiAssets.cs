using Android.Content;
using Android.Opengl;
using Takoyaki.Core;

namespace Takoyaki.Android
{
    public class TakoyakiAssets
    {
        public int MainProgram { get; private set; }
        public int ToppingProgram { get; private set; }
        
        public int BatterTex { get; private set; }
        public int CookedTex { get; private set; }
        public int BurntTex { get; private set; }
        public int NoiseTex { get; private set; }

        public void Initialize(Context context)
        {
            // Load Shaders
            MainProgram = ShaderHelper.LoadProgram(context, "takoyaki.vert", "takoyaki.frag");
            ToppingProgram = ShaderHelper.LoadProgram(context, "topping.vert", "topping.frag"); 

            // Load Textures
            BatterTex = LoadProceduralTexture(ProceduralTexture.GenerateBatter(64));
            CookedTex = LoadProceduralTexture(ProceduralTexture.GenerateCooked(64));
            BurntTex = LoadProceduralTexture(ProceduralTexture.GenerateBurnt(64));
            NoiseTex = LoadProceduralTexture(ProceduralTexture.GenerateNoiseMap(64));
        }

        private int LoadProceduralTexture(ProceduralTexture tex)
        {
            int[] texIds = new int[1];
            GLES30.GlGenTextures(1, texIds, 0);
            int id = texIds[0];

            GLES30.GlBindTexture(GLES30.GlTexture2d, id);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMinFilter, GLES30.GlLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureMagFilter, GLES30.GlLinear);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureWrapS, GLES30.GlRepeat);
            GLES30.GlTexParameteri(GLES30.GlTexture2d, GLES30.GlTextureWrapT, GLES30.GlRepeat);

            // Upload the pixel buffer directly to the GPU
            GLES30.GlTexImage2D(GLES30.GlTexture2d, 0, GLES30.GlRgba, tex.Width, tex.Height, 0, GLES30.GlRgba, GLES30.GlUnsignedByte, Java.Nio.ByteBuffer.Wrap(tex.Pixels));

            return id;
        }

        public void BindTextures()
        {
            GLES30.GlActiveTexture(GLES30.GlTexture0);
            GLES30.GlBindTexture(GLES30.GlTexture2d, BatterTex);
            
            GLES30.GlActiveTexture(GLES30.GlTexture1);
            GLES30.GlBindTexture(GLES30.GlTexture2d, CookedTex);
            
            GLES30.GlActiveTexture(GLES30.GlTexture2);
            GLES30.GlBindTexture(GLES30.GlTexture2d, BurntTex);
            
            GLES30.GlActiveTexture(GLES30.GlTexture3);
            GLES30.GlBindTexture(GLES30.GlTexture2d, NoiseTex);
        }
    }
}
