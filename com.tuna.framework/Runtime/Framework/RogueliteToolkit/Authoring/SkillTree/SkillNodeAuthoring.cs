using System;
using System.Collections.Generic;
using System.Linq;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree.Authoring
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SkillNodeAuthoring : MonoBehaviour
    {
        [SerializeField] private SkillNodeDataSO _data;
        [SerializeField, HideInInspector] private string _resourceId;
        public string ResourceId => _data != null ? _data.ResourceId : _resourceId;
        public SkillNodeDataSO Data => _data;
        public UnityEngine.Object DataTarget => _data != null ? _data : this;

        public void SetData(SkillNodeDataSO data)
        {
            if (data == null && _data != null)
            {
                _resourceId = ResourceId;
                _id = Id;
                _displayName = DisplayName;
                _description = Description;
                _icon = Icon;
                _color = Color;
                _maxLevel = LevelsPerPhase;
                _phaseCount = PhaseCount;
                _phaseEndLevels = PhaseEndLevels.ToArray();
                _nodeSize = NodeSize;
                _nodeScale = NodeScale;
                _instantFreeUpgrade = InstantFreeUpgrade;
                _nodeColor = NodeColor;
                _costResourceType = CostResourceType;
                _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
                _additionalCosts = AdditionalCosts.ToArray();
                _costs = CopyCosts();
                _statEffects = CopyStatEffects();
                _effects = CopyEffects();
            }

            _data = data;
        }

        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _instantFreeUpgrade;
        [SerializeField] private SkillNodeColor _nodeColor = SkillNodeColor.Blue;

        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_iconSize")]
        private SkillNodeSize _nodeSize;

        [SerializeField, Min(0.01f), UnityEngine.Serialization.FormerlySerializedAs("_iconScale")]
        private float _nodeScale = 1f;

        [SerializeField] private Color _color = UnityEngine.Color.white;

        [SerializeField, Min(1), InspectorName("Levels Per Phase")]
        private int _maxLevel = 1;

        [SerializeField, Range(1, 4)] private int _phaseCount = 1;
        [SerializeField] private int[] _phaseEndLevels = Array.Empty<int>();
        [SerializeField] private double[] _costs = Array.Empty<double>();
        [SerializeField] private SkillResourceCost[] _additionalCosts = Array.Empty<SkillResourceCost>();
        [SerializeField] private SkillTreeResourcesType _costResourceType;

        [SerializeField, HideInInspector]
        private SkillTreeResourcesType[] _costResourceTypes = Array.Empty<SkillTreeResourcesType>();

        [SerializeField] private RequirementMode _incomingRequirementMode;

        [SerializeField] private StatGameplayEffect[] _statEffects =
            Array.Empty<StatGameplayEffect>();

        [SerializeReference] private GameplayEffect[] _effects =
            Array.Empty<GameplayEffect>();

        [SerializeField, HideInInspector] private Vector2 _editorSize =
            new(320f, 240f);

        [SerializeField, HideInInspector] private bool _editorExpanded = true;

        public string Id => _data != null ? _data.Id : _id;
        public string DisplayName => _data != null ? _data.DisplayName : _displayName;
        public string Description => _data != null ? _data.Description : _description;
        public Sprite Icon => _data != null ? _data.Icon : _icon;
        public bool InstantFreeUpgrade => _data != null ? _data.InstantFreeUpgrade : _instantFreeUpgrade;
        public SkillNodeColor NodeColor => _data != null ? _data.NodeColor : _nodeColor;
        public SkillNodeSize NodeSize => _data != null ? _data.NodeSize : _nodeSize;
        public float NodeScale => _data != null ? _data.NodeScale : Mathf.Max(0.01f, _nodeScale);
        public Color Color => _data != null ? _data.Color : _color;
        public int LevelsPerPhase => _data != null ? _data.LevelsPerPhase : Mathf.Max(1, _maxLevel);

        public IReadOnlyList<int> PhaseEndLevels =>
            _data != null ? _data.PhaseEndLevels : _phaseEndLevels ?? Array.Empty<int>();

        public int PhaseCount => _data != null ? _data.PhaseCount
            : PhaseEndLevels.Count > 0 ? PhaseEndLevels.Count : Mathf.Clamp(_phaseCount, 1, 4);

        public int MaxLevel => PhaseEndLevels.Count > 0
            ? PhaseEndLevels[PhaseEndLevels.Count - 1]
            : LevelsPerPhase * PhaseCount;

        public IReadOnlyList<SkillResourceCost> AdditionalCosts =>
            _data != null ? _data.AdditionalCosts : _additionalCosts;

        public IReadOnlyList<double> Costs => _data != null ? _data.Costs : _costs ?? Array.Empty<double>();

        public SkillTreeResourcesType CostResourceType => _data != null ? _data.CostResourceType :
            _costResourceTypes != null && _costResourceTypes.Length > 0 ? _costResourceTypes[0] : _costResourceType;

        public RequirementMode IncomingRequirementMode => _incomingRequirementMode;

        public IReadOnlyList<StatGameplayEffect> StatEffects =>
            _data != null ? _data.StatEffects : _statEffects ?? Array.Empty<StatGameplayEffect>();

        public IReadOnlyList<GameplayEffect> Effects =>
            _data != null ? _data.Effects : _effects ?? Array.Empty<GameplayEffect>();

        public RectTransform RectTransform => (RectTransform)transform;

        public Vector2 EditorSize => _editorSize == new Vector2(440f, 480f)
            ? new Vector2(320f, 240f)
            : new(
                Mathf.Max(320f, _editorSize.x), Mathf.Max(240f, _editorSize.y));

        public bool EditorExpanded => _editorExpanded;

        public void SetEditorSize(Vector2 size)
        {
            _editorSize = new Vector2(
                Mathf.Max(320f, size.x), Mathf.Max(240f, size.y));
        }

        public void SetEditorExpanded(bool expanded) => _editorExpanded = expanded;

        public double[] CopyCosts() => Costs.ToArray();

        public StatGameplayEffect[] CopyStatEffects() => StatEffects.ToArray();

        public GameplayEffect[] CopyEffects() => Effects.ToArray();

        private void OnValidate()
        {
            _id = _id?.Trim();
            _maxLevel = Mathf.Max(1, _maxLevel);
            if (_data != null) return;
            _phaseCount = Mathf.Clamp(_phaseCount, 1, 4);
            for (int i = 0; i < (_phaseEndLevels?.Length ?? 0); i++)
                _phaseEndLevels[i] = Mathf.Max(i == 0 ? 1 : _phaseEndLevels[i - 1] + 1, _phaseEndLevels[i]);
            int previousCosts = _costs?.Length ?? 0;
            double lastCost = previousCosts > 0 ? _costs[previousCosts - 1] : 0;
            Array.Resize(ref _costs, MaxLevel);
            for (int i = previousCosts; i < _costs.Length; i++)
                _costs[i] = lastCost;
            _costResourceType = CostResourceType;
            _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
            if (_costs == null)
                return;
            for (int i = 0; i < _costs.Length; i++)
                _costs[i] = Math.Max(0, _costs[i]);
        }
    }
}