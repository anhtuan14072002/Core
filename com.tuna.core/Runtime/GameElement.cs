using UnityEngine;

namespace Core
{
    public class GameElement : MonoBehaviour
    {
        protected bool HasSetup { get; private set; }

        private void OnEnable()
        {
            if (!HasSetup)
            {
                HasSetup = true;
                Global.Add(this);
            }
            Enable();
        }

        private void OnDisable()
        {
            if (HasSetup)
            {
                HasSetup = false;
                Global.Remove(this);
            }
            Disable();
        }

        protected virtual void Enable(){}
        protected virtual void Disable(){}
    }
}