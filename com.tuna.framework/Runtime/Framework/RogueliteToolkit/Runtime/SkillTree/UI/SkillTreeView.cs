using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.SkillTree.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillTreeView : MonoBehaviour
    {
        [SerializeField] private SkillTreeDefinition _definition;
        [SerializeField] private RectTransform _nodesRoot;
        [SerializeField] private RectTransform _connectionsRoot;
        [SerializeField] private SkillNodeView _nodePrefab;
        [SerializeField] private SkillConnectionView _connectionPrefab;

        private readonly Dictionary<string, SkillNodeView> _nodeViews =
            new(StringComparer.Ordinal);
        private readonly List<SkillConnectionView> _connectionViews = new();
        private SkillTreeState _state;
        private GameplayEffectContext _context;

        public SkillTreeState State => _state;

        public SkillTreeState CreateState(
            GameplayEffectContext context, SkillTreeSaveData saveData = null)
        {
            if (_definition == null)
                throw new InvalidOperationException("SkillTreeView requires a definition.");
            SkillTreeState state = new(_definition);
            if (saveData != null)
                state.Restore(saveData, context);
            Bind(state, context);
            return state;
        }

        public void Bind(SkillTreeState state, GameplayEffectContext context)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (_definition == null)
                _definition = state.Definition;
            if (_definition != state.Definition)
                throw new ArgumentException(
                    "The state belongs to a different SkillTreeDefinition.", nameof(state));

            Unsubscribe();
            _state = state;
            _context = context;
            _state.SkillChanged += OnSkillChanged;
            Rebuild();
        }

        public void Refresh()
        {
            foreach (SkillNodeView view in _nodeViews.Values)
                view.Refresh();
            for (int i = 0; i < _connectionViews.Count; i++)
                _connectionViews[i].Refresh();
        }

        private void Rebuild()
        {
            if (_nodePrefab == null || _connectionPrefab == null ||
                _nodesRoot == null || _connectionsRoot == null)
                throw new InvalidOperationException(
                    "SkillTreeView roots and prefabs must be assigned.");

            ClearChildren(_nodesRoot);
            ClearChildren(_connectionsRoot);
            _nodeViews.Clear();
            _connectionViews.Clear();

            for (int i = 0; i < _definition.Nodes.Count; i++)
            {
                SkillNodeDefinition node = _definition.Nodes[i];
                if (node == null || !node.HasValidId || _nodeViews.ContainsKey(node.Id))
                    continue;
                SkillNodeView view = Instantiate(_nodePrefab, _nodesRoot);
                view.name = node.Id;
                RectTransform rect = view.RectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = node.Position;
                view.Bind(node, _state, _context);
                _nodeViews.Add(node.Id, view);
            }

            for (int i = 0; i < _definition.Connections.Count; i++)
            {
                SkillConnectionDefinition connection = _definition.Connections[i];
                if (connection == null ||
                    !_nodeViews.TryGetValue(connection.FromNodeId, out SkillNodeView from) ||
                    !_nodeViews.TryGetValue(connection.ToNodeId, out SkillNodeView to))
                    continue;
                SkillConnectionView view = Instantiate(
                    _connectionPrefab, _connectionsRoot);
                view.name = $"{connection.FromNodeId} -> {connection.ToNodeId}";
                view.transform.SetAsFirstSibling();
                view.Bind(connection, _state, _context,
                    from.RectTransform, to.RectTransform);
                _connectionViews.Add(view);
            }
            Refresh();
        }

        private void OnDestroy() => Unsubscribe();
        private void OnSkillChanged(SkillChange change) => Refresh();

        private void Unsubscribe()
        {
            if (_state != null)
                _state.SkillChanged -= OnSkillChanged;
        }

        private static void ClearChildren(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
        }
    }
}
