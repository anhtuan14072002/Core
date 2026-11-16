using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WZ
{
    [RequireComponent(typeof(Image))]
    public class UIButton : UIBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Scale")] [SerializeField] private Vector3 _scalePress = new Vector3(0.9f, 0.9f, 0.9f);
        [SerializeField] private Vector3 _scaleNormal = Vector3.one;
        [SerializeField] private float _duration = 0.1f;

        [Header("Color")] [SerializeField] private Color _colorNormal = Color.white;
        [SerializeField] private Color _colorDisable = Color.gray;

        [SerializeField] private Button _button;
        private Image _image;
        private RectTransform _rectTransform;
        private MotionHandle _scaleMotion;
        private MotionHandle _colorMotion;

#if UNITY_EDITOR
        protected override void Reset()
        {
            _button = GetComponent<Button>();
        }
#endif

        protected override void Awake()
        {
            base.Awake();
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateButtonState();
        }

        protected override void OnDisable()
        {
            _scaleMotion.TryCancel();
            _colorMotion.TryCancel();
            base.OnDisable();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;
            AnimateScale(_scalePress, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_button.interactable) return;
            AnimateScale(_scaleNormal, Ease.OutBack);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_button.interactable) return;
        }

        private void UpdateButtonState()
        {
            _colorMotion.TryCancel();
            var color = _button.interactable ? _colorNormal : _colorDisable;
            _colorMotion = _image.DoSetImageColor(false, color, _duration, Ease.Linear);
            AnimateScale(_scaleNormal, Ease.Linear);
        }

        private void AnimateScale(Vector3 scale, Ease ease)
        {
            _scaleMotion.TryCancel();
            _scaleMotion = _rectTransform.DoScaleRect(false, scale, _duration, ease);
        }

        public void RefreshButtonState()
        {
            UpdateButtonState();
        }
    }
}
