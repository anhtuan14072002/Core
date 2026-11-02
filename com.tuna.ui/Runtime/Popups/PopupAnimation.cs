using System;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Tuna.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform _contentRoot;

        [Header("Animation")]
        [SerializeField, Min(0.01f)]
        private float _duration = 0.2f;

        [SerializeField, Range(0.5f, 1f)]
        private float _closedScale = 0.86f;

        [SerializeField] private Ease _openEase = Ease.OutBack;
        [SerializeField] private Ease _closeEase = Ease.InCubic;

        [Header("Behaviour")]
        [SerializeField] private bool _animateOnEnable = true;
        [SerializeField] private bool _bringToFront = true;

        private enum PopupState
        {
            Closed,
            Opening,
            Opened,
            Closing
        }

        private CanvasGroup _canvasGroup;
        private Vector3 _originalScale;
        private float _originalAlpha;

        private MotionHandle _fadeMotion;
        private MotionHandle _scaleMotion;

        private PopupState _state = PopupState.Closed;
        private Action _onClosed;

        private bool _initialized;

        public bool IsOpen => _state == PopupState.Opening || _state == PopupState.Opened;

        public bool IsAnimating => _state == PopupState.Opening || _state == PopupState.Closing;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (_initialized) return;

            _canvasGroup = GetComponent<CanvasGroup>();
            _originalAlpha = _canvasGroup.alpha;

            if (_contentRoot == transform) _contentRoot = null;
            if (_contentRoot != null) _originalScale = _contentRoot.localScale;

            _initialized = true;
        }

        private void OnEnable()
        {
            Initialize();

            if (_animateOnEnable) Open();
            else ApplyOpenedState();
        }

        private void OnDisable()
        {
            StopMotion();

            _onClosed = null;
            _state = PopupState.Closed;

            if (!_initialized) return;

            _canvasGroup.alpha = _originalAlpha;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (_contentRoot != null) _contentRoot.localScale = _originalScale;
        }

        public void Open()
        {
            Initialize();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                if (!_animateOnEnable) PlayOpen();
                return;
            }

            if (!isActiveAndEnabled) return;
            if (IsOpen) return;
            PlayOpen();
        }

        private void PlayOpen()
        {
            bool reversing = _state == PopupState.Closing;
            StopMotion();

            _state = PopupState.Opening;
            _onClosed = null;

            if (_bringToFront) transform.SetAsLastSibling();

            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (!reversing)
            {
                _canvasGroup.alpha = 0f;
                if (_contentRoot != null)
                    _contentRoot.localScale = _originalScale * _closedScale;
            }

            Animate(_originalAlpha, _originalScale, _openEase, false);
        }

        public void Close() => Close(null);

        public void Close(Action onClosed)
        {
            if (!isActiveAndEnabled) return;
            if (_state == PopupState.Closing || _state == PopupState.Closed) return;

            StopMotion();

            _state = PopupState.Closing;
            _onClosed = onClosed;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            Animate(0f, _originalScale * _closedScale, _closeEase, true);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void CloseImmediate()
        {
            if (!gameObject.activeSelf) return;
            StopMotion();
            var callback = _onClosed;
            _onClosed = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void Animate(float targetAlpha, Vector3 targetScale, Ease ease, bool closing)
        {
            var fade = LMotion.Create(_canvasGroup.alpha, targetAlpha, _duration)
                .WithEase(closing ? Ease.InCubic : Ease.OutCubic)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);

            if (closing)
                fade = fade.WithOnComplete(CompleteClose);
            else
                fade = fade.WithOnComplete(CompleteOpen);

            _fadeMotion = fade.BindToAlpha(_canvasGroup);

            if (_contentRoot == null)
                return;

            _scaleMotion = LMotion.Create(_contentRoot.localScale, argetScale, _duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScale(_contentRoot);
        }

        private void CompleteOpen()
        {
            if (_state != PopupState.Opening) return;
            _state = PopupState.Opened;
        }

        private void CompleteClose()
        {
            if (_state != PopupState.Closing) return;
            var callback = _onClosed;
            _onClosed = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }

        private void ApplyOpenedState()
        {
            StopMotion();

            _state = PopupState.Opened;
            _onClosed = null;

            _canvasGroup.alpha = _originalAlpha;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (_contentRoot != null)
                _contentRoot.localScale = _originalScale;

            if (_bringToFront)
                transform.SetAsLastSibling();
        }

        private void StopMotion()
        {
            _fadeMotion.TryCancel();
            _scaleMotion.TryCancel();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_contentRoot == transform)
            {
                _contentRoot = null;

                Debug.LogWarning(
                    "[PopupAnimation] Content Root cannot be Popup root.",
                    this);
            }
        }
#endif
    }
}
