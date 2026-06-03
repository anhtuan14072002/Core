using UnityEngine;

namespace Core
{
    public class FitCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _mapWidth;
        [SerializeField] private float _mapHeight;
        private void Start()
        {
            FitCameraForAllDevices(_camera, _mapWidth);
        }

        void FitCameraToWidth(Camera cam, float mapWidth)
        {
            float aspect = (float)Screen.width / Screen.height;
            cam.orthographicSize = (mapWidth / 2f) / aspect;
        }
        void FitCameraToMap(Camera cam, float mapWidth, float mapHeight)
        {
            float aspect = (float)Screen.width / Screen.height;

            float sizeByHeight = mapHeight / 2f;
            float sizeByWidth  = (mapWidth / 2f) / aspect;

            cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
        }
        void FitCameraForAllDevices(Camera cam, float mapWidth)
        {
            float aspect = (float)Screen.width / Screen.height;

            const float designAspect = 9f / 16f;
            float usedAspect = Mathf.Min(aspect, designAspect);
            cam.orthographic = true;
            cam.orthographicSize = (mapWidth / 2f) / usedAspect;
        }
    }
}