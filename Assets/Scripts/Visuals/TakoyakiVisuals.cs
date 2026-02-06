using UnityEngine;

namespace TakoyakiPhysics.Visuals
{
    [RequireComponent(typeof(Renderer))]
    public class TakoyakiVisuals : MonoBehaviour
    {
        [Header("Shader Property Names")]
        [SerializeField] private string cookPropName = "_CookLevel";
        [SerializeField] private string batterPropName = "_BatterAmount";
        
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private TakoyakiController _controller;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _controller = GetComponent<TakoyakiController>();
        }

        private void Update()
        {
            // Lazy Init for safety
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_controller == null) _controller = GetComponent<TakoyakiController>();
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
            
            if (_controller == null || _renderer == null) return;

            // Update shader properties based on controller state
            _renderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetFloat(cookPropName, _controller.CookLevel);
            _propBlock.SetFloat(batterPropName, _controller.BatterAmount);
            
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}
