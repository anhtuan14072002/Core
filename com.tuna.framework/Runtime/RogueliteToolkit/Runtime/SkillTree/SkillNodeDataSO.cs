using System;
using System.Collections.Generic;
using System.Linq;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    public enum SkillNodeSize { Small = 0, Large = 1 }

    [CreateAssetMenu(fileName = "SkillNode", menuName = "Roguelite Toolkit/Skill Node Data")]
    public sealed class SkillNodeDataSO : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_iconSize")] private SkillNodeSize _nodeSize;
        [SerializeField, Min(0.01f), UnityEngine.Serialization.FormerlySerializedAs("_iconScale")] private float _nodeScale = 1f;
        [SerializeField] private Color _color = UnityEngine.Color.white;
        [SerializeField, Min(1), InspectorName("Levels Per Phase")] private int _maxLevel = 1;
        [SerializeField, Range(1, 4)] private int _phaseCount = 1;
        [SerializeField] private int[] _phaseEndLevels = Array.Empty<int>();
        [SerializeField] private int[] _costs = Array.Empty<int>();
        [SerializeField] private SkillTreeResourcesType _costResourceType;
        [SerializeField, HideInInspector] private SkillTreeResourcesType[] _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
        [SerializeField] private StatGameplayEffect[] _statEffects = Array.Empty<StatGameplayEffect>();
        [SerializeReference] private GameplayEffect[] _effects = Array.Empty<GameplayEffect>();

        [SerializeField, HideInInspector]
        private string _resourceId;
        public string ResourceId => _resourceId;

        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public SkillNodeSize NodeSize => _nodeSize;
        public float NodeScale => Mathf.Max(0.01f, _nodeScale);
        public Color Color => _color;
        public int LevelsPerPhase => Mathf.Max(1, _maxLevel);
        public IReadOnlyList<int> PhaseEndLevels => _phaseEndLevels ?? Array.Empty<int>();
        public int PhaseCount => PhaseEndLevels.Count > 0 ? PhaseEndLevels.Count : Mathf.Clamp(_phaseCount, 1, 4);
        public int MaxLevel => PhaseEndLevels.Count > 0
            ? PhaseEndLevels[PhaseEndLevels.Count - 1] : LevelsPerPhase * PhaseCount;
        public IReadOnlyList<int> Costs => _costs ?? Array.Empty<int>();
        public SkillTreeResourcesType CostResourceType => 
            _costResourceTypes != null && _costResourceTypes.Length > 0 ? _costResourceTypes[0] : _costResourceType;
        public IReadOnlyList<StatGameplayEffect> StatEffects => _statEffects ?? Array.Empty<StatGameplayEffect>();
        public IReadOnlyList<GameplayEffect> Effects => _effects ?? Array.Empty<GameplayEffect>();

        public void CopyFrom(SkillNodeDefinition source)
        {
            _resourceId = source.ResourceId;
            _id = source.Id;
            _displayName = source.DisplayName;
            _description = source.Description;
            _icon = source.Icon;
            _nodeSize = source.NodeSize;
            _nodeScale = source.NodeScale;
            _color = source.Color;
            _maxLevel = source.LevelsPerPhase;
            _phaseCount = source.PhaseCount;
            _phaseEndLevels = source.PhaseEndLevels.ToArray();
            _costs = source.Costs.ToArray();
            _costResourceType = source.CostResourceType;
            _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
            _statEffects = source.StatEffects.ToArray();
            _effects = source.Effects.ToArray();
        }

        private void OnValidate()
        {
            _id = _id?.Trim();
            _maxLevel = Mathf.Max(1, _maxLevel);
            _phaseCount = Mathf.Clamp(_phaseCount, 1, 4);
            for (int i = 0; i < (_phaseEndLevels?.Length ?? 0); i++)
                _phaseEndLevels[i] = Mathf.Max(i == 0 ? 1 : _phaseEndLevels[i - 1] + 1, _phaseEndLevels[i]);
            int previousCosts = _costs?.Length ?? 0;
            int lastCost = previousCosts > 0 ? _costs[previousCosts - 1] : 0;
            Array.Resize(ref _costs, MaxLevel);
            for (int i = previousCosts; i < _costs.Length; i++)
                _costs[i] = lastCost;
            _costResourceType = CostResourceType;
            _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
            if (_costs != null)
                for (int i = 0; i < _costs.Length; i++)
                    _costs[i] = Mathf.Max(0, _costs[i]);
        }
    }
}
