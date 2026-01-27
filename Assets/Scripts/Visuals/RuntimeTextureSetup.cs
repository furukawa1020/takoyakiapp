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
                
                // transform checks to avoid overriding if already assigned in editor?
                // For this prototype, we ALWAYS generate to be safe and "Code First"
                
                if (mat.shader.name.Contains("Takoyaki"))
                {
                    mat.SetTexture("_MainTex", ProceduralTextureGen.GenerateBatterTexture());
                    mat.SetTexture("_CookedTex", ProceduralTextureGen.GenerateCookedTexture());
                    mat.SetTexture("_BurntTex", ProceduralTextureGen.GenerateBurntTexture());
                    mat.SetTexture("_NoiseTex", ProceduralTextureGen.GenerateNoiseMap());
                    
                    // Defaults if not set
                    if (mat.GetFloat("_OilFresnel") == 0) mat.SetFloat("_OilFresnel", 5.0f);
                    if (mat.GetFloat("_DisplacementStrength") == 0) mat.SetFloat("_DisplacementStrength", 0.05f);
                    
                    Debug.Log("[RuntimeTextureSetup] Procedural Textures Generated and Assigned.");
                }
            }
        }
    }
}
