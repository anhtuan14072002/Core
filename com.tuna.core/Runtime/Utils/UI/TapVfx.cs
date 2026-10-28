using UnityEngine;

namespace Crusher
{
    [DisallowMultipleComponent]
    public sealed class TapVfx : MonoBehaviour
    {
        [SerializeField] private RectTransform _effectArea;
        [SerializeField] private ParticleSystem[] _effects;
        private int _nextEffect;

        private void Awake()
        {
            for (int i = 0; i < _effects.Length; i++)
                _effects[i].gameObject.SetActive(false);
        }

        private void Update()
        {
            for (int i = 0; i < _effects.Length; i++)
            {
                var effect = _effects[i];
                if (effect.gameObject.activeSelf && !effect.IsAlive(true))
                    effect.gameObject.SetActive(false);
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began)
                        Play(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0))
            {
                Play(Input.mousePosition);
            }
        }

        private void Play(Vector2 screenPosition)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _effectArea, screenPosition, null, out var position))
                return;

            var effect = _effects[_nextEffect];
            _nextEffect = (_nextEffect + 1) % _effects.Length;
            effect.gameObject.SetActive(true);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.transform.localPosition = new Vector3(position.x, position.y, 0f);
            effect.transform.SetAsLastSibling();
            effect.Play(true);
        }
    }
}
