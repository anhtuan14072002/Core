using UnityEngine;

namespace Core
{
    public class InternetServices : MonoBehaviour
    {
        private void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}