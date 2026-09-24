using System;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public sealed class SkillResourceCost
    {
        [SerializeField] private SkillTreeResourcesType _resource;
        [SerializeField] private double[] _costs = Array.Empty<double>();

        public SkillTreeResourcesType Resource => _resource;
        public double GetCost(int level) => level > 0 && level <= _costs.Length ? _costs[level - 1] : 0;
    }
}
