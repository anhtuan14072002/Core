using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public sealed class SkillNodeDefinition
    {
        [SerializeField] private SkillNodeDataSO _data;
        // Legacy fields retain old asset compatibility until migration succeeds.
        [SerializeField, HideInInspector] private string _id;
        [SerializeField, HideInInspector] private string _displayName;
        [SerializeField, HideInInspector] private string _description;
        [SerializeField, HideInInspector] private Sprite _icon;
        [SerializeField, HideInInspector] private bool _instantFreeUpgrade;
        [SerializeField, HideInInspector] private SkillNodeColor _nodeColor = SkillNodeColor.Blue;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_iconSize")] private SkillNodeSize _nodeSize;
        [SerializeField, Min(0.01f), UnityEngine.Serialization.FormerlySerializedAs("_iconScale")] private float _nodeScale = 1f;
        [SerializeField, HideInInspector] private Color _color = UnityEngine.Color.white;
        [SerializeField] private Vector2 _position;
        [SerializeField, HideInInspector] private int _maxLevel = 1;
        [SerializeField, Range(1, 4)] private int _phaseCount = 1;
        [SerializeField] private int[] _phaseEndLevels = Array.Empty<int>();
        [SerializeField, HideInInspector] private double[] _costs = Array.Empty<double>();
        [SerializeField] private SkillResourceCost[] _additionalCosts = Array.Empty<SkillResourceCost>();
        [SerializeField] private SkillTreeResourcesType _costResourceType;
        [SerializeField, HideInInspector] private SkillTreeResourcesType[] _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
        [SerializeField] private RequirementMode _incomingRequirementMode;
        [SerializeField, HideInInspector] private StatGameplayEffect[] _statEffects =
            Array.Empty<StatGameplayEffect>();
        [SerializeReference, HideInInspector] private GameplayEffect[] _effects =
            Array.Empty<GameplayEffect>();

        [SerializeField] private string _resourceId;
        public string ResourceId => _data != null ? _data.ResourceId : _resourceId;
        public void SetNodeSize(SkillNodeSize nodeSize) => _nodeSize = nodeSize;
        public void SetNodeScale(float scale) => _nodeScale = Mathf.Max(0.01f, scale);
        public void SetResourceId(string resourceId) => _resourceId = resourceId;

        public SkillNodeDataSO Data => _data;
        public bool InstantFreeUpgrade => _data != null ? _data.InstantFreeUpgrade : _instantFreeUpgrade;
        public SkillNodeColor NodeColor => _data != null ? _data.NodeColor : _nodeColor;
        public string Id => _data != null ? _data.Id : _id;
        public string DisplayName => _data != null ? _data.DisplayName : string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public string Description => _data != null ? _data.Description : _description;
        public Sprite Icon => _data != null ? _data.Icon : _icon;
        public SkillNodeSize NodeSize => _data != null ? _data.NodeSize : _nodeSize;
        public float NodeScale => _data != null ? _data.NodeScale : Mathf.Max(0.01f, _nodeScale);
        public Color Color => _data != null ? _data.Color : _color;
        public Vector2 Position => _position;
        public int LevelsPerPhase => _data != null ? _data.LevelsPerPhase : Mathf.Max(1, _maxLevel);
        public IReadOnlyList<int> PhaseEndLevels => _data != null ? _data.PhaseEndLevels : _phaseEndLevels ?? Array.Empty<int>();
        public int PhaseCount => _data != null ? _data.PhaseCount
            : PhaseEndLevels.Count > 0 ? PhaseEndLevels.Count : Mathf.Clamp(_phaseCount, 1, 4);
        public int MaxLevel => PhaseEndLevels.Count > 0
            ? PhaseEndLevels[PhaseEndLevels.Count - 1] : LevelsPerPhase * PhaseCount;
        public IReadOnlyList<SkillResourceCost> AdditionalCosts => _data != null ? _data.AdditionalCosts : _additionalCosts;
        public IReadOnlyList<double> Costs => _data != null ? _data.Costs : _costs ?? Array.Empty<double>();
        public SkillTreeResourcesType CostResourceType => _data != null ? _data.CostResourceType : 
            _costResourceTypes != null && _costResourceTypes.Length > 0 ? _costResourceTypes[0] : _costResourceType;
        public RequirementMode IncomingRequirementMode => _incomingRequirementMode;
        public IReadOnlyList<StatGameplayEffect> StatEffects =>
            _data != null ? _data.StatEffects : _statEffects ?? Array.Empty<StatGameplayEffect>();
        public IReadOnlyList<GameplayEffect> Effects =>
            _data != null ? _data.Effects : _effects ?? Array.Empty<GameplayEffect>();
        public bool HasValidId => !string.IsNullOrWhiteSpace(Id);

        // Phase indices are zero-based; stars are earned at each end level. End levels are promotion thresholds,
        // except the final end, which is the inclusive maximum level.
        public int GetPhaseStartLevel(int phase) => phase == 0 ? 0 : GetPhaseEndLevel(phase - 1);

        public int GetPhaseEndLevel(int phase) => PhaseEndLevels.Count > 0
            ? PhaseEndLevels[Mathf.Clamp(phase, 0, PhaseCount - 1)] : (phase + 1) * LevelsPerPhase;

        public int GetPhaseLevelCount(int phase) => GetPhaseEndLevel(phase) - GetPhaseStartLevel(phase);

        public int GetPhase(int totalLevel)
        {
            int level = Mathf.Clamp(totalLevel, 0, MaxLevel);
            for (int phase = 0; phase < PhaseCount - 1; phase++)
                if (level < GetPhaseEndLevel(phase)) return phase;
            return PhaseCount - 1;
        }

        public int GetLevelInPhase(int totalLevel) =>
            Mathf.Clamp(totalLevel, 0, MaxLevel) - GetPhaseStartLevel(GetPhase(totalLevel));

        public string GetProgressLabel(int totalLevel) => PhaseCount == 1
            ? $"{Mathf.Clamp(totalLevel, 0, MaxLevel)}/{MaxLevel}"
            : $"{GetLevelInPhase(totalLevel)}/{GetPhaseLevelCount(GetPhase(totalLevel))} | {GetPhase(totalLevel) + (totalLevel >= MaxLevel ? 1 : 0)} sao";

        public void SetPhaseEndLevels(IReadOnlyList<int> ends)
        {
            _phaseEndLevels = new int[ends?.Count ?? 0];
            for (int i = 0; i < _phaseEndLevels.Length; i++)
                _phaseEndLevels[i] = Mathf.Max(i == 0 ? 1 : _phaseEndLevels[i - 1] + 1, ends[i]);
        }

        public string GetDescriptionForLevel(int level)
        {
            string description = Description;
            if (string.IsNullOrEmpty(description)) return description;
            int displayedLevel = Mathf.Clamp(level, 1, MaxLevel);
            for (int i = 0; i < StatEffects.Count; i++)
            {
                StatGameplayEffect effect = StatEffects[i];
                if (effect == null) continue;
                float value = effect.GetValueAtLevel(displayedLevel) - effect.GetValueAtLevel(displayedLevel - 1);
                string formatted = (value > 0f ? "+ " : value < 0f ? "- " : string.Empty) +
                    Mathf.Abs(value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                description = description.Replace("{" + i + "}", formatted);
                if (i == 0)
                    description = description.Replace("{%}", formatted + "%");
            }
            return description;
        }

        public void SetData(SkillNodeDataSO data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _data = data;
            _id = _displayName = _description = null;
            _icon = null;
            _nodeColor = SkillNodeColor.Blue;
            _color = UnityEngine.Color.white;
            _costs = Array.Empty<double>();
            _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
            _statEffects = Array.Empty<StatGameplayEffect>();
            _effects = Array.Empty<GameplayEffect>();
        }

        public int CostResourceCount => 1 + AdditionalCosts.Count;

        public void SetAdditionalCosts(IReadOnlyList<SkillResourceCost> costs)
        {
            _additionalCosts = new SkillResourceCost[costs.Count];
            for (int i = 0; i < costs.Count; i++) _additionalCosts[i] = costs[i];
        }

        public double GetCostForLevel(int level, int resourceIndex = 0)
        {
            if (level < 1 || level > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            if (InstantFreeUpgrade) return 0;
            if (resourceIndex > 0) return AdditionalCosts[resourceIndex - 1].GetCost(level);
            return level <= Costs.Count
                ? Math.Max(0, Costs[level - 1])
                : 0;
        }

        public SkillTreeResourcesType GetCostResourceTypeForLevel(int level, int resourceIndex = 0)
        {
            if (level < 1 || level > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            return resourceIndex == 0 ? CostResourceType : AdditionalCosts[resourceIndex - 1].Resource;
        }

        public void NotifyLevelChanged(
            GameplayEffectContext context, int previousLevel, int currentLevel)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!HasValidId)
                throw new InvalidOperationException("A skill node requires a stable ID.");

            EffectSource source = new("skill", Id);
            int effectIndex = 0;
                for (int i = 0; i < StatEffects.Count; i++, effectIndex++)
                    StatEffects[i]?.OnValueChanged(
                        context, source, effectIndex, previousLevel, currentLevel);
                for (int i = 0; i < Effects.Count; i++, effectIndex++)
                    Effects[i]?.OnValueChanged(
                        context, source, effectIndex, previousLevel, currentLevel);
        }

        public void Configure(string id, string displayName, string description, Sprite icon,
            Vector2 position, int maxLevel, int[] costs, RequirementMode incomingRequirementMode,
            StatGameplayEffect[] statEffects, GameplayEffect[] effects,
            SkillTreeResourcesType costResourceType = SkillTreeResourcesType.None, int phaseCount = 1,
            Color? color = null, bool instantFreeUpgrade = false, SkillNodeColor nodeColor = SkillNodeColor.Blue)
            => Configure(id, displayName, description, icon, position, maxLevel,
                costs == null ? null : Array.ConvertAll(costs, value => (double)value),
                incomingRequirementMode, statEffects, effects, costResourceType, phaseCount, color,
                instantFreeUpgrade, nodeColor);

        public void Configure(
            string id,
            string displayName,
            string description,
            Sprite icon,
            Vector2 position,
            int maxLevel,
            double[] costs,
            RequirementMode incomingRequirementMode,
            StatGameplayEffect[] statEffects,
            GameplayEffect[] effects,
            SkillTreeResourcesType costResourceType = SkillTreeResourcesType.None,
            int phaseCount = 1,
            Color? color = null,
            bool instantFreeUpgrade = false,
            SkillNodeColor nodeColor = SkillNodeColor.Blue)
        {
            _data = null;
            _additionalCosts = Array.Empty<SkillResourceCost>();
            _instantFreeUpgrade = instantFreeUpgrade;
            _nodeColor = nodeColor;
            _nodeSize = SkillNodeSize.Small;
            _phaseEndLevels = Array.Empty<int>();
            _costResourceType = costResourceType;
            _costResourceTypes = Array.Empty<SkillTreeResourcesType>();
            _id = id?.Trim();
            _displayName = displayName;
            _description = description;
            _icon = icon;
            _color = color ?? UnityEngine.Color.white;
            _position = position;
            _maxLevel = Mathf.Max(1, maxLevel);
            _phaseCount = Mathf.Clamp(phaseCount, 1, 4);
            _costs = costs != null ? (double[])costs.Clone() : Array.Empty<double>();
            for (int i = 0; i < _costs.Length; i++)
                _costs[i] = Math.Max(0, _costs[i]);
            _incomingRequirementMode = incomingRequirementMode;
            _statEffects = statEffects ?? Array.Empty<StatGameplayEffect>();
            _effects = effects ?? Array.Empty<GameplayEffect>();
        }
    }
}
