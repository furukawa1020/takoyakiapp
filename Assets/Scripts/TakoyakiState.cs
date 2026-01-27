using UnityEngine;

namespace TakoyakiPhysics
{
    public abstract class TakoyakiState
    {
        protected TakoyakiController Controller;

        public TakoyakiState(TakoyakiController controller)
        {
            Controller = controller;
        }

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void UpdateState() { }
    }
}
