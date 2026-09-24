using System;
using System.Collections.Generic;
using System.Linq;
using RogueliteToolkit.Effects;
using RogueliteToolkit.SkillTree.Authoring;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RogueliteToolkit.SkillTree.Editor
{
    public sealed class SkillTreeAuthoringWindow : EditorWindow
    {
        private const float MaxZoom = 4f;
        [SerializeField] private SkillTreeAuthoring _tree;
        [SerializeField] private SkillConnectionCondition _condition;
        [SerializeField] private int _minimumLevel = 1;

        private SkillTreeGraphView _graphView;
        private ObjectField _treeField;
        private IntegerField _minimumLevelField;
        private Slider _zoomField;

        [MenuItem("Tools/Roguelite Toolkit/Skill Tree Authoring")]
        public static void OpenFromMenu() => GetWindow<SkillTreeAuthoringWindow>(
            "Skill Tree Authoring");

        internal static void Open(SkillTreeAuthoring tree)
        {
            SkillTreeAuthoringWindow window = GetWindow<SkillTreeAuthoringWindow>(
                "Skill Tree Authoring");
            window._tree = tree;
            window.SyncTreeField();
            window.RebuildGraph();
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.tuna.framework/Runtime/Framework/RogueliteToolkit/Editor/" +
                "SkillTree/SkillTreeAuthoringWindow.uss");
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            BuildToolbar();
            _graphView = new SkillTreeGraphView(this);
            _graphView.style.flexGrow = 1f;
            rootVisualElement.Add(_graphView);
            _zoomField?.SetValueWithoutNotify(1f);
            RebuildGraph();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += RebuildGraph;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= RebuildGraph;
        }

        private void BuildToolbar()
        {
            Toolbar toolbar = new();
            _treeField = new ObjectField("Authoring Root")
            {
                objectType = typeof(SkillTreeAuthoring),
                allowSceneObjects = true,
                value = _tree
            };
            _treeField.style.minWidth = 280f;
            _treeField.RegisterValueChangedCallback(evt =>
            {
                _tree = evt.newValue as SkillTreeAuthoring;
                RebuildGraph();
            });
            toolbar.Add(_treeField);

            toolbar.Add(new ToolbarButton(CreateRoot) { text = "Create Root" });
            toolbar.Add(new ToolbarButton(RepairSelectedRoot) { text = "Repair Selected" });
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarButton(CreateNode) { text = "Create Node" });
            toolbar.Add(new ToolbarButton(DuplicateSelectedNode) { text = "Duplicate" });
            toolbar.Add(new ToolbarButton(DeleteSelection) { text = "Delete" });
            toolbar.Add(new ToolbarSpacer());

            EnumField conditionField = new("New Edge", _condition);
            conditionField.RegisterValueChangedCallback(evt =>
            {
                _condition = (SkillConnectionCondition)evt.newValue;
                UpdateMinimumLevelVisibility();
            });
            toolbar.Add(conditionField);

            _minimumLevelField = new IntegerField("Level")
            {
                value = Mathf.Max(1, _minimumLevel)
            };
            _minimumLevelField.style.width = 105f;
            _minimumLevelField.RegisterValueChangedCallback(evt =>
            {
                _minimumLevel = Mathf.Max(1, evt.newValue);
                if (_minimumLevelField.value != _minimumLevel)
                    _minimumLevelField.SetValueWithoutNotify(_minimumLevel);
            });
            toolbar.Add(_minimumLevelField);
            UpdateMinimumLevelVisibility();

            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new ToolbarButton(RebuildGraph) { text = "Refresh" });
            toolbar.Add(new ToolbarButton(RefreshFromDefinition)
            {
                text = "Refresh From Definition",
                tooltip = "Replace the authoring graph with Output. Undo with Ctrl+Z."
            });
            toolbar.Add(new ToolbarButton(() => _graphView?.FrameAll())
                { text = "Frame All" });
            _zoomField = new Slider("Zoom", 0.25f, MaxZoom)
            {
                value = 1f,
                showInputField = true
            };
            _zoomField.style.width = 150f;
            _zoomField.RegisterValueChangedCallback(evt =>
                _graphView?.SetZoom(evt.newValue));
            toolbar.Add(_zoomField);
            toolbar.Add(new ToolbarButton(Bake) { text = "Bake" });
            rootVisualElement.Add(toolbar);

            Toolbar nodeToolbar = new();
            nodeToolbar.AddToClassList("skill-tree-node-toolbar");
            Label nodeToolbarLabel = new("Node Cards");
            nodeToolbarLabel.AddToClassList("skill-tree-node-toolbar-label");
            nodeToolbar.Add(nodeToolbarLabel);
            ToolbarButton collapseAll = new(() => SetAllNodesExpanded(false))
            {
                text = "Collapse All",
                tooltip = "Collapse every skill node card"
            };
            collapseAll.AddToClassList("skill-tree-collapse-all");
            nodeToolbar.Add(collapseAll);
            ToolbarButton expandAll = new(() => SetAllNodesExpanded(true))
            {
                text = "Expand All",
                tooltip = "Expand every skill node card"
            };
            expandAll.AddToClassList("skill-tree-expand-all");
            nodeToolbar.Add(expandAll);
            nodeToolbar.Add(new ToolbarButton(ResetNodeView)
            {
                text = "Reset Node View",
                tooltip = "Restore default card size and expand all nodes. Undo with Ctrl+Z."
            });
            rootVisualElement.Add(nodeToolbar);
        }

        private void UpdateMinimumLevelVisibility()
        {
            if (_minimumLevelField != null)
            {
                _minimumLevelField.style.display = _condition ==
                                                   SkillConnectionCondition.MinimumLevel
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        private void CreateRoot()
        {
            _tree = SkillTreeAuthoringUtility.CreateTree();
            SyncTreeField();
            RebuildGraph();
        }

        private void RepairSelectedRoot()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null || selected.GetComponent<RectTransform>() == null)
            {
                EditorUtility.DisplayDialog("Repair Skill Tree",
                    "Select the broken RectTransform root in the Hierarchy first.", "OK");
                return;
            }

            _tree = SkillTreeAuthoringUtility.RepairTree(selected);
            SyncTreeField();
            RebuildGraph();
        }

        private void CreateNode()
        {
            if (!RequireTree())
                return;
            SkillNodeAuthoring node = SkillTreeAuthoringUtility.CreateNode(_tree);
            RebuildGraph();
            _graphView?.SelectNode(node);
        }

        private void CreateNodeAt(Vector2 graphPosition)
        {
            if (!RequireTree())
                return;
            SkillNodeAuthoring node = SkillTreeAuthoringUtility.CreateNode(
                _tree, new Vector2(graphPosition.x, -graphPosition.y));
            RebuildGraph();
            _graphView?.SelectNode(node);
        }

        private void DuplicateSelectedNode()
        {
            if (!RequireTree())
                return;
            SkillNodeAuthoring source = _graphView?.SelectedNode;
            if (source == null)
            {
                EditorUtility.DisplayDialog("Duplicate Skill Node",
                    "Select a node in the graph first.", "OK");
                return;
            }

            SkillNodeAuthoring node =
                SkillTreeAuthoringUtility.DuplicateNode(_tree, source);
            RebuildGraph();
            _graphView?.SelectNode(node);
        }

        private void DeleteSelection()
        {
            if (_graphView == null || _graphView.selection.Count == 0)
                return;
            _graphView.DeleteSelectionFromToolbar();
        }

        private void RefreshFromDefinition()
        {
            if (!SkillTreeAuthoringUtility.TryRefreshFromDefinition(_tree, out string error))
            {
                EditorUtility.DisplayDialog("Refresh From Definition", error, "OK");
                return;
            }

            RebuildGraph();
            _graphView?.FrameAll();
        }

        private void Bake()
        {
            if (RequireTree())
                SkillTreeAuthoringUtility.Bake(_tree);
        }

        private void ResetNodeView()
        {
            if (!RequireTree())
                return;
            SkillNodeAuthoring[] nodes = SkillTreeAuthoringUtility.GetAuthoredNodes(_tree);
            Undo.RecordObjects(nodes, "Reset Skill Node View");
            foreach (SkillNodeAuthoring node in nodes)
            {
                node.SetEditorSize(new Vector2(320f, 240f));
                node.SetEditorExpanded(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                EditorUtility.SetDirty(node);
            }

            RebuildGraph();
        }

        private bool RequireTree()
        {
            if (_tree != null)
                return true;
            EditorUtility.DisplayDialog("Skill Tree",
                "Assign or create an Authoring Root first.", "OK");
            return false;
        }

        private void SyncTreeField()
        {
            _treeField?.SetValueWithoutNotify(_tree);
        }

        private void RebuildGraph()
        {
            if (_graphView == null)
                return;
            _graphView.Populate(_tree);
        }

        internal void SetAllNodesExpanded(bool expanded) =>
            _graphView?.SetAllNodesExpanded(expanded);

        internal bool TryCreateConnection(
            SkillNodeAuthoring from, SkillNodeAuthoring to,
            out SkillConnectionAuthoring connection)
        {
            connection = null;
            if (!SkillTreeAuthoringUtility.TryCreateConnection(
                    _tree, from, to, _condition, _minimumLevel, out string error))
            {
                EditorUtility.DisplayDialog("Cannot Create Connection", error, "OK");
                return false;
            }

            SkillConnectionAuthoring[] connections =
                _tree.GetComponentsInChildren<SkillConnectionAuthoring>(true);
            connection = connections.FirstOrDefault(item =>
                item.From == from && item.To == to);
            return connection != null;
        }

        private sealed class SkillTreeGraphView : GraphView
        {
            private readonly SkillTreeAuthoringWindow _window;
            private readonly Dictionary<SkillNodeAuthoring, SkillNodeElement> _nodes = new();
            private readonly MiniMap _miniMap;
            private bool _isPopulating;
            private readonly List<SkillNodeElement> _editingGestureNodes = new();

            internal SkillNodeAuthoring SelectedNode =>
                selection.OfType<SkillNodeElement>().FirstOrDefault()?.Authoring;

            internal SkillTreeGraphView(SkillTreeAuthoringWindow window)
            {
                _window = window;
                name = "Skill Tree Graph";
                AddToClassList("skill-tree-graph");
                style.flexGrow = 1f;
                SetupZoom(ContentZoomer.DefaultMinScale, MaxZoom);
                RegisterCallback<KeyDownEvent>(OnAlignKey);
                // SelectionDragger can inspect mouse input before a child field's
                // bubbling callback. Disable movement before that manipulator runs.
                RegisterCallback<MouseDownEvent>(BeginEditingGesture, TrickleDown.TrickleDown);
                RegisterCallback<MouseUpEvent>(_ => schedule.Execute(EndEditingGesture),
                    TrickleDown.TrickleDown);
                this.AddManipulator(new ContentDragger());
                // Nodes use an explicit title drag handle. SelectionDragger treats
                // embedded inspector gestures as graph movement (including selection).
                this.AddManipulator(new RectangleSelector());
                GridBackground grid = new();
                Insert(0, grid);
                grid.StretchToParentSize();

                _miniMap = new MiniMap { anchored = true };
                _miniMap.SetPosition(new Rect(10f, 30f, 220f, 150f));
                Add(_miniMap);
                RegisterCallback<GeometryChangedEvent>(_ => PositionMiniMap());
                graphViewChanged = OnGraphViewChanged;
            }

            public override void AddToSelection(ISelectable selectable)
            {
                base.AddToSelection(selectable);
                RefreshEdgeHighlights();
            }

            public override void RemoveFromSelection(ISelectable selectable)
            {
                base.RemoveFromSelection(selectable);
                RefreshEdgeHighlights();
            }

            public override void ClearSelection()
            {
                base.ClearSelection();
                RefreshEdgeHighlights();
            }

            private void RefreshEdgeHighlights()
            {
                foreach (SkillEdgeElement edge in edges.ToList().OfType<SkillEdgeElement>())
                {
                    edge.RefreshHighlightLayer();
                    edge.UpdateEdgeControl();
                    edge.MarkDirtyRepaint();
                }
            }

            private void OnAlignKey(KeyDownEvent evt)
            {
                if ((evt.keyCode != KeyCode.Alpha1 && evt.keyCode != KeyCode.Keypad1) ||
                    evt.altKey || evt.ctrlKey || evt.commandKey || evt.shiftKey)
                    return;
                VisualElement target = evt.target as VisualElement;
                while (target != null && target != this)
                {
                    if (target.ClassListContains("unity-base-field") ||
                        target.ClassListContains("unity-text-input"))
                        return;
                    target = target.parent;
                }

                AlignSelectedGrid();
                evt.StopPropagation();
            }

            private void AlignSelectedGrid()
            {
                List<SkillNodeElement> selected = selection.OfType<SkillNodeElement>().ToList();
                if (selected.Count < 2) return;
                Rect[] positions = selected.Select(node => node.GetPosition()).ToArray();
                List<float> columns = new() { positions[0].x };
                List<float> rows = new() { positions[0].y };
                float columnTolerance = positions.Min(rect => rect.width) * 0.5f;
                float rowTolerance = positions.Min(rect => rect.height) * 0.5f;
                Undo.IncrementCurrentGroup();
                Undo.RecordObjects(selected.Select(node => (UnityEngine.Object)node.Authoring.RectTransform).ToArray(),
                    "Align Skill Nodes To Grid");
                for (int i = 1; i < selected.Count; i++)
                {
                    Rect position = positions[i];
                    position.x = AlignAxis(position.x, columns, columnTolerance);
                    position.y = AlignAxis(position.y, rows, rowTolerance);
                    selected[i].SetPosition(position);
                    RectTransform rect = selected[i].Authoring.RectTransform;
                    rect.anchoredPosition = new Vector2(position.x, -position.y);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                    EditorUtility.SetDirty(rect);
                }
            }

            private static float AlignAxis(float value, List<float> anchors, float tolerance)
            {
                // Ponytail: infer rows/columns within half a card; explicit grid spacing is needed for scattered layouts.
                float nearest = value;
                float distance = float.PositiveInfinity;
                foreach (float anchor in anchors)
                {
                    float delta = Mathf.Abs(value - anchor);
                    if (delta <= tolerance && delta < distance)
                    {
                        nearest = anchor;
                        distance = delta;
                    }
                }

                if (float.IsPositiveInfinity(distance)) anchors.Add(value);
                return nearest;
            }

            private void BeginEditingGesture(MouseDownEvent evt)
            {
                EndEditingGesture();
                if (evt.button != 0)
                    return;
                VisualElement target = evt.target as VisualElement;
                while (target != null && target != this)
                {
                    if (target.ClassListContains("skill-node-editor-input"))
                    {
                        foreach (SkillNodeElement node in _nodes.Values)
                        {
                            if ((node.capabilities & Capabilities.Movable) == 0)
                                continue;
                            _editingGestureNodes.Add(node);
                            node.capabilities &= ~Capabilities.Movable;
                        }

                        return;
                    }

                    target = target.parent;
                }
            }

            private void EndEditingGesture()
            {
                foreach (SkillNodeElement node in _editingGestureNodes)
                    node.capabilities |= Capabilities.Movable;
                _editingGestureNodes.Clear();
            }

            public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
            {
                Vector2 graphPosition =
                    contentViewContainer.WorldToLocal(evt.mousePosition);
                evt.menu.AppendAction("Create/Skill Node",
                    _ => _window.CreateNodeAt(graphPosition));
                evt.menu.AppendSeparator("Create/");
                evt.menu.AppendAction("Edit/Duplicate Selected Node",
                    _ => _window.DuplicateSelectedNode(),
                    _ => SelectedNode != null
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Edit/Delete Selection",
                    _ => _window.DeleteSelection(),
                    _ => selection.Count > 0
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("View/Frame All", _ => FrameAll());
                evt.menu.AppendAction("View/Reset Zoom", _ => SetZoom(1f));
                evt.menu.AppendAction("View/Collapse All Nodes",
                    _ => SetAllNodesExpanded(false));
                evt.menu.AppendAction("View/Expand All Nodes",
                    _ => SetAllNodesExpanded(true));
                evt.menu.AppendAction("Refresh", _ => _window.RebuildGraph());
                evt.menu.AppendAction("Bake Skill Tree", _ => _window.Bake());
            }

            internal void SetZoom(float scale)
            {
                scale = Mathf.Clamp(scale, 0.25f, MaxZoom);
                UpdateViewTransform(viewTransform.position, Vector3.one * scale);
                _window._zoomField?.SetValueWithoutNotify(scale);
            }

            internal void SetAllNodesExpanded(bool expanded)
            {
                foreach (SkillNodeElement node in _nodes.Values)
                    node.SetExpanded(expanded, expanded);
            }

            private void PositionMiniMap()
            {
                const float width = 220f;
                const float height = 150f;
                Rect target = new(
                    Mathf.Max(10f, contentRect.width - width - 12f),
                    Mathf.Max(30f, contentRect.height - height - 12f),
                    width, height);
                Rect current = _miniMap.GetPosition();
                if ((current.position - target.position).sqrMagnitude > 0.25f ||
                    (current.size - target.size).sqrMagnitude > 0.25f)
                    _miniMap.SetPosition(target);
            }

            public override List<Port> GetCompatiblePorts(
                Port startPort, NodeAdapter nodeAdapter)
            {
                return ports.ToList().Where(port =>
                    port != startPort && port.node != startPort.node &&
                    port.direction != startPort.direction).ToList();
            }

            internal void Populate(SkillTreeAuthoring tree)
            {
                _isPopulating = true;
                try
                {
                    DeleteElements(graphElements.Where(element =>
                        element is SkillNodeElement || element is Edge ||
                        element is GraphHintElement).ToList());
                    _nodes.Clear();
                    if (tree == null)
                    {
                        AddElement(new GraphHintElement(
                            "Create or assign an Authoring Root to begin."));
                        return;
                    }

                    SkillNodeAuthoring[] authoredNodes =
                        SkillTreeAuthoringUtility.GetAuthoredNodes(tree);
                    for (int i = 0; i < authoredNodes.Length; i++)
                    {
                        SkillNodeElement element = new(authoredNodes[i], _window);
                        _nodes.Add(authoredNodes[i], element);
                        AddElement(element);
                    }

                    SkillConnectionAuthoring[] connections =
                        tree.GetComponentsInChildren<SkillConnectionAuthoring>(true);
                    for (int i = 0; i < connections.Length; i++)
                    {
                        SkillConnectionAuthoring authored = connections[i];
                        if (authored.From == null || authored.To == null ||
                            !_nodes.TryGetValue(authored.From, out SkillNodeElement from) ||
                            !_nodes.TryGetValue(authored.To, out SkillNodeElement to))
                            continue;
                        SkillEdgeElement edge = new(authored)
                        {
                            output = from.Output,
                            input = to.Input
                        };
                        edge.output.Connect(edge);
                        edge.input.Connect(edge);
                        AddElement(edge);
                    }
                }
                finally
                {
                    _isPopulating = false;
                }
            }

            internal void SelectNode(SkillNodeAuthoring node)
            {
                if (node == null || !_nodes.TryGetValue(node, out SkillNodeElement element))
                    return;
                ClearSelection();
                AddToSelection(element);
                FrameSelection();
            }

            internal void DeleteSelectionFromToolbar() =>
                DeleteElements(selection.OfType<GraphElement>().ToList());

            private GraphViewChange OnGraphViewChanged(GraphViewChange change)
            {
                if (_isPopulating)
                    return change;

                if (change.movedElements != null)
                {
                    foreach (SkillNodeElement node in
                             change.movedElements.OfType<SkillNodeElement>())
                    {
                        Rect position = node.GetPosition();
                        Undo.RecordObject(node.Authoring.RectTransform, "Move Skill Node");
                        node.Authoring.RectTransform.anchoredPosition =
                            new Vector2(position.x, -position.y);
                        EditorUtility.SetDirty(node.Authoring);
                    }
                }

                if (change.edgesToCreate != null)
                {
                    List<Edge> accepted = new();
                    foreach (Edge edge in change.edgesToCreate)
                    {
                        SkillNodeElement from = edge.output?.node as SkillNodeElement;
                        SkillNodeElement to = edge.input?.node as SkillNodeElement;
                        if (from != null && to != null &&
                            _window.TryCreateConnection(from.Authoring, to.Authoring,
                                out SkillConnectionAuthoring authored))
                        {
                            edge.userData = authored;
                            accepted.Add(edge);
                        }
                    }

                    change.edgesToCreate = accepted;
                }

                if (change.elementsToRemove != null)
                {
                    foreach (GraphElement element in change.elementsToRemove)
                    {
                        if (element is SkillEdgeElement edge && edge.Authoring != null)
                            Undo.DestroyObjectImmediate(edge.Authoring.gameObject);
                        else if (element is Edge newEdge &&
                                 newEdge.userData is SkillConnectionAuthoring authored &&
                                 authored != null)
                            Undo.DestroyObjectImmediate(authored.gameObject);
                        else if (element is SkillNodeElement node && node.Authoring != null)
                            SkillTreeAuthoringUtility.DeleteNode(
                                _window._tree, node.Authoring);
                    }
                }

                return change;
            }
        }

        private sealed class SkillNodeElement : Node
        {
            internal SkillNodeAuthoring Authoring { get; }
            internal Port Input { get; }
            internal Port Output { get; }

            private readonly SkillTreeAuthoringWindow _window;
            private readonly List<VisualElement> _alignmentGuides = new();
            private Image _iconPreview;
            private Label _iconPlaceholder;
            private Resizer _resizer;
            private bool _applyingExpandedLayout;
            private Rect _graphRect;
            private bool _hasGraphRect;
            private VisualElement _outputPortGroup;
            private Button _cardToggle;
            private VisualElement _identityFields;
            private VisualElement _identityExpandedParent;
            private VisualElement _collapsedIdentity;
            private bool _isDragging;

            internal SkillNodeElement(
                SkillNodeAuthoring authoring, SkillTreeAuthoringWindow window)
            {
                Authoring = authoring;
                _window = window;
                AddToClassList("skill-tree-node");
                // Run before embedded fields consume the click. Raising the card
                // must not steal focus, capture input, or interrupt port/field editing.
                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0) return;
                    // Preserve the selected group when starting a drag on one of its cards.
                    GraphView graph = GetFirstAncestorOfType<GraphView>();
                    if (graph != null && !selected)
                    {
                        if (!evt.actionKey && !evt.shiftKey) graph.ClearSelection();
                        graph.AddToSelection(this);
                    }

                    if (Authoring != null && Selection.activeGameObject != Authoring.gameObject)
                        Selection.activeGameObject = Authoring.gameObject;
                    QueueBringToFront();
                }, TrickleDown.TrickleDown);
                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0) evt.StopPropagation();
                });
                RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0) evt.StopPropagation();
                });
                title = string.IsNullOrWhiteSpace(authoring.DisplayName)
                    ? authoring.Id
                    : authoring.DisplayName;
                // The scene component owns size/state; GraphView view-data must not
                // restore an older expanded rectangle after Collapse All.

                Color nodeBackground = new(0.12f, 0.13f, 0.16f, 1f);
                style.backgroundColor = nodeBackground;
                mainContainer.style.backgroundColor = nodeBackground;
                extensionContainer.style.backgroundColor = nodeBackground;
                extensionContainer.style.paddingLeft = 8f;
                extensionContainer.style.paddingRight = 8f;
                extensionContainer.style.paddingTop = 6f;
                extensionContainer.style.paddingBottom = 8f;
                extensionContainer.style.flexGrow = 1f;
                extensionContainer.style.overflow = Overflow.Hidden;
                titleContainer.style.display = DisplayStyle.Flex;
                titleContainer.style.flexShrink = 0f;
                titleContainer.style.height = 38f;
                titleContainer.style.minHeight = 38f;
                titleContainer.style.visibility = Visibility.Visible;
                titleContainer.style.opacity = 1f;

                Input = InstantiatePort(Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi, typeof(bool));
                Input.portName = "IN";
                Input.portColor = new Color(0.25f, 0.72f, 1f, 1f);
                Input.AddToClassList("skill-node-input-port");
                Output = InstantiatePort(Orientation.Horizontal, Direction.Output,
                    Port.Capacity.Multi, typeof(bool));
                Output.portName = "OUT";
                Output.portColor = new Color(1f, 0.58f, 0.18f, 1f);
                Output.AddToClassList("skill-node-output-port");

                VisualElement portRow = new();
                portRow.AddToClassList("skill-node-port-row");
                VisualElement inputPortGroup = new();
                inputPortGroup.AddToClassList("skill-node-port-group");
                inputPortGroup.AddToClassList("skill-node-input-group");
                inputPortGroup.Add(Input);
                _outputPortGroup = new VisualElement();
                _outputPortGroup.AddToClassList("skill-node-port-group");
                _outputPortGroup.AddToClassList("skill-node-output-group");
                _outputPortGroup.Add(Output);
                portRow.Add(inputPortGroup);
                portRow.Add(_outputPortGroup);
                topContainer.style.display = DisplayStyle.None;
                Button edit = new(() => SkillNodeEditWindow.Open(Authoring)) { text = "Edit" };
                edit.style.height = 24f;
                edit.style.flexShrink = 0f;
                edit.tooltip = "Edit this node's data";
                PreventNodeDrag(edit);
                mainContainer.Insert(1, edit);
                mainContainer.Insert(2, portRow);

                BuildSummary();
                // Let child controls handle input before blocking GraphView's drag gesture.
                // Ports and the card background remain available for connecting/moving.
                PreventNodeDrag(extensionContainer);
                PreventNodeDrag(_collapsedIdentity);
                expanded = authoring.EditorExpanded;
                RefreshExpandedState();
                RefreshPorts();

                Vector2 position = authoring.RectTransform.anchoredPosition;
                Vector2 size = authoring.EditorSize;
                SetPosition(new Rect(position.x, -position.y, size.x, size.y));
                capabilities |= Capabilities.Resizable;
                _resizer = new Resizer();
                Add(_resizer);
                RegisterCallback<GeometryChangedEvent>(OnNodeGeometryChanged);
                RegisterCallback<DetachFromPanelEvent>(_ => ClearAlignmentGuides());
                schedule.Execute(ApplyExpandedLayout);
            }

            private void BuildSummary()
            {
                title = string.Empty;
                VisualElement nativeCollapse = titleContainer.Q("collapse-button");
                if (nativeCollapse != null)
                    nativeCollapse.style.display = DisplayStyle.None;
                _cardToggle = new Button(() => SetExpanded(!expanded, !expanded));
                _cardToggle.AddToClassList("skill-node-card-toggle");
                PreventNodeDrag(_cardToggle);
                titleContainer.Insert(0, _cardToggle);
                Label dragHandle = new("⠿")
                {
                    name = "node-drag-handle",
                    tooltip = "Drag to move and align with other nodes. Hold Alt to disable snapping."
                };
                titleContainer.Insert(1, dragHandle);
                List<(SkillNodeElement node, Rect start)> draggedNodes = new();
                Vector2 dragStart = default;
                Rect nodeStart = default;
                dragHandle.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;
                    draggedNodes.Clear();
                    foreach (SkillNodeElement node in GetFirstAncestorOfType<GraphView>().selection
                                 .OfType<SkillNodeElement>())
                    {
                        draggedNodes.Add((node, node.GetPosition()));
                        node._isDragging = true;
                    }

                    dragStart = parent.WorldToLocal(evt.mousePosition);
                    nodeStart = GetPosition();
                    dragHandle.CaptureMouse();
                    evt.StopPropagation();
                });
                dragHandle.RegisterCallback<MouseMoveEvent>(evt =>
                {
                    if (!_isDragging || !dragHandle.HasMouseCapture())
                        return;
                    Rect moved = nodeStart;
                    moved.position += (Vector2)parent.WorldToLocal(evt.mousePosition) - dragStart;
                    moved = AlignWithOtherNodes(moved, !evt.altKey);
                    Vector2 delta = moved.position - nodeStart.position;
                    foreach (var entry in draggedNodes)
                    {
                        Rect position = entry.start;
                        position.position += delta;
                        entry.node.SetPosition(position);
                    }

                    evt.StopPropagation();
                });

                void FinishDrag()
                {
                    if (!_isDragging)
                        return;
                    ClearAlignmentGuides();
                    Undo.RecordObjects(
                        draggedNodes.Select(entry => (UnityEngine.Object)entry.node.Authoring.RectTransform).ToArray(),
                        "Move Skill Nodes");
                    foreach (var entry in draggedNodes)
                    {
                        Rect moved = entry.node.GetPosition();
                        RectTransform rect = entry.node.Authoring.RectTransform;
                        rect.anchoredPosition = new Vector2(moved.x, -moved.y);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                        EditorUtility.SetDirty(rect);
                        entry.node._isDragging = false;
                    }

                    draggedNodes.Clear();
                }

                dragHandle.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (evt.button != 0 || !_isDragging)
                        return;
                    FinishDrag();
                    dragHandle.ReleaseMouse();
                    evt.StopPropagation();
                });
                dragHandle.RegisterCallback<MouseCaptureOutEvent>(_ => FinishDrag());
                TextField displayName = new() { isDelayed = true };
                displayName.AddToClassList("skill-node-title-field");
                PreventNodeDrag(displayName);
                displayName.RegisterValueChangedCallback(evt =>
                {
                    if (Authoring == null) return;
                    using SerializedObject serialized = new(Authoring.DataTarget);
                    serialized.Update();
                    serialized.FindProperty("_displayName").stringValue = evt.newValue;
                    serialized.ApplyModifiedProperties();
                });
                mainContainer.Insert(1, displayName);
                EnumField incomingMode = new(Authoring.IncomingRequirementMode);
                incomingMode.AddToClassList("skill-node-incoming-field");
                incomingMode.tooltip = "How incoming connections are combined";
                PreventNodeDrag(incomingMode);
                incomingMode.RegisterValueChangedCallback(evt =>
                {
                    if (Authoring == null) return;
                    using SerializedObject serialized = new(Authoring);
                    serialized.Update();
                    serialized.FindProperty("_incomingRequirementMode").intValue =
                        (int)(RequirementMode)evt.newValue;
                    serialized.ApplyModifiedProperties();
                });
                _outputPortGroup.Insert(0, incomingMode);

                VisualElement quickRow = new();
                quickRow.AddToClassList("skill-node-quick-row");
                VisualElement iconFrame = new();
                iconFrame.AddToClassList("skill-node-icon-frame");
                _iconPreview = new Image { scaleMode = ScaleMode.ScaleToFit };
                _iconPreview.AddToClassList("skill-node-icon-preview");
                _iconPlaceholder = new Label("No Icon");
                _iconPlaceholder.AddToClassList("skill-node-icon-placeholder");
                iconFrame.Add(_iconPreview);
                iconFrame.Add(_iconPlaceholder);
                quickRow.Add(iconFrame);

                _identityFields = new VisualElement();
                _identityFields.AddToClassList("skill-node-quick-fields");
                _identityExpandedParent = quickRow;
                _collapsedIdentity = new VisualElement();
                _collapsedIdentity.AddToClassList("skill-node-collapsed-identity");
                _collapsedIdentity.style.display = DisplayStyle.None;
                mainContainer.Insert(4, _collapsedIdentity);
                ScrollView collapsedDetails = new(ScrollViewMode.Vertical);
                collapsedDetails.style.height = 32f;
                Label collapsedSummary = new() { name = "collapsed-value-price" };
                collapsedSummary.style.whiteSpace = WhiteSpace.Normal;
                collapsedDetails.Add(collapsedSummary);
                _collapsedIdentity.Add(collapsedDetails);
                Label identity = new();
                identity.style.whiteSpace = WhiteSpace.Normal;
                _identityFields.Add(identity);
                quickRow.Add(_identityFields);
                extensionContainer.Add(quickRow);
                ScrollView details = new(ScrollViewMode.Vertical);
                details.AddToClassList("skill-node-settings-scroll");
                Label summary = new();
                summary.style.whiteSpace = WhiteSpace.Normal;
                details.Add(summary);
                extensionContainer.Add(details);
                tooltip = "Select this node to edit its data in the Inspector.";

                UnityEngine.Object observedTarget = null;
                int observedDataDirty = -1;
                int observedAuthoringDirty = -1;

                void RefreshSummary()
                {
                    if (Authoring == null) return;
                    if (!_isDragging)
                    {
                        Rect rect = GetPosition();
                        Vector2 position = Authoring.RectTransform.anchoredPosition;
                        Vector2 graphPosition = new(position.x, -position.y);
                        if ((rect.position - graphPosition).sqrMagnitude > 0.01f)
                        {
                            rect.position = graphPosition;
                            SetPosition(rect);
                        }
                    }

                    UnityEngine.Object target = Authoring.DataTarget;
                    int dataDirty = EditorUtility.GetDirtyCount(target);
                    int authoringDirty = EditorUtility.GetDirtyCount(Authoring);
                    RefreshIconPreview();
                    if (target == observedTarget && dataDirty == observedDataDirty &&
                        authoringDirty == observedAuthoringDirty) return;
                    observedTarget = target;
                    observedDataDirty = dataDirty;
                    observedAuthoringDirty = authoringDirty;
                    displayName.SetValueWithoutNotify(Authoring.DisplayName ?? Authoring.Id);
                    displayName.tooltip = displayName.value;
                    incomingMode.SetValueWithoutNotify(Authoring.IncomingRequirementMode);
                    identity.text =
                        $"ID: {Authoring.Id}\nLevels: {Authoring.MaxLevel} | Phases: {Authoring.PhaseCount}";
                    identity.tooltip = identity.text;
                    List<string> rows = new()
                    {
                        "Price (" + Authoring.CostResourceType + "): " + string.Join(", ", Authoring.Costs)
                    };
                    foreach (SkillResourceCost cost in Authoring.AdditionalCosts)
                        rows.Add("Price (" + cost.Resource + "): " + string.Join(", ",
                            Enumerable.Range(1, Authoring.MaxLevel)
                                .Select(level => cost.GetCost(level).ToString("G6"))));
                    StatGameplayEffect[] statEffects = Authoring.StatEffects.Where(effect => effect != null).ToArray();
                    if (statEffects.Length == 0)
                        rows.Add("Value: —");
                    foreach (StatGameplayEffect effect in statEffects)
                    {
                        string label = statEffects.Length > 1
                            ? $"Value ({(effect.Stat != null ? effect.Stat.name : "None")})"
                            : "Value";
                        rows.Add(label + ": " + string.Join(", ", Enumerable.Range(1, Authoring.MaxLevel)
                            .Select(level =>
                                (effect.GetValueAtLevel(level) - effect.GetValueAtLevel(level - 1))
                                .ToString("0.###"))));
                    }

                    collapsedSummary.text = string.Join("\n", rows);
                    collapsedSummary.tooltip = collapsedSummary.text;
                    rows.Add("Resources: " + Authoring.CostResourceType);
                    foreach (StatGameplayEffect effect in statEffects)
                        rows.Add($"Stat: {(effect.Stat != null ? effect.Stat.name : "None")} ({effect.Operation})");
                    summary.text = string.Join("\n", rows);
                    summary.tooltip = summary.text;
                    RefreshIconPreview();
                }

                RefreshSummary();
                // Read the current DataTarget each time so assigning a different asset
                // in the Inspector also refreshes the card without rebuilding the graph.
                schedule.Execute(RefreshSummary).Every(150);
            }

            private static void PreventNodeDrag(VisualElement element)
            {
                element.AddToClassList("skill-node-editor-input");
                element.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0)
                        evt.StopPropagation();
                });
                element.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                        evt.StopPropagation();
                });
            }

            private void ClearAlignmentGuides()
            {
                foreach (VisualElement guide in _alignmentGuides) guide.RemoveFromHierarchy();
                _alignmentGuides.Clear();
            }

            private Rect AlignWithOtherNodes(Rect moved, bool snap)
            {
                ClearAlignmentGuides();
                GraphView graph = GetFirstAncestorOfType<GraphView>();
                if (graph == null || !snap) return moved;
                float zoom = Mathf.Max(0.01f, graph.viewTransform.scale.x);
                float threshold = 6f / zoom;
                float bestX = threshold, bestY = threshold;
                float deltaX = 0f, deltaY = 0f, lineX = 0f, lineY = 0f;
                Rect targetX = default, targetY = default;
                bool foundX = false, foundY = false;
                float[] xs = { moved.xMin, moved.center.x, moved.xMax };
                float[] ys = { moved.yMin, moved.center.y, moved.yMax };
                foreach (SkillNodeElement other in graph.nodes.ToList().OfType<SkillNodeElement>())
                {
                    if (other == this || other._isDragging) continue;
                    Rect target = other.GetPosition();
                    float[] tx = { target.xMin, target.center.x, target.xMax };
                    float[] ty = { target.yMin, target.center.y, target.yMax };
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float dx = tx[j] - xs[i];
                            if (Mathf.Abs(dx) < bestX)
                            {
                                bestX = Mathf.Abs(dx);
                                deltaX = dx;
                                lineX = tx[j];
                                targetX = target;
                                foundX = true;
                            }

                            float dy = ty[j] - ys[i];
                            if (Mathf.Abs(dy) < bestY)
                            {
                                bestY = Mathf.Abs(dy);
                                deltaY = dy;
                                lineY = ty[j];
                                targetY = target;
                                foundY = true;
                            }
                        }
                    }
                }

                moved.position += new Vector2(deltaX, deltaY);
                float padding = 16f / zoom;
                if (foundX)
                    AddAlignmentGuide(new Rect(lineX, Mathf.Min(moved.yMin, targetX.yMin) - padding,
                        1f / zoom,
                        Mathf.Max(moved.yMax, targetX.yMax) - Mathf.Min(moved.yMin, targetX.yMin) + padding * 2f));
                if (foundY)
                    AddAlignmentGuide(new Rect(Mathf.Min(moved.xMin, targetY.xMin) - padding, lineY,
                        Mathf.Max(moved.xMax, targetY.xMax) - Mathf.Min(moved.xMin, targetY.xMin) + padding * 2f,
                        1f / zoom));
                return moved;
            }

            private void AddAlignmentGuide(Rect rect)
            {
                VisualElement guide = new() { pickingMode = PickingMode.Ignore };
                guide.style.position = Position.Absolute;
                guide.style.left = rect.x;
                guide.style.top = rect.y;
                guide.style.width = rect.width;
                guide.style.height = rect.height;
                guide.style.backgroundColor = new Color(0.2f, 0.9f, 1f, 0.95f);
                parent.Add(guide);
                _alignmentGuides.Add(guide);
            }

            private void OnNodeGeometryChanged(GeometryChangedEvent evt)
            {
                if (evt.target != this || _applyingExpandedLayout)
                    return;
                if (Authoring.EditorExpanded != expanded)
                {
                    Undo.RecordObject(Authoring, expanded
                        ? "Expand Skill Node"
                        : "Collapse Skill Node");
                    Authoring.SetEditorExpanded(expanded);
                    EditorUtility.SetDirty(Authoring);
                    ApplyExpandedLayout();
                    return;
                }

                if (!expanded)
                {
                    if (Mathf.Abs(evt.newRect.width - 224f) > 0.5f ||
                        Mathf.Abs(evt.newRect.height - 158f) > 0.5f)
                        ApplyExpandedLayout();
                    return;
                }

                Vector2 size = evt.newRect.size;
                // Resizer changes inline dimensions directly; retain that size,
                // but never derive graph coordinates from a transient layout.
                if (_hasGraphRect && size.x > 0f && size.y > 0f)
                    _graphRect.size = size;
                if (size.x < 1f || size.y < 1f ||
                    (Authoring.EditorSize - size).sqrMagnitude < 0.25f)
                    return;
                Undo.RecordObject(Authoring, "Resize Skill Node");
                Authoring.SetEditorSize(size);
                EditorUtility.SetDirty(Authoring);
            }

            internal void SetExpanded(bool value, bool expandAllSections = false)
            {
                expanded = value;
                RefreshExpandedState();
                extensionContainer.style.display = value
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                Undo.RecordObject(Authoring, value
                    ? "Expand Skill Node"
                    : "Collapse Skill Node");
                Authoring.SetEditorExpanded(value);
                EditorUtility.SetDirty(Authoring);
                ApplyExpandedLayout();
            }

            private void ApplyExpandedLayout()
            {
                if (Authoring == null)
                    return;
                _applyingExpandedLayout = true;
                Rect rect = GetPosition();
                _cardToggle.text = expanded ? "▼" : "▶";
                _cardToggle.tooltip = expanded ? "Collapse this card" : "Expand this card";
                VisualElement identityParent = expanded
                    ? _identityExpandedParent
                    : _collapsedIdentity;
                if (_identityFields.parent != identityParent)
                {
                    if (expanded)
                        identityParent.Add(_identityFields);
                    else
                        identityParent.Insert(0, _identityFields);
                }

                _collapsedIdentity.style.display = expanded
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
                extensionContainer.style.display = expanded
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                titleContainer.style.height = 38f;
                titleContainer.style.minHeight = 38f;
                if (expanded)
                {
                    style.minWidth = 320f;
                    style.minHeight = 240f;
                    style.maxWidth = StyleKeyword.None;
                    style.maxHeight = StyleKeyword.None;
                    Vector2 size = Authoring.EditorSize;
                    rect.width = size.x;
                    rect.height = size.y;
                    RemoveFromClassList("skill-tree-node-collapsed");
                    if (_resizer != null)
                        _resizer.style.display = DisplayStyle.Flex;
                }
                else
                {
                    rect.width = 224f;
                    rect.height = 206f;
                    style.minWidth = 224f;
                    style.maxWidth = 224f;
                    style.minHeight = 206f;
                    style.maxHeight = 206f;
                    AddToClassList("skill-tree-node-collapsed");
                    if (_resizer != null)
                        _resizer.style.display = DisplayStyle.None;
                }

                SetPosition(rect);
                _applyingExpandedLayout = false;
            }

            public override Rect GetPosition() => _hasGraphRect
                ? _graphRect
                : base.GetPosition();

            public override void SetPosition(Rect position)
            {
                if (!IsFinite(position.x) || !IsFinite(position.y) ||
                    !IsFinite(position.width) || !IsFinite(position.height))
                    return;
                _graphRect = position;
                _hasGraphRect = true;
                base.SetPosition(position);
                // Node.SetPosition sets left/top only. Explicit dimensions keep
                // content edits from sizing/reflowing the outer graph card.
                style.position = Position.Absolute;
                style.width = position.width;
                style.height = position.height;
            }

            private static bool IsFinite(float value) =>
                !float.IsNaN(value) && !float.IsInfinity(value);

            private void RefreshIconPreview()
            {
                bool hasIcon = Authoring != null && Authoring.Icon != null;
                _iconPreview.sprite = hasIcon ? Authoring.Icon : null;
                Vector2 nodeSize = new(32f, 32f);
                _iconPreview.style.width = nodeSize.x;
                _iconPreview.style.height = nodeSize.y;
                VisualElement frame = _iconPreview.parent;
                frame.style.minWidth = Mathf.Max(112f, nodeSize.x + 8f);
                frame.style.height = Mathf.Max(96f, nodeSize.y + 8f);
                if (frame.parent != null)
                {
                    frame.parent.style.height = Mathf.Max(96f, nodeSize.y + 8f);
                    frame.parent.style.minHeight = Mathf.Max(96f, nodeSize.y + 8f);
                }

                _iconPreview.style.display = hasIcon
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _iconPlaceholder.style.display = hasIcon
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            private void QueueBringToFront()
            {
                // Changing hierarchy during dispatch can retarget the compatibility
                // mouse event. Raise only after the current input event has finished.
                schedule.Execute(() =>
                {
                    if (panel != null) BringToFront();
                });
            }

            public override void OnSelected()
            {
                base.OnSelected();
                QueueBringToFront();
                if (Authoring != null)
                    Selection.activeGameObject = Authoring.gameObject;
            }
        }

        private sealed class SkillEdgeElement : Edge
        {
            internal SkillConnectionAuthoring Authoring { get; }

            internal SkillEdgeElement(SkillConnectionAuthoring authoring)
            {
                layer = -1;
                Authoring = authoring;
                userData = authoring;
                tooltip = authoring.Condition == SkillConnectionCondition.MinimumLevel
                    ? $"Requires {authoring.From.Id} level >= {authoring.MinimumLevel}"
                    : $"Requires {authoring.From.Id} unlocked";
            }

            internal void RefreshHighlightLayer()
            {
                bool highlighted = input?.node.selected == true || output?.node.selected == true;
                int targetLayer = highlighted ? Mathf.Max(input.node.layer, output.node.layer) + 1 : -1;
                if (layer == targetLayer) return;
                GraphView graph = GetFirstAncestorOfType<GraphView>();
                layer = targetLayer;
                if (graph != null)
                {
                    graph.RemoveElement(this);
                    graph.AddElement(this);
                }
            }

            public override bool UpdateEdgeControl()
            {
                bool updated = base.UpdateEdgeControl();
                if (input?.node.selected == true || output?.node.selected == true)
                {
                    edgeControl.inputColor = new Color(1f, 0.85f, 0.2f);
                    edgeControl.outputColor = new Color(1f, 0.85f, 0.2f);
                    edgeControl.edgeWidth = 4;
                }

                return updated;
            }

            public override void OnSelected()
            {
                base.OnSelected();
                if (Authoring != null)
                    Selection.activeGameObject = Authoring.gameObject;
            }
        }

        private sealed class GraphHintElement : GraphElement
        {
            internal GraphHintElement(string message)
            {
                Label label = new(message);
                label.style.fontSize = 16f;
                label.style.color = new Color(0.75f, 0.75f, 0.75f);
                Add(label);
                SetPosition(new Rect(40f, 40f, 420f, 40f));
                capabilities &= ~Capabilities.Selectable;
                capabilities &= ~Capabilities.Deletable;
                capabilities &= ~Capabilities.Movable;
            }
        }
    }
}