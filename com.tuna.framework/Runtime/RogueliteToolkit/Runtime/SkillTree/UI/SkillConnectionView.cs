using RogueliteToolkit.Effects;
using UnityEngine;
using UnityEngine.UI;

namespace RogueliteToolkit.SkillTree.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class SkillConnectionView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Color _lockedColor = new(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color _availableColor = new(0.35f, 0.65f, 1f, 1f);
        [SerializeField] private Color _activeColor = new(0.35f, 0.8f, 0.45f, 1f);

        private SkillConnectionDefinition _definition;
        private SkillTreeState _state;
        private GameplayEffectContext _context;
        private RectTransform _from;
        private RectTransform _to;

        public SkillConnectionVisualState VisualState { get; private set; }

        public void Bind(
            SkillConnectionDefinition definition,
            SkillTreeState state,
            GameplayEffectContext context,
            RectTransform from,
            RectTransform to)
        {
            _definition = definition;
            _state = state;
            _context = context;
            _from = from;
            _to = to;
            if (_image == null)
                _image = GetComponent<Image>();
            _image.raycastTarget = false;
            SkillConnectionLayout.Apply((RectTransform)transform, from, to);
            Refresh();
        }

        public void Refresh()
        {
            if (_definition == null || _state == null)
                return;
            bool active = _definition.Requirement != null &&
                          _definition.Requirement.IsMet(_state, _context);
            bool targetAvailable = _state.CanPurchase(
                _definition.ToNodeId, _context);
            VisualState = active
                ? SkillConnectionVisualState.Active
                : targetAvailable
                    ? SkillConnectionVisualState.Available
                    : SkillConnectionVisualState.Locked;
            if (_image != null)
                _image.color = VisualState switch
                {
                    SkillConnectionVisualState.Active => _activeColor,
                    SkillConnectionVisualState.Available => _availableColor,
                    _ => _lockedColor
                };
            SkillConnectionLayout.Apply((RectTransform)transform, _from, _to);
        }
    }
}
