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
            // Creamy, slightly uneven batter with domain warping for flow
            return GenerateAdvancedNoise(size, 8.0f, 
                new Vector4(0.98f, 0.92f, 0.75f, 1f), // Raw Batter (Yellow-White)
                new Vector4(0.92f, 0.85f, 0.6f, 1f),  // Shadows
                0.5f, 0.05f); // Soft warping
        }

        public static ProceduralTexture GenerateCooked(int size)
        {
            // Golden Brown with high contrast "Crispy" spots
            // Reference: https://imgur.com/takoyaki_render.png
            // Needs to look like fried dough, not just brown noise
            return GenerateAdvancedNoise(size, 15.0f, 
                new Vector4(0.85f, 0.55f, 0.1f, 1f), // Golden
                new Vector4(0.5f, 0.25f, 0.05f, 1f), // Deep Fried
                2.0f, 0.3f); // Strong warping for "fried" texture
        }
        
        public static ProceduralTexture GenerateBurnt(int size)
        {
            // Carbonized, sharp details
            return GenerateAdvancedNoise(size, 30.0f, 
                new Vector4(0.2f, 0.1f, 0.05f, 1f), // Dark Brown
                new Vector4(0.05f, 0.05f, 0.05f, 1f), // Black Char
                0.0f, 0.5f); // High grit
        }

        // Generates the Height/displacement map
        public static ProceduralTexture GenerateNoiseMap(int size)
        {
            return GenerateAdvancedNoise(size, 12.0f, 
                new Vector4(0f, 0f, 0f, 1f), 
                new Vector4(1f, 1f, 1f, 1f), 
                1.0f, 0.2f); 
        }

        private static ProceduralTexture GenerateAdvancedNoise(int size, float scale, Vector4 colA, Vector4 colB, float warpStrength, float gritAmount)
        {
            var tex = new ProceduralTexture(size);
            float seed = (float)(new Random().NextDouble() * 100);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 1. Domain Warping (f(p + f(p)))
                    float nx = (float)x / size * scale;
                    float ny = (float)y / size * scale;

                    float qx = PerlinNoise.Noise(nx + seed, ny + seed);
                    float qy = PerlinNoise.Noise(nx + 5.2f + seed, ny + 1.3f + seed);

                    float rx = nx + warpStrength * qx;
                    float ry = ny + warpStrength * qy;

                    float n = PerlinNoise.Noise(rx, ry); // Base Organic Noise

                    // 2. Add "Grit" (High frequency detail for crispiness/tenkasu)
                    float grit = PerlinNoise.Noise(nx * 10f, ny * 10f);
                    n += grit * gritAmount;

                    // Normalize roughly -1..1 -> 0..1
                    n = n * 0.5f + 0.5f;
                    
                    // Contrast Curve (Sharpen details)
                    n = n * n * (3 - 2 * n); 

                    Vector4 finalCol = Vector4.Lerp(colA, colB, Math.Clamp(n, 0f, 1f));
                    finalCol.W = 1.0f; // Alpha
                    tex.SetPixel(x, y, finalCol);
                }
            }
            return tex;
        }
    }
}
