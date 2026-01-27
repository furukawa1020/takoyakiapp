using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    public class ToppingVisuals : MonoBehaviour
    {
        public bool HasOctopus { get; private set; } = false;
        
        private GameObject _octopusLeg;
        private ParticleSystem _gingerFX;
        private ParticleSystem _aonoriFX;

        private void Start()
        {
            CreateOctopusLeg();
            CreateGingerFX();
            CreateAonoriFX();
        }

        private void CreateOctopusLeg()
        {
            // Procedural Octopus Leg: A Red Capsule
            _octopusLeg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _octopusLeg.name = "OctopusLeg";
            Destroy(_octopusLeg.GetComponent<Collider>()); // Visual only
            
            _octopusLeg.transform.SetParent(this.transform);
            _octopusLeg.transform.localPosition = new Vector3(0, 0.3f, 0); // Sticking out
            _octopusLeg.transform.localRotation = Quaternion.Euler(45, 0, 45); // Angled
            _octopusLeg.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            
            Renderer r = _octopusLeg.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Standard"));
            r.material.color = new Color(0.8f, 0.1f, 0.1f); // Deep Red
            r.material.smoothness = 0.7f; // Wet look
            
            // Initially hidden. Must be added by player.
            _octopusLeg.SetActive(false); 
        }

        public void AddOctopus()
        {
            if (_octopusLeg != null)
            {
                _octopusLeg.SetActive(true);
                HasOctopus = true;
                // Add "Plop" effect here later
            }
        }

        private void CreateGingerFX()
        {
            // Procedural Particles for Ginger
            GameObject go = new GameObject("FX_Ginger");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            
            _gingerFX = go.AddComponent<ParticleSystem>();
            var main = _gingerFX.main;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 0.4f, 0.6f); // Pink
            main.loop = false;
            main.playOnAwake = false;
            
            var emission = _gingerFX.emission;
            emission.enabled = false;
            
            var shape = _gingerFX.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f; // Cover surface
            
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        private void CreateAonoriFX()
        {
            // Green speckles
            GameObject go = new GameObject("FX_Aonori");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0); // Up
            
            _aonoriFX = go.AddComponent<ParticleSystem>();
            var main = _aonoriFX.main;
            main.startSize = 0.02f; // Tiny
            main.startColor = new Color(0.1f, 0.5f, 0.1f); // Dark Green
            main.loop = false;
            main.playOnAwake = false;
             main.maxParticles = 500;
            
            var emission = _aonoriFX.emission;
            emission.enabled = false;
            
            var shape = _aonoriFX.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.85f; // On surface
            
            var render = go.GetComponent<ParticleSystemRenderer>();
            render.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void AddGinger()
        {
            _gingerFX.Emit(20);
        }
        
        public void AddBonito()
        {
            if (_bonitoFX != null && !_bonitoFX.isPlaying)
            {
                _bonitoFX.Play(); // Loop them? Or just emit a bunch that stay?
                // Real bonito moves with heat. Let's emit ones with high damping and noise.
                _bonitoFX.Emit(30);
            }
        }

        public void AddMayo()
        {
            if (_mayoLine != null)
            {
                _mayoLine.enabled = true;
                // Animate drawing later? For now just appear.
            }
        }

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

        private void CreateBonitoFX()
        {
            // Dancing Katsuobushi
            GameObject go = new GameObject("FX_Bonito");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(-90, 0, 0);

            _bonitoFX = go.AddComponent<ParticleSystem>();
            var main = _bonitoFX.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.12f); // Paper thin
            main.startColor = new Color(0.8f, 0.6f, 0.4f); // Light brown
            main.gravityModifier = 0f; // They float/dance
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // Move with ball
            
            var emission = _bonitoFX.emission;
            emission.enabled = false;

            var shape = _bonitoFX.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.8f;

            var vel = _bonitoFX.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            vel.z = new ParticleSystem.MinMaxCurve(0.1f, 0.5f); // Rise slightly

            // The "Dance" comes from Noise
            var noise = _bonitoFX.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 2.0f; // Fast fluttering
            
            var rotation = _bonitoFX.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = 45f;

            var render = go.GetComponent<ParticleSystemRenderer>();
            render.renderMode = ParticleSystemRenderMode.Billboard;
            render.material = new Material(Shader.Find("Sprites/Default"));
        }

        private void CreateMayoFX()
        {
            // Mayonnaise Zig-Zag
            GameObject go = new GameObject("FX_Mayo");
            go.transform.SetParent(this.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            _mayoLine = go.AddComponent<LineRenderer>();
            _mayoLine.useWorldSpace = false;
            _mayoLine.startWidth = 0.08f;
            _mayoLine.endWidth = 0.08f;
            _mayoLine.material = new Material(Shader.Find("Sprites/Default")); // Flat white
            
            // Zig Zag positions
            int points = 10;
            Vector3[] positions = new Vector3[points];
            float width = 0.7f;
            float length = 0.7f;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                float x = Mathf.Lerp(-width/2, width/2, t);
                float z = ((i % 2 == 0) ? -1 : 1) * length * 0.4f; // Zig zag deviation
                // Project onto sphere (y)
                float r = 0.82f; // Surface radius + offset
                // Simplification: Just arc it over top
                float y = Mathf.Sqrt(Mathf.Max(0, r*r - x*x - z*z)); 
                
                positions[i] = new Vector3(x, y, z);
            }
            _mayoLine.positionCount = points;
            _mayoLine.SetPositions(positions);
            _mayoLine.enabled = false;
        }
