using UnityEngine;
using UnityEngine.UI;

namespace TakoyakiPhysics.UI
{
    /// <summary>
    /// Control theory visualizer - displays proportional/integral/derivative metrics
    /// Real-time feedback on shaping algorithm performance
    /// </summary>
    public class ControlTheoryDisplay : MonoBehaviour
    {
        private static ControlTheoryDisplay _singleton;
        public static ControlTheoryDisplay GetInstance() => _singleton;

        [SerializeField] private Image proportionalIndicator;
        [SerializeField] private Image integralIndicator;
        [SerializeField] private Image derivativeIndicator;
        
        [SerializeField] private float barMaximumHeight = 100f;
        [SerializeField] private Color proportionalTint = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color integralTint = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color derivativeTint = new Color(0.3f, 0.5f, 1f);
        
        [SerializeField] private float proportionalScaling = 10f;
        [SerializeField] private float integralScaling = 5f;
        [SerializeField] private float derivativeScaling = 15f;
        
        private float proportionalTerm;
        private float integralTerm;
        private float derivativeTerm;
        
        private float desiredShape = 1.0f;
        private float accumulatedError;
        private float priorError;

        void Awake()
        {
            _singleton = this;
        }

        void Start()
        {
            if (!proportionalIndicator || !integralIndicator || !derivativeIndicator)
            {
                ConstructVisualizationBars();
            }
            
            if (proportionalIndicator) proportionalIndicator.color = proportionalTint;
            if (integralIndicator) integralIndicator.color = integralTint;
            if (derivativeIndicator) derivativeIndicator.color = derivativeTint;
        }

        void Update()
        {
            ComputeControlMetrics();
            RefreshBarHeights();
        }

        void ComputeControlMetrics()
        {
            float actualShape = 0f;
            var gameMgr = GameManager.Instance;
            if (gameMgr?.ActiveTakoyakis != null && gameMgr.ActiveTakoyakis.Length > 0)
            {
                actualShape = gameMgr.ActiveTakoyakis[0].ShapeIntegrity;
            }
            
            float shapeError = desiredShape - actualShape;
            
            proportionalTerm = shapeError;
            
            accumulatedError += shapeError * Time.deltaTime;
            accumulatedError = Mathf.Clamp(accumulatedError, -1f, 1f);
            integralTerm = accumulatedError;
            
            float errorRate = (shapeError - priorError) / Mathf.Max(Time.deltaTime, 0.001f);
            derivativeTerm = errorRate;
            
            priorError = shapeError;
            
            var rhythmTracker = TakoyakiPhysics.Game.RhythmHarmonyTracker.GetInstance();
            if (rhythmTracker)
            {
                float gyroError = rhythmTracker.GetRotationDeviation();
                proportionalTerm = Mathf.Max(proportionalTerm, gyroError / 10f);
            }
        }

        void RefreshBarHeights()
        {
            if (proportionalIndicator)
            {
                float pHeight = Mathf.Abs(proportionalTerm) * proportionalScaling;
                var rectTrans = proportionalIndicator.rectTransform;
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, Mathf.Min(pHeight, barMaximumHeight));
            }
            
            if (integralIndicator)
            {
                float iHeight = Mathf.Abs(integralTerm) * integralScaling;
                var rectTrans = integralIndicator.rectTransform;
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, Mathf.Min(iHeight, barMaximumHeight));
            }
            
            if (derivativeIndicator)
            {
                float dHeight = Mathf.Abs(derivativeTerm) * derivativeScaling;
                var rectTrans = derivativeIndicator.rectTransform;
                rectTrans.sizeDelta = new Vector2(rectTrans.sizeDelta.x, Mathf.Min(dHeight, barMaximumHeight));
            }
        }

        void ConstructVisualizationBars()
        {
            GameObject container = new GameObject("ControlTheory_Container");
            container.transform.SetParent(transform);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.1f);
            containerRect.anchorMax = new Vector2(0.2f, 0.4f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            proportionalIndicator = ConstructSingleBar(container.transform, "ProportionalBar", 0f);
            integralIndicator = ConstructSingleBar(container.transform, "IntegralBar", 30f);
            derivativeIndicator = ConstructSingleBar(container.transform, "DerivativeBar", 60f);
        }

        Image ConstructSingleBar(Transform parent, string barName, float xPosition)
        {
            GameObject barObject = new GameObject(barName);
            barObject.transform.SetParent(parent);
            
            RectTransform rectTransform = barObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(xPosition, 0);
            rectTransform.sizeDelta = new Vector2(20f, 0f);
            
            return barObject.AddComponent<Image>();
        }

        public Vector3 RetrieveCurrentMetrics()
        {
            return new Vector3(proportionalTerm, integralTerm, derivativeTerm);
        }
    }
}
