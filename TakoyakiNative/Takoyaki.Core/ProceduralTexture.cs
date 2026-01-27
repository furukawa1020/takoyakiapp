using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public class ProceduralTexture
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Pixels { get; private set; } // RGBA 8888

        public ProceduralTexture(int size)
        {
            Width = size;
            Height = size;
            Pixels = new byte[size * size * 4];
        }

        public void SetPixel(int x, int y, Vector4 color)
        {
            // Clamp 0..1
            color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
            
            int idx = (y * Width + x) * 4;
            Pixels[idx] = (byte)(color.X * 255);
            Pixels[idx + 1] = (byte)(color.Y * 255);
            Pixels[idx + 2] = (byte)(color.Z * 255);
            Pixels[idx + 3] = (byte)(color.W * 255);
        }

        // --- Generations ---

        public static ProceduralTexture GenerateBatter(int size)
        {
            return GenerateFractal(size, 12.0f, new Vector4(1.0f, 0.95f, 0.7f, 1f), new Vector4(0.92f, 0.8f, 0.5f, 1f), 4);
        }

        public static ProceduralTexture GenerateCooked(int size)
        {
            // Deep Golden Brown
            return GenerateFractal(size, 20.0f, new Vector4(0.9f, 0.6f, 0.1f, 1f), new Vector4(0.5f, 0.25f, 0.05f, 1f), 5);
        }
        
        public static ProceduralTexture GenerateBurnt(int size)
        {
            return GenerateFractal(size, 25.0f, new Vector4(0.2f, 0.15f, 0.1f, 1f), new Vector4(0.0f, 0.0f, 0.0f, 1f), 4);
        }
        
         public static ProceduralTexture GenerateNoiseMap(int size)
        {
            // Scalar for displacement
            return GenerateFractal(size, 15.0f, new Vector4(0f, 0f, 0f, 1f), new Vector4(1f, 1f, 1f, 1f), 4);
        }

        private static ProceduralTexture GenerateFractal(int size, float scale, Vector4 colA, Vector4 colB, int octaves)
        {
            var tex = new ProceduralTexture(size);
            float seed = (float)(new Random().NextDouble() * 100);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sample = 0;
                    float amplitude = 1;
                    float frequency = 1;
                    float totalAmp = 0;

                    for (int i = 0; i < octaves; i++)
                    {
                        float nx = seed + (float)x / size * scale * frequency;
                        float ny = seed + (float)y / size * scale * frequency;
                        // Map 0..1 roughly
                        float p = PerlinNoise.Noise(nx, ny) * 0.5f + 0.5f; 
                        sample += p * amplitude;
                        totalAmp += amplitude;
                        amplitude *= 0.5f;
                        frequency *= 2.0f;
                    }
                    sample /= totalAmp;

                    // Non-linear mapping for contrast
                    float t = sample * sample * (3 - 2 * sample); // Smoothstep
                    
                    Vector4 finalCol = Vector4.Lerp(colA, colB, t);
                    finalCol.W = 1.0f;
                    tex.SetPixel(x, y, finalCol);
                }
            }
            return tex;
        }
    }
}
