using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public class RuntimeTextureSetup : MonoBehaviour
    {
        private void Start()
        {
            Renderer r = GetComponent<Renderer>();
            if (r != null)
            {
                // Ensure material exists (clone)
                Material mat = r.material; 
                
                // Force Assign Shader if it's not correct
                if (!mat.shader.name.Contains("Takoyaki"))
                {
                    Shader s = Shader.Find("Takoyaki/TakoyakiCinematic");
                    if (s != null) 
                    {
                        mat.shader = s;
                        Debug.Log("[RuntimeTextureSetup] Force assigned TakoyakiCinematic shader.");
                    }
                    else
                    {
                        Debug.LogError("[RuntimeTextureSetup] Critical Error: 'Takoyaki/TakoyakiCinematic' Shader not found!");
                        // Fallback to Standard to see something at least
                        mat.shader = Shader.Find("Standard");
                        mat.color = Color.yellow; 
                    }
                }

                if (mat.shader.name.Contains("Takoyaki"))
                {
                    mat.SetTexture("_MainTex", ProceduralTextureGen.GenerateBatterTexture());
                    mat.SetTexture("_CookedTex", ProceduralTextureGen.GenerateCookedTexture());
                    mat.SetTexture("_BurntTex", ProceduralTextureGen.GenerateBurntTexture());
                    mat.SetTexture("_NoiseTex", ProceduralTextureGen.GenerateNoiseMap());
                    
                    // Defaults if not set
                    mat.SetFloat("_OilFresnel", 5.0f);
                    mat.SetFloat("_DisplacementStrength", 0.05f);
                    mat.SetFloat("_CookLevel", 0.0f); // Ensure it starts raw
                    
                    Debug.Log("[RuntimeTextureSetup] Procedural Textures Generated and Assigned Successfully.");
                }
            }
        }
    }
}
