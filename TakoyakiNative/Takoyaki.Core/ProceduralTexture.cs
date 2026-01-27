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
            // 1. Base Fried Crust (Worley)
            var tex = GenerateWorleyCrust(size, 8.0f, 
                new Vector4(0.85f, 0.55f, 0.1f, 1f), // Base Golden
                new Vector4(0.4f, 0.2f, 0.05f, 1f),  // Deep Fried Spots
                new Vector4(1.0f, 0.9f, 0.8f, 1f));  // Highlight/Flour
            
            // 2. Add Tenkasu / Burnt Bits (High Frequency Grit)
            // Small, sharp, dark islands
            AddDebrisPass(tex, 50.0f, 0.7f, new Vector4(0.2f, 0.1f, 0.0f, 1f));

            return tex;
        }

        public static ProceduralTexture GenerateBurnt(int size)
        {
            // Dark base + Heavy Char debris
            var tex = GenerateWorleyCrust(size, 20.0f, 
                 new Vector4(0.2f, 0.1f, 0.05f, 1f), 
                 new Vector4(0.1f, 0.05f, 0.0f, 1f),
                 new Vector4(0.2f, 0.15f, 0.1f, 1f));
            
            AddDebrisPass(tex, 60.0f, 0.6f, new Vector4(0.05f, 0.05f, 0.05f, 1f));
            return tex;
        }

        public static ProceduralTexture GenerateNoiseMap(int size)
        {
            // Height Map: Base Worley + Debris bumps
            var tex = GenerateWorleyCrust(size, 8.0f, 
                new Vector4(0f, 0f, 0f, 1f), // Low
                new Vector4(1f, 1f, 1f, 1f), // High
                new Vector4(0.5f, 0.5f, 0.5f, 1f));
            
            // Debris sticks out
            AddDebrisPass(tex, 50.0f, 0.7f, new Vector4(1f, 1f, 1f, 1f)); 
            return tex;
        }

        // ... GenerateWorleyCrust implementation ...

        private static void AddDebrisPass(ProceduralTexture tex, float scale, float threshold, Vector4 debrisColor)
        {
            float seed = (float)(new Random().NextDouble() * 100);
            for (int y = 0; y < tex.Height; y++)
            {
                for (int x = 0; x < tex.Width; x++)
                {
                    float nx = (float)x / tex.Width * scale;
                    float ny = (float)y / tex.Height * scale;

                    // Sharp white noise
                    float noise = PerlinNoise.Noise(nx, ny); // -1..1
                    // Make it sparse
                    if (noise > threshold) // e.g. > 0.7
                    {
                        // It's a flake/bit
                        // Alpha blend
                        int idx = (y * tex.Width + x) * 4;
                        // Read existing
                        Vector4 current = new Vector4(
                            tex.Pixels[idx] / 255.0f,
                            tex.Pixels[idx+1] / 255.0f,
                            tex.Pixels[idx+2] / 255.0f,
                            1.0f
                        );
                        
                        Vector4 result = Vector4.Lerp(current, debrisColor, 0.8f);
                        tex.SetPixel(x, y, result);
                    }
                }
            }
        }

        private static ProceduralTexture GenerateWorleyCrust(int size, float scale, Vector4 colBase, Vector4 colDeep, Vector4 colHigh)
        {
            // ... (Previous logic, ensure it's preserved or re-pasted here if replacing whole block)
            // Re-pasting the exact GenerateWorleyCrust logic for safety as per tool instruction
             var tex = new ProceduralTexture(size);
            float seed = (float)(new Random().NextDouble() * 100);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (float)x / size * scale;
                    float ny = (float)y / size * scale;
                    float w1 = WorleyNoise.Noise(nx, ny, seed);
                    float w2 = WorleyNoise.Noise(nx * 4f, ny * 4f, seed + 10f);
                    float bump = (1.0f - w1); 
                    bump += (1.0f - w2) * 0.3f; 
                    bump = Math.Clamp(bump, 0f, 1f);
                    
                    Vector4 finalCol;
                    if (bump < 0.3f)
                    {
                        float t = bump / 0.3f;
                        finalCol = Vector4.Lerp(colDeep, colBase, t);
                    }
                    else
                    {
                        float t = (bump - 0.3f) / 0.7f;
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
