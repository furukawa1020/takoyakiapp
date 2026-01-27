using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public class ToppingVisuals : MonoBehaviour
    {
        public bool HasOctopus { get; private set; } = false;
        public bool HasGinger { get; private set; } = false;
        public bool HasAonori { get; private set; } = false;
        public bool HasBonito { get; private set; } = false;
        public bool HasMayo { get; private set; } = false;
        
        private GameObject _octopusLeg;
        private ParticleSystem _gingerFX;
        private ParticleSystem _aonoriFX;
        private ParticleSystem _bonitoFX;
        private LineRenderer _mayoLine;

        private void Start()
        {
            CreateOctopusLeg();
            CreateGingerFX();
            CreateAonoriFX();
            CreateBonitoFX();
            CreateMayoFX();
        }

        // --- OCTOPUS ---
        private void CreateOctopusLeg()
        {
            _octopusLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _octopusLeg.name = "OctopusLeg";
            Destroy(_octopusLeg.GetComponent<Collider>());
            
            _octopusLeg.transform.SetParent(this.transform);
            _octopusLeg.transform.localPosition = new Vector3(0, 0.3f, 0); 
            _octopusLeg.transform.localRotation = Quaternion.Euler(45, 0, 45); 
            _octopusLeg.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            
            Renderer r = _octopusLeg.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = new Color(0.8f, 0.1f, 0.1f); 
            r.material.SetFloat("_Glossiness", 0.7f); 
            
            _octopusLeg.SetActive(false); 
        }

        public void AddOctopus()
        {
            if (_octopusLeg != null)
            {
                _octopusLeg.SetActive(true);
                HasOctopus = true;
            }
        }

        // --- GINGER ---
        private void CreateGingerFX()
        {
            GameObject go = new GameObject("FX_Ginger");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            
            _gingerFX = go.AddComponent<ParticleSystem>();
            var main = _gingerFX.main;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 0.4f, 0.6f); 
            main.loop = false;
            main.playOnAwake = false;
            
            var emission = _gingerFX.emission;
            emission.enabled = false;
            
            var shape = _gingerFX.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f; 
            
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void AddGinger()
        {
            if (_gingerFX != null) 
            {
                _gingerFX.Emit(20);
                HasGinger = true;
            }
        }

        // --- AONORI ---
        private void CreateAonoriFX()
        {
            GameObject go = new GameObject("FX_Aonori");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0); 
            
            _aonoriFX = go.AddComponent<ParticleSystem>();
            var main = _aonoriFX.main;
            main.startSize = 0.02f; 
            main.startColor = new Color(0.1f, 0.5f, 0.1f); 
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 500;
            
            var emission = _aonoriFX.emission;
            emission.enabled = false;
            
            var shape = _aonoriFX.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.85f; 
            
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void AddAonori()
        {
             if (_aonoriFX != null) 
             {
                 _aonoriFX.Emit(50);
                 HasAonori = true;
             }
        }

        // --- BONITO ---
        private void CreateBonitoFX()
        {
            GameObject go = new GameObject("FX_Bonito");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);

            _bonitoFX = go.AddComponent<ParticleSystem>();
            var main = _bonitoFX.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.12f); 
            main.startColor = new Color(0.8f, 0.6f, 0.4f); 
            main.gravityModifier = 0f; 
            main.simulationSpace = ParticleSystemSimulationSpace.Local; 
            
            var emission = _bonitoFX.emission;
            emission.enabled = false;

            var shape = _bonitoFX.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.8f;

            var vel = _bonitoFX.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            vel.z = new ParticleSystem.MinMaxCurve(0.1f, 0.5f); 

            var noise = _bonitoFX.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 2.0f; 
            
            var rotation = _bonitoFX.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = 45f;

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Billboard;
            render.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void AddBonito()
        {
            if (_bonitoFX != null && !_bonitoFX.isPlaying)
            {
                _bonitoFX.Play();
                _bonitoFX.Emit(30);
                HasBonito = true;
            }
        }

        // --- MAYO ---
        private void CreateMayoFX()
        {
            GameObject go = new GameObject("FX_Mayo");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            _mayoLine = go.AddComponent<LineRenderer>();
            _mayoLine.useWorldSpace = false;
            _mayoLine.startWidth = 0.08f;
            _mayoLine.endWidth = 0.08f;
            _mayoLine.material = new Material(Shader.Find("Sprites/Default")); 
            
            int points = 20; // Smoother
            Vector3[] positions = new Vector3[points];
            float width = 0.7f;
            float length = 0.7f;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                float x = Mathf.Lerp(-width/2, width/2, t);
                float z = ((i % 4 < 2) ? -1 : 1) * length * 0.3f; // Jiggly Zig Zag
                
                float r = 0.82f; 
                float y = Mathf.Sqrt(Mathf.Max(0, r*r - x*x - z*z)); 
                
                positions[i] = new Vector3(x, y, z);
            }
            _mayoLine.positionCount = points;
            _mayoLine.SetPositions(positions);
            _mayoLine.enabled = false;
        }

        public void AddMayo()
        {
            if (_mayoLine != null)
            {
                _mayoLine.enabled = true;
                HasMayo = true;
            }
        }
    }
}
