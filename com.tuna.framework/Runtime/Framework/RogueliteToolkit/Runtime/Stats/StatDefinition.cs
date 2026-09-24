using UnityEngine;

namespace RogueliteToolkit.Stats
{
    [CreateAssetMenu(fileName = "Stat", menuName = "Roguelite Toolkit/Stat Definition")]
    public sealed class StatDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private float _defaultBaseValue;
        [SerializeField] private bool _hasMinimum;
        [SerializeField] private float _minimum;
        [SerializeField] private bool _hasMaximum;
        [SerializeField] private float _maximum = 100f;

        public string Id => _id;
        public float DefaultBaseValue => _defaultBaseValue;
        public bool HasMinimum => _hasMinimum;
        public float Minimum => _minimum;
        public bool HasMaximum => _hasMaximum;
        public float Maximum => _maximum;
        public bool HasValidId => !string.IsNullOrWhiteSpace(_id);

        public float Clamp(float value)
        {
            if (_hasMinimum)
                value = Mathf.Max(_minimum, value);
            if (_hasMaximum)
                value = Mathf.Min(_maximum, value);
            return value;
        }

        private void OnValidate()
        {
            _id = _id?.Trim();
            if (_hasMinimum && _hasMaximum && _maximum < _minimum)
                _maximum = _minimum;
        }
    }
}
