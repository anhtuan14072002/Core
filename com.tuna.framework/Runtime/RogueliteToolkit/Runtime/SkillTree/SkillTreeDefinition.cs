using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    [CreateAssetMenu(fileName = "SkillTree", menuName = "Roguelite Toolkit/Skill Tree Definition")]
    public sealed class SkillTreeDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_smallIconSize")] private Vector2 _smallNodeSize = new(160f, 80f);
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_largeIconSize")] private Vector2 _largeNodeSize = new(240f, 120f);
        [SerializeField] private SkillNodeDefinition[] _nodes =
            Array.Empty<SkillNodeDefinition>();
        [SerializeField] private SkillConnectionDefinition[] _connections =
            Array.Empty<SkillConnectionDefinition>();

        private Dictionary<string, SkillNodeDefinition> _lookup;

        public string Id => _id;
        public Vector2 SmallNodeSize => _smallNodeSize;
        public Vector2 LargeNodeSize => _largeNodeSize;
        public Vector2 GetNodeDimensions(SkillNodeSize size, float scale = 1f) =>
            (size == SkillNodeSize.Large ? LargeNodeSize : SmallNodeSize) * Mathf.Max(0.01f, scale);
        public IReadOnlyList<SkillNodeDefinition> Nodes =>
            _nodes ?? Array.Empty<SkillNodeDefinition>();
        public IReadOnlyList<SkillConnectionDefinition> Connections =>
            _connections ?? Array.Empty<SkillConnectionDefinition>();
        public bool HasValidId => !string.IsNullOrWhiteSpace(_id);

        public bool TryGetNode(string nodeId, out SkillNodeDefinition node)
        {
            EnsureLookup();
            return _lookup.TryGetValue(nodeId ?? string.Empty, out node);
        }

        public void GetIncomingConnections(
            string nodeId, List<SkillConnectionDefinition> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            for (int i = 0; i < Connections.Count; i++)
            {
                SkillConnectionDefinition connection = Connections[i];
                if (connection != null &&
                    string.Equals(connection.ToNodeId, nodeId, StringComparison.Ordinal))
                    output.Add(connection);
            }
        }

        public void Configure(
            string id,
            SkillNodeDefinition[] nodes,
            SkillConnectionDefinition[] connections)
        {
            _id = id?.Trim();
            _nodes = nodes ?? Array.Empty<SkillNodeDefinition>();
            _connections = connections ?? Array.Empty<SkillConnectionDefinition>();
            _lookup = null;
        }

        private void EnsureLookup()
        {
            // Node IDs may be edited on referenced assets without validating this tree.
            _lookup = new Dictionary<string, SkillNodeDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < Nodes.Count; i++)
            {
                SkillNodeDefinition node = Nodes[i];
                if (node == null || !node.HasValidId)
                    continue;
                if (!_lookup.TryAdd(node.Id, node))
                    Debug.LogError($"Duplicate skill node ID '{node.Id}' in '{name}'.", this);
            }
        }

        private void OnValidate()
        {
            _id = _id?.Trim();
            _lookup = null;
        }
    }
}
