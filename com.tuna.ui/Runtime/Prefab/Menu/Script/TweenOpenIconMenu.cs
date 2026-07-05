using PrimeTween;
using UnityEngine;

namespace Core
{
    public class TweenOpenIconMenu : MonoBehaviour
    {
        private Tween _focusTween;

        private void OnEnable()
        {
            if (_focusTween.isAlive)
            {
                _focusTween.Stop();
            }

            gameObject.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            _focusTween = Tween.Scale(gameObject.transform, new Vector3(1.25f, 1.25f, 1.25f), 0.25f, Ease.OutBack);
        }
    }
}
