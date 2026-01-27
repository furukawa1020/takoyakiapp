using UnityEngine;
using UnityEngine.EventSystems;

namespace TakoyakiPhysics.UI
{
    public class VirtualArrowButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Vector2 direction; // e.g. (1, 0) for Right

        public void OnPointerDown(PointerEventData eventData)
        {
            if (TakoyakiPhysics.InputManager.Instance != null)
            {
                // We add this direction to the virtual input
                // But InputManager expects a total vector. 
                // Let's modify InputManager to accumulate or just set raw direction here.
                // Simple approach: InputManager has a method AddVirtualInput(dir).
                // Actually, let's keep it simple: While held, apply force?
                // Better: InputManager stores state.
                
                // Let's use a simpler pattern:
                // InputManager.Instance.SetVirtualInput(direction);
                // But we need to handle multiple buttons... 
                // Let's assume one button pressed at a time for tilt?
                
                // Correction: We need to Add to the current input state.
                // But InputManager just exposes a Setter.
                // Let's assume we update a static "pressed" state in this class?
                // Or just hack it: InputManager's SetVirtualInput takes the vector.
                
                 TakoyakiPhysics.InputManager.Instance.SetVirtualInput(direction);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
             if (TakoyakiPhysics.InputManager.Instance != null)
             {
                 // Reset only if we match the direction? 
                 // Simple reset for now (single touch assumed)
                 TakoyakiPhysics.InputManager.Instance.SetVirtualInput(Vector2.zero);
             }
        }
    }
}
