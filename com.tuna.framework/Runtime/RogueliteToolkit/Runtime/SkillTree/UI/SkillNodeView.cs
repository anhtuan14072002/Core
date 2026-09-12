using RogueliteToolkit.Effects;
using UnityEngine;
using UnityEngine.UI;

namespace RogueliteToolkit.SkillTree.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SkillNodeView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _displayName;
        [SerializeField] private Text _level;
        [Header("States")]
        [SerializeField] private Color _lockedColor = new(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color _availableColor = new(0.35f, 0.65f, 1f, 1f);
        [SerializeField] private Color _purchasedColor = new(0.35f, 0.8f, 0.45f, 1f);
        [SerializeField] private Color _maxedColor = new(1f, 0.75f, 0.2f, 1f);

        private SkillNodeDefinition _definition;
        private SkillTreeState _state;
        private GameplayEffectContext _context;

        public SkillNodeDefinition Definition => _definition;
        public RectTransform RectTransform => (RectTransform)transform;
        public SkillNodeVisualState VisualState { get; private set; }

        public void Bind(
            SkillNodeDefinition definition,
            SkillTreeState state,
            GameplayEffectContext context)
        {
            _definition = definition;
            _state = state;
            _context = context;
            Vector2 nodeDimensions = state.Definition.GetNodeDimensions(definition.NodeSize, definition.NodeScale);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, nodeDimensions.x);
            RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, nodeDimensions.y);
            if (_button != null)
            {
                _button.onClick.RemoveListener(Purchase);
                _button.onClick.AddListener(Purchase);
            }
            if (_displayName != null)
                _displayName.text = definition.DisplayName;
            if (_icon != null)
            {
                _icon.sprite = definition.Icon;
                _icon.enabled = definition.Icon != null;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_definition == null || _state == null)
                return;

            int level = _state.GetLevel(_definition);
            bool canPurchase = _state.CanPurchase(_definition, _context);
            if (level >= _definition.MaxLevel)
                VisualState = SkillNodeVisualState.Maxed;
            else if (level > 0)
                VisualState = SkillNodeVisualState.Purchased;
            else if (canPurchase)
                VisualState = SkillNodeVisualState.Available;
            else
                VisualState = SkillNodeVisualState.Locked;

            if (_background != null)
                _background.color = GetColor(VisualState);
            if (_level != null)
                _level.text = _definition.GetProgressLabel(level);
            if (_button != null)
                _button.interactable = canPurchase;
        }

        private void Purchase()
        {
            if (_definition != null && _state != null && _context != null)
                _state.Purchase(_definition, _context);
        }

        private Color GetColor(SkillNodeVisualState state)
        {
            return state switch
            {
                SkillNodeVisualState.Available => _availableColor,
                SkillNodeVisualState.Purchased => _purchasedColor,
                SkillNodeVisualState.Maxed => _maxedColor,
                _ => _lockedColor
            };
        }
    }
}
