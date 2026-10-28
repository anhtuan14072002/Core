using UnityEngine.Events;
using System;

namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Toggle))]
    public class ToggleGraphicSwap : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _on;
        [SerializeField] private Sprite _off;
        [SerializeField] private UnityEvent _onEvent;
        [SerializeField] private UnityEvent _offEvent;
        [SerializeField] private ToggleType _toggleType;

        public static event Action<ToggleType, bool> OnToggleGroupChanged;

        private bool _isUpdatingFromGroup = false;

        private void Reset()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void Awake()
        {
            _toggle.onValueChanged.AddListener(OnTargetToggleValueChanged);
        }

        private void OnEnable()
        {
            LoadToggleState();
            OnToggleGroupChanged += OnOtherToggleChanged;
        }

        private void OnDisable()
        {
            OnToggleGroupChanged -= OnOtherToggleChanged;
        }

        private void LoadToggleState()
        {
            string key = $"Toggle_{_toggleType}";
            bool savedState = PlayerPrefs.GetInt(key, _toggle.isOn ? 1 : 0) == 1;

            _isUpdatingFromGroup = true;
            _toggle.isOn = savedState;
            SetImage(savedState);
            _isUpdatingFromGroup = false;
        }
        private void SaveToggleState(bool state)
        {
            string key = $"Toggle_{_toggleType}";
            PlayerPrefs.SetInt(key, state ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnTargetToggleValueChanged(bool on)
        {
            if (_isUpdatingFromGroup) return;
            SetImage(on);
            SaveToggleState(on);
            if (on) _onEvent.Invoke();
            else _offEvent.Invoke();
            OnToggleGroupChanged?.Invoke(_toggleType, on);
        }

        private void OnOtherToggleChanged(ToggleType toggleType, bool value)
        {
            if (toggleType == _toggleType && !_isUpdatingFromGroup)
            {
                _isUpdatingFromGroup = true;
                _toggle.isOn = value;
                SetImage(value);
                _isUpdatingFromGroup = false;
            }
        }

        private void SetImage(bool on)
        {
            _image.sprite = on ? _off : _on;
        }
        public enum ToggleType
        {
            Sound,
            Music,
            BMG,
            Vibration,
        }
    }
}
