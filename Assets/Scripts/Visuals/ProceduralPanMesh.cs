using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public static class ProceduralPanMesh
    {
        public static Mesh Generate()
        {
            Mesh mesh = new Mesh();
            mesh.name = "TakoyakiPanMesh";

            int gridSize = 40; // Resolution
            float size = 2.0f; // Total width
            float pitRadius = 0.6f;
            float pitDepth = 0.5f;

            Vector3[] vertices = new Vector3[(gridSize + 1) * (gridSize + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[gridSize * gridSize * 6];

            for (int y = 0; y <= gridSize; y++)
            {
                for (int x = 0; x <= gridSize; x++)
                {
                    int i = y * (gridSize + 1) + x;
                    
                    float u = (float)x / gridSize;
                    float v = (float)y / gridSize;

                    float xPos = (u - 0.5f) * size;
                    float zPos = (v - 0.5f) * size;
                    float yPos = 0;

                    // Create Pit
                    float dist = Mathf.Sqrt(xPos * xPos + zPos * zPos);
                    if (dist < pitRadius)
                    {
                        // Spherical depression
                        // Normalized distance
                        float t = dist / pitRadius;
                        // y = -sqrt(1 - t^2) * depth (Unit sphere curve scaled)
                        // But let's use cosine for smoother falloff at edges
                        // yPos = -Mathf.Cos(t * Mathf.PI * 0.5f) * pitDepth; // Sharp?
                        
                        // Let's use simple sphere equation: x^2 + y^2 + z^2 = r^2
                        // y = -sqrt(r^2 - dist^2)
                        // Virtual sphere for the pit
                        float sphereR = (pitRadius * pitRadius + pitDepth * pitDepth) / (2 * pitDepth);
                        float yCircle = -Mathf.Sqrt(Mathf.Max(0, sphereR * sphereR - dist * dist));
                        yPos = yCircle + (sphereR - pitDepth);
                    }

                    vertices[i] = new Vector3(xPos, yPos, zPos);
                    uvs[i] = new Vector2(u, v);
                }
            }

            int tri = 0;
            for (int y = 0; y < gridSize; y++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    int i = y * (gridSize + 1) + x;
                    
                    triangles[tri + 0] = i;
                    triangles[tri + 1] = i + gridSize + 1;
                    triangles[tri + 2] = i + 1;

                    triangles[tri + 3] = i + 1;
                    triangles[tri + 4] = i + gridSize + 1;
                    triangles[tri + 5] = i + gridSize + 2;

                    tri += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
    }
}
