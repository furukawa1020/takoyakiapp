using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public static class ProceduralTextureGen
    {
        public static Texture2D GenerateBatterTexture()
        {
            // Richer creamy yellow with more organic variance
            return GenerateFractalNoiseTexture(512, 12.0f, new Color(1.0f, 0.95f, 0.7f), new Color(0.92f, 0.8f, 0.5f), 4); 
        }

        public static Texture2D GenerateCookedTexture()
        {
            // Realistic Golden Brown with high contrast spots
            return GenerateFractalNoiseTexture(512, 20.0f, new Color(0.9f, 0.6f, 0.1f), new Color(0.5f, 0.25f, 0.05f), 5); 
        }

        public static Texture2D GenerateBurntTexture()
        {
            // Deep charred Carbon texture
            return GenerateFractalNoiseTexture(512, 25.0f, new Color(0.2f, 0.15f, 0.1f), Color.black, 4);
        }

        public static Texture2D GenerateNoiseMap()
        {
            // High freq detail
            return GenerateFractalNoiseTexture(512, 15.0f, Color.black, new Color(0.8f, 0.8f, 0.8f), 4);
        }

        private static Texture2D GenerateFractalNoiseTexture(int size, float scale, Color colorA, Color colorB, int octaves)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            float seed = Random.value * 100f; // Variation per generation

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
                        float xCoord = seed + (float)x / size * scale * frequency;
                        float yCoord = seed + (float)y / size * scale * frequency;
                        // Use 0-1 range Perlin Noise
                        float p = Mathf.PerlinNoise(xCoord, yCoord); 
                        sample += p * amplitude;
                        
                        totalAmplitude += amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2.0f;
                    }
                    
                    sample /= totalAmplitude;
                    
                    // Add subtle grain/blur
                    float grain = (Random.value - 0.5f) * 0.05f;
                    sample = Mathf.Clamp01(sample + grain);

                    // Non-linear color mapping for better "baked" look
                    // sample^2 pushes it towards colorA (lighter) or B (darker) depending on interpolation
                    float t = Mathf.SmoothStep(0f, 1f, sample);
                    
                    pixels[y * size + x] = Color.Lerp(colorA, colorB, t);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true); // Generate Mipmaps
            return tex;
        }
    }
}
    }
}
