using PrimeTween;
using UnityEngine;

namespace Core
{
    public class Popup : MonoBehaviour
    {
        private void OnEnable()
        {
            gameObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            TweenPopupOpen();
        }

        private void OnDisable()
        {
            gameObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        }
        public void OpenPopup()
        {
            gameObject.SetActive(true);
        }
        
        public void ClosePopup()
        {
            TweenPopupClose();
        }

        private void TweenPopupOpen()
        {
            Tween.Scale(gameObject.transform, Vector3.one, 0.25f, Ease.OutBack);
        }

        private void TweenPopupClose()
        {
            Tween.Scale(gameObject.transform, Vector3.zero, 0.25f, Ease.InBack)
                .OnComplete(() => { gameObject.SetActive(false); });
        }
    }
}