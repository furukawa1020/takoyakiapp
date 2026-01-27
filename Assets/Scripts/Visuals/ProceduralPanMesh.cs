using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public static class ProceduralPanMesh
    {
        public static Mesh Generate(int rows = 3, int cols = 3, float spacing = 1.5f)
        {
            Mesh mesh = new Mesh();
            mesh.name = "TakoyakiPanMesh";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            float pitRadius = 0.6f;
            float pitDepth = 0.55f; // Slightly deeper for stability
            
            // Calculate total size based on grid
            float width = spacing * (cols + 1) * 0.8f; // Extra border
            float depth = spacing * (rows + 1) * 0.8f;
            
            // High fidelity resolution
            // gridX/Z = 400 gives ~160k vertices. Good balance for PC.
            int gridX = 400; 
            int gridZ = 400;

            Vector3[] vertices = new Vector3[(gridX + 1) * (gridZ + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[gridX * gridZ * 6];

            // Pre-calculate pit centers
            Vector3[] pitCenters = new Vector3[rows * cols];
            float startX = -((cols - 1) * spacing) / 2.0f;
            float startZ = -((rows - 1) * spacing) / 2.0f;
            
            int pIdx = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    pitCenters[pIdx] = new Vector3(startX + c * spacing, 0, startZ + r * spacing);
                    pIdx++;
                }
            }

            for (int z = 0; z <= gridZ; z++)
            {
                for (int x = 0; x <= gridX; x++)
                {
                    int i = z * (gridX + 1) + x;
                    
                    float u = (float)x / gridX;
                    float v = (float)z / gridZ;

                    float xPos = (u - 0.5f) * width;
                    float zPos = (v - 0.5f) * depth;
                    float yPos = 0;

                    // Find closest pit and apply depression
                    for (int p = 0; p < pitCenters.Length; p++)
                    {
                        float dx = xPos - pitCenters[p].x;
                        float dz = zPos - pitCenters[p].z;
                        float distSq = dx*dx + dz*dz;
                        
                        // Optimization: Check distSq against radius^2
                        if (distSq < pitRadius * pitRadius)
                        {
                            float dist = Mathf.Sqrt(distSq);
                            
                            // Virtual sphere calculation for smooth curve
                            float sphereR = (pitRadius * pitRadius + pitDepth * pitDepth) / (2 * pitDepth);
                            float yCircle = -Mathf.Sqrt(Mathf.Max(0, sphereR * sphereR - dist * dist));
                            float depression = yCircle + (sphereR - pitDepth);
                            
                            // If depression is lower than current yPos (which starts at 0), use it
                            if (depression < yPos) yPos = depression;
                        }
                    }

                    vertices[i] = new Vector3(xPos, yPos, zPos);
                    uvs[i] = new Vector2(u, v);
                }
            }

            int tri = 0;
            for (int z = 0; z < gridZ; z++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    int i = z * (gridX + 1) + x;
                    
                    triangles[tri + 0] = i;
                    triangles[tri + 1] = i + gridX + 1;
                    triangles[tri + 2] = i + 1;

                    triangles[tri + 3] = i + 1;
                    triangles[tri + 4] = i + gridX + 1;
                    triangles[tri + 5] = i + gridX + 2;

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
