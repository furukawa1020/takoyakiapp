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
            // Reference: Deep fried, bubbly crust.
            // Worley Noise creates the "Bubble" shapes.
            // We invert it 1-W to get "Cracks" or use W for "Bumps".
            
            return GenerateWorleyCrust(size, 8.0f, 
                new Vector4(0.85f, 0.55f, 0.1f, 1f), // Base Golden
                new Vector4(0.4f, 0.2f, 0.05f, 1f),  // Deep Fried Spots
                new Vector4(1.0f, 0.9f, 0.8f, 1f));  // Highlight/Flour
        }

        public static ProceduralTexture GenerateNoiseMap(int size)
        {
            // Height Map: Needs to match the visual crust.
            // High Worley values = Bumps.
            return GenerateWorleyCrust(size, 8.0f, 
                new Vector4(0f, 0f, 0f, 1f), 
                new Vector4(1f, 1f, 1f, 1f), 
                new Vector4(0.5f, 0.5f, 0.5f, 1f));
        }

        private static ProceduralTexture GenerateWorleyCrust(int size, float scale, Vector4 colBase, Vector4 colDeep, Vector4 colHigh)
        {
            var tex = new ProceduralTexture(size);
            float seed = (float)(new Random().NextDouble() * 100);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * scale;
                    float ny = (float)y / size * scale;

                    // 1. Base Worley (Large Bubbles)
                    float w1 = WorleyNoise.Noise(nx, ny, seed);
                    
                    // 2. Detail Worley (Small crispies)
                    float w2 = WorleyNoise.Noise(nx * 4f, ny * 4f, seed + 10f);

                    // Composite
                    // w1 is dist to center. 0=Center, 1=Edge.
                    // Fried food often has "bubbled" surface. 
                    // Let's use (1-w1) as the "Bump". Center is high.
                    float bump = (1.0f - w1); 
                    
                    // Add micro details
                    bump += (1.0f - w2) * 0.3f; 

                    // Sharpen
                    bump = Math.Clamp(bump, 0f, 1f);
                    
                    // Color Mapping
                    // Low bump (crevices) -> Deep Color (Oil pools, darker)
                    // High bump (tops) -> Base Color -> Webbing Highlight
                    
                    Vector4 finalCol;
                    if (bump < 0.3f)
                    {
                        // Crevice
                        float t = bump / 0.3f;
                        finalCol = Vector4.Lerp(colDeep, colBase, t);
                    }
                    else
                    {
                        // Top
                        float t = (bump - 0.3f) / 0.7f;
                        // Add some flour noise on top
                        float flour = PerlinNoise.Noise(nx * 10f, ny * 10f) > 0.6f ? 0.2f : 0.0f;
                        finalCol = Vector4.Lerp(colBase, colHigh, t * 0.5f + flour);
                    }

                    finalCol.W = 1.0f;
                    tex.SetPixel(x, y, finalCol);
                }
            }
            return tex;
        }
    }
}
