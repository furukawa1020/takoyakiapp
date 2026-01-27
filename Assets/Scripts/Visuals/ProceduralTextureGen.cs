using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public static class ProceduralTextureGen
    {
        public static Texture2D GenerateBatterTexture()
        {
            // Pale, slight creamy, wet look
            return GenerateFractalNoiseTexture(256, 8.0f, new Color(0.95f, 0.92f, 0.85f), new Color(0.9f, 0.88f, 0.8f), 2); 
        }

        public static Texture2D GenerateCookedTexture()
        {
            // Uneven Golden Brown with some darker spots
            return GenerateFractalNoiseTexture(256, 12.0f, new Color(0.85f, 0.6f, 0.2f), new Color(0.6f, 0.3f, 0.05f), 4); 
        }

        public static Texture2D GenerateBurntTexture()
        {
            // Charred black/dark brown
            return GenerateFractalNoiseTexture(256, 15.0f, new Color(0.15f, 0.1f, 0.05f), Color.black, 3);
        }

        public static Texture2D GenerateNoiseMap()
        {
            // Detailed height map
            return GenerateFractalNoiseTexture(256, 10.0f, Color.black, Color.white, 3);
        }

        private static Texture2D GenerateFractalNoiseTexture(int size, float scale, Color colorA, Color colorB, int octaves)
        {
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sample = 0;
                    float amplitude = 1;
                    float frequency = 1;
                    float totalAmplitude = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float xCoord = (float)x / size * scale * frequency;
                        float yCoord = (float)y / size * scale * frequency;
                        sample += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
                        
                        totalAmplitude += amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2.0f;
                    }
                    
                    sample /= totalAmplitude; // Normalize 0..1
                    
                    // Add some random high-freq noise for flour grain
                    float grain = Random.Range(-0.02f, 0.02f);
                    sample += grain;

                    pixels[y * size + x] = Color.Lerp(colorA, colorB, Mathf.Clamp01(sample));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
