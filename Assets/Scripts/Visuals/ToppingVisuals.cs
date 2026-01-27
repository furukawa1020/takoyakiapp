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
        
        public void AddAonori()
        {
            _aonoriFX.Emit(50);
        }
    }
}
