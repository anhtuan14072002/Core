using UnityEngine;

namespace Core
{
    public class FPSDisplay : MonoBehaviour
    {
        [SerializeField] private float updateInterval = 0.5f;

        private int frameCount = 0;
        private float timer = 0f;
        private float fps = 0f;

        private GUIStyle style;

        private void Awake()
        {
            style = new GUIStyle();
            style.fontSize = 32;
            style.normal.textColor = Color.yellow;
            style.fontStyle = FontStyle.Bold;
        }

        private void Update()
        {
            frameCount++;
            timer += Time.unscaledDeltaTime;

            if (timer >= updateInterval)
            {
                fps = frameCount / timer;
                frameCount = 0;
                timer = 0;
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 200, 40), $"{fps:F1} FPS", style);
        }
    }
}