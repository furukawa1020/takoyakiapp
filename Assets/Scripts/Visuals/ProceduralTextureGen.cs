using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public static class ProceduralTextureGen
    {
        public static Texture2D GenerateBatterTexture()
        {
            return GenerateNoiseTexture(256, 5.0f, new Color(1.0f, 0.95f, 0.8f), new Color(0.9f, 0.85f, 0.7f)); // Creamy white
        }

        public static Texture2D GenerateCookedTexture()
        {
            return GenerateNoiseTexture(256, 8.0f, new Color(0.8f, 0.5f, 0.2f), new Color(0.6f, 0.3f, 0.1f)); // Golden Brown
        }

        public static Texture2D GenerateBurntTexture()
        {
            return GenerateNoiseTexture(256, 12.0f, new Color(0.2f, 0.1f, 0.1f), Color.black); // Charred
        }

        public static Texture2D GenerateNoiseMap()
        {
            return GenerateNoiseTexture(256, 10.0f, Color.black, Color.white); // Height map
        }

        private static Texture2D GenerateNoiseTexture(int size, float scale, Color colorA, Color colorB)
        {
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float xCoord = (float)x / size * scale;
                    float yCoord = (float)y / size * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    pixels[y * size + x] = Color.Lerp(colorA, colorB, sample);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
