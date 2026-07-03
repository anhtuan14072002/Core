using UnityEngine;
using UnityEngine.EventSystems;

namespace Core
{
    [DefaultExecutionOrder(-999)]
    public class Initializer : MonoBehaviour
    {
        [SerializeField] EventSystem eventSystem;
        
        private static Initializer initializer;
        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }

        public void Init()
        {
            if (initializer != null) return;

            initializer = this;
            
            GameObject = gameObject;
            Transform = transform;
            
            DontDestroyOnLoad(gameObject);
        }

        public void InitModules()
        {
        }

        public void InitSDKs()
        {
        }
    }
}