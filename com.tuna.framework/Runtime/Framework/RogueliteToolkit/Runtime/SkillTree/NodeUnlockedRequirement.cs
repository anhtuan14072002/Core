using System;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public sealed class NodeUnlockedRequirement : SkillRequirement
    {
        [SerializeField] private string _nodeId;

        public string NodeId => _nodeId;

        public NodeUnlockedRequirement() { }

        public NodeUnlockedRequirement(string nodeId)
        {
            _nodeId = nodeId?.Trim();
        }

        public override bool IsMet(
            SkillTreeState state, GameplayEffectContext context) =>
            state != null && state.IsPurchased(_nodeId);
    }
}
