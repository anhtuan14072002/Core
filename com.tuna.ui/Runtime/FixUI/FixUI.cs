using UnityEngine;
using System.Collections;

namespace Core
{
    public class FixUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _groupBtnBottom;
        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();

            if (Camera.main.orthographicSize > 7.5f)
            {
                var pos = _groupBtnBottom.anchoredPosition;
                pos.y += 150f;
                _groupBtnBottom.anchoredPosition = pos;
            }
        }
    }
}