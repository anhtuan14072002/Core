using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    public sealed class SkillTreeState
    {
        private readonly SkillTreeDefinition _definition;
        private readonly Dictionary<string, int> _levels =
            new(StringComparer.Ordinal);
        private readonly List<SkillConnectionDefinition> _incoming = new();

        public SkillTreeDefinition Definition => _definition;
        public event Action<SkillChange> SkillChanged;

        public SkillTreeState(SkillTreeDefinition definition)
        {
            _definition = definition != null
                ? definition
                : throw new ArgumentNullException(nameof(definition));
            if (!definition.HasValidId)
                throw new ArgumentException(
                    "A SkillTreeDefinition requires a stable ID.", nameof(definition));
        }

        public int GetLevel(SkillNodeDefinition node)
        {
            ValidateNode(node);
            return GetLevel(node.Id);
        }

        public int GetLevel(string nodeId) =>
            !string.IsNullOrWhiteSpace(nodeId) &&
            _levels.TryGetValue(nodeId, out int level)
                ? level
                : 0;

        public bool IsPurchased(SkillNodeDefinition node) => GetLevel(node) > 0;

        public bool IsPurchased(string nodeId) => GetLevel(nodeId) > 0;

        // Visibility previews one outgoing step from purchased nodes. It is separate
        // from affordability and All/Any/minimum-level purchase requirements.
        public bool IsVisible(SkillNodeDefinition node)
        {
            ValidateNode(node);
            if (IsPurchased(node.Id)) return true;
            _definition.GetIncomingConnections(node.Id, _incoming);
            if (_incoming.Count == 0) return true;
            for (int i = 0; i < _incoming.Count; i++)
                if (IsPurchased(_incoming[i].FromNodeId)) return true;
            return false;
        }

        public bool CanPurchase(
            SkillNodeDefinition node, GameplayEffectContext context)
        {
            ValidateNode(node);
            int currentLevel = GetLevel(node.Id);
            if (currentLevel >= node.MaxLevel)
                return false;
            if (!AreIncomingRequirementsMet(node, context))
                return false;

            int cost = node.GetCostForLevel(currentLevel + 1);
            if (cost <= 0)
                return true;
            return context != null &&
                   context.TryGet(out ISkillPointWallet wallet) &&
                   wallet.CanSpend(cost, node.GetCostResourceTypeForLevel(currentLevel + 1));
        }

        public bool CanPurchase(string nodeId, GameplayEffectContext context) =>
            _definition.TryGetNode(nodeId, out SkillNodeDefinition node) &&
            CanPurchase(node, context);

        public bool Purchase(
            SkillNodeDefinition node, GameplayEffectContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!CanPurchase(node, context))
                return false;

            int previousLevel = GetLevel(node.Id);
            int currentLevel = previousLevel + 1;
            int cost = node.GetCostForLevel(currentLevel);
            if (cost > 0)
            {
                ISkillPointWallet wallet = context.GetRequired<ISkillPointWallet>();
                if (!wallet.TrySpend(cost, node.GetCostResourceTypeForLevel(currentLevel)))
                    return false;
            }

            _levels[node.Id] = currentLevel;
            node.NotifyLevelChanged(context, previousLevel, currentLevel);
            SkillChanged?.Invoke(new SkillChange(node, previousLevel, currentLevel));
            return true;
        }

        public bool Purchase(string nodeId, GameplayEffectContext context) =>
            _definition.TryGetNode(nodeId, out SkillNodeDefinition node) &&
            Purchase(node, context);

        public int PurchaseMax(
            SkillNodeDefinition node, GameplayEffectContext context)
        {
            int purchasedLevels = 0;
            while (Purchase(node, context))
                purchasedLevels++;
            return purchasedLevels;
        }

        public void WriteSaveData(SkillTreeSaveData output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.TreeId = _definition.Id;
            output.Nodes ??= new List<SkillNodeLevelData>();
            output.Nodes.Clear();
            for (int i = 0; i < _definition.Nodes.Count; i++)
            {
                SkillNodeDefinition node = _definition.Nodes[i];
                int level = node != null ? GetLevel(node.Id) : 0;
                if (level > 0)
                    output.Nodes.Add(new SkillNodeLevelData(node.Id, level));
            }
        }

        public void Restore(
            SkillTreeSaveData data, GameplayEffectContext context)
        {
            if (_levels.Count != 0)
                throw new InvalidOperationException(
                    "Restore requires an empty SkillTreeState.");
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (data == null)
                return;
            if (!string.IsNullOrWhiteSpace(data.TreeId) &&
                !string.Equals(data.TreeId, _definition.Id, StringComparison.Ordinal))
                throw new ArgumentException(
                    $"Save data belongs to tree '{data.TreeId}', not '{_definition.Id}'.",
                    nameof(data));
            if (data.Nodes == null)
                return;

            HashSet<string> restored = new(StringComparer.Ordinal);
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                SkillNodeLevelData entry = data.Nodes[i];
                if (entry.Level <= 0 || !restored.Add(entry.NodeId) ||
                    !_definition.TryGetNode(entry.NodeId, out SkillNodeDefinition node))
                    continue;

                int level = Mathf.Clamp(entry.Level, 0, node.MaxLevel);
                _levels[node.Id] = level;
                node.NotifyLevelChanged(context, 0, level);
                SkillChanged?.Invoke(new SkillChange(node, 0, level));
            }
        }

        public bool AreIncomingRequirementsMet(
            SkillNodeDefinition node, GameplayEffectContext context)
        {
            ValidateNode(node);
            _definition.GetIncomingConnections(node.Id, _incoming);
            if (_incoming.Count == 0)
                return true;

            bool requireAll = node.IncomingRequirementMode == RequirementMode.All;
            for (int i = 0; i < _incoming.Count; i++)
            {
                SkillRequirement requirement = _incoming[i].Requirement;
                bool isMet = requirement != null && requirement.IsMet(this, context);
                if (requireAll && !isMet)
                    return false;
                if (!requireAll && isMet)
                    return true;
            }
            return requireAll;
        }

        private void ValidateNode(SkillNodeDefinition node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (!_definition.TryGetNode(node.Id, out SkillNodeDefinition ownedNode) ||
                !ReferenceEquals(node, ownedNode))
                throw new ArgumentException(
                    "The node does not belong to this skill tree.", nameof(node));
        }
    }
}
