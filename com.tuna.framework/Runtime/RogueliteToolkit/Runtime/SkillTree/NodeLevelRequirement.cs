using System;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public sealed class NodeLevelRequirement : SkillRequirement
    {
        [SerializeField] private string _nodeId;
        [SerializeField, Min(1)] private int _minimumLevel = 1;

        public string NodeId => _nodeId;
        public int MinimumLevel => Mathf.Max(1, _minimumLevel);

        public NodeLevelRequirement() { }

        public NodeLevelRequirement(string nodeId, int minimumLevel)
        {
            _nodeId = nodeId?.Trim();
            _minimumLevel = Mathf.Max(1, minimumLevel);
        }

        public override bool IsMet(
            SkillTreeState state, GameplayEffectContext context) =>
            state != null && state.GetLevel(_nodeId) >= MinimumLevel;
    }
}
