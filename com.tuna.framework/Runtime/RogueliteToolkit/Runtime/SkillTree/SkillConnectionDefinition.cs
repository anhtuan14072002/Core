using System;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public sealed class SkillConnectionDefinition
    {
        [SerializeField] private string _fromNodeId;
        [SerializeField] private string _toNodeId;
        [SerializeReference] private SkillRequirement _requirement;

        public string FromNodeId => _fromNodeId;
        public string ToNodeId => _toNodeId;
        public SkillRequirement Requirement => _requirement;

        public SkillConnectionDefinition() { }

        public SkillConnectionDefinition(
            string fromNodeId, string toNodeId, SkillRequirement requirement)
        {
            _fromNodeId = fromNodeId?.Trim();
            _toNodeId = toNodeId?.Trim();
            _requirement = requirement;
        }
    }
}
