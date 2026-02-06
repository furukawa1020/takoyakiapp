using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    /// <summary>
    /// Golden aura screen overlay - vignette effect for perfect harmony state
    /// Custom post-processing for zen mastery visualization
    /// </summary>
    public class GoldenAuraOverlay : MonoBehaviour
    {
        private static GoldenAuraOverlay _singleton;
        public static GoldenAuraOverlay GetInstance() => _singleton;

        [SerializeField] private Color auralTint = new Color(1f, 0.85f, 0.5f, 1f);
        [SerializeField] private float peakOpacity = 0.8f;
        [SerializeField] private float transitionRate = 2f;
        [SerializeField] private float vignetteRadius = 0.15f;
        [SerializeField] private float vignetteSmoothness = 0.3f;
        
        private Material postProcessMat;
        private float currentOpacity;
        private float desiredOpacity;

        void Awake()
        {
            _singleton = this;
        }

        void Start()
        {
            Shader overlayShader = Shader.Find("Unlit/Color");
            if (overlayShader)
            {
                postProcessMat = new Material(overlayShader);
            }
        }

        void Update()
        {
            currentOpacity = Mathf.Lerp(currentOpacity, desiredOpacity, Time.deltaTime * transitionRate);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (postProcessMat && currentOpacity > 0.01f)
            {
                postProcessMat.SetColor("_GlowColor", auralTint);
                postProcessMat.SetFloat("_Intensity", currentOpacity);
                postProcessMat.SetFloat("_EdgeThickness", vignetteRadius);
                postProcessMat.SetFloat("_EdgeSoftness", vignetteSmoothness);
                Graphics.Blit(src, dest, postProcessMat);
            }
            else
            {
                Graphics.Blit(src, dest);
            }
        }

        public void AdjustAuraStrength(float harmonyValue)
        {
            if (harmonyValue > 0.7f)
            {
                float normalizedGlow = (harmonyValue - 0.7f) / 0.3f;
                desiredOpacity = normalizedGlow * peakOpacity;
            }
            else
            {
                desiredOpacity = 0f;
            }
        }

        void OnDestroy()
        {
            if (postProcessMat) Destroy(postProcessMat);
        }
    }
}
