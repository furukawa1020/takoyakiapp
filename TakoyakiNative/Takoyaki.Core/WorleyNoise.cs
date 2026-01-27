using System;
using System.Numerics;

namespace Takoyaki.Core
{
    public static class WorleyNoise
    {
        // Cellular Noise (Voronoi)
        // Returns magnitude of distance to nearest feature point (0..1 approx)
        // producing a "bubbled" or "cracked" texture.
        
        public static float Noise(float x, float y, float z)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            int zi = (int)Math.Floor(z);

            float minDist = 1.0f;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        // Neighbor cell
                        int nx = xi + dx;
                        int ny = yi + dy;
                        int nz = zi + dz;

                        // Feature point inside neighbor
                        Vector3 point = GetFeaturePoint(nx, ny, nz);
                        
                        // Distance from current pixel to that feature point
                        // Current pixel local pos is (x, y, z)
                        // Feature point world pos is point
                        float dist = Vector3.Distance(new Vector3(x, y, z), point);
                        
                        if (dist < minDist)
                        {
                            minDist = dist;
                        }
                    }
                }
            }
            return minDist; // 0 (center of bubble) to 1 (edge)
        }

        // Returns world position of the feature point in cell (x,y,z)
        private static Vector3 GetFeaturePoint(int x, int y, int z)
        {
            // Pseudo-random hash
            int h = Hash(x, y, z);
            float rx = (h & 255) / 255.0f;
            float ry = ((h >> 8) & 255) / 255.0f;
            float rz = ((h >> 16) & 255) / 255.0f;
            
            return new Vector3(x + rx, y + ry, z + rz);
        }

        private static int Hash(int x, int y, int z)
        {
            // Simple XOR hash
            int h = x * 374761393 + y * 668265263 + z * 10595295; // Primes
            h = (h ^ (h >> 13)) * 1274126177;
            return h ^ (h >> 16);
        }
    }
}
