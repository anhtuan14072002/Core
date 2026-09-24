using System;
using System.Collections.Generic;
using RogueliteToolkit.SkillTree.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RogueliteToolkit.SkillTree.Editor
{
    internal static class SkillTreeAuthoringUtility
    {
        internal static SkillTreeAuthoring CreateTree()
        {
            GameObject root = null;
            try
            {
                root = new GameObject("SkillTreeAuthoring", typeof(RectTransform));
                Canvas parentCanvas = Selection.activeGameObject != null
                    ? Selection.activeGameObject.GetComponentInParent<Canvas>()
                    : null;
                if (parentCanvas != null)
                {
                    root.transform.SetParent(parentCanvas.transform, false);
                    Stretch((RectTransform)root.transform);
                }
                else
                {
                    Canvas canvas = root.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    root.AddComponent<CanvasScaler>();
                    root.AddComponent<GraphicRaycaster>();
                }

                GetOrCreateRoot(root.transform, "Tree");
                SkillTreeAuthoring tree = ConfigureTree(root);
                Undo.RegisterCreatedObjectUndo(root, "Create Skill Tree Authoring");
                Selection.activeGameObject = root;
                return tree;
            }
            catch (Exception exception)
            {
                if (root != null)
                    UnityEngine.Object.DestroyImmediate(root);
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Create Skill Tree Failed",
                    "The authoring root could not be created. Check the Console for details.",
                    "OK");
                return null;
            }
        }

        internal static SkillTreeAuthoring RepairTree(GameObject root)
        {
            if (root == null || root.GetComponent<RectTransform>() == null)
                return null;
            Undo.RegisterFullObjectHierarchyUndo(root, "Repair Skill Tree Authoring");
            SkillTreeAuthoring tree = ConfigureTree(root);
            Selection.activeGameObject = root;
            return tree;
        }

        private static SkillTreeAuthoring ConfigureTree(GameObject root)
        {
            SkillTreeAuthoring tree = root.GetComponent<SkillTreeAuthoring>();
            if (tree == null)
                tree = root.AddComponent<SkillTreeAuthoring>();
            if (tree == null)
                throw new InvalidOperationException(
                    "Unity could not attach SkillTreeAuthoring to the scene object.");

            Transform content = root.transform.Find("Tree");
            if (content == null)
                content = root.transform;
            RectTransform connections = GetOrCreateRoot(content, "Connections");
            RectTransform nodes = GetOrCreateRoot(content, "Nodes");
            SerializedObject serialized = new(tree);
            SerializedProperty id = serialized.FindProperty("_id");
            if (string.IsNullOrWhiteSpace(id.stringValue))
                id.stringValue = "skill_tree";
            serialized.FindProperty("_connectionsRoot").objectReferenceValue =
                connections;
            serialized.FindProperty("_nodesRoot").objectReferenceValue = nodes;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tree;
        }

        private static RectTransform GetOrCreateRoot(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            RectTransform rect = existing as RectTransform;
            if (rect == null)
            {
                GameObject gameObject = new(name, typeof(RectTransform));
                gameObject.transform.SetParent(parent, false);
                rect = (RectTransform)gameObject.transform;
            }

            Stretch(rect);
            return rect;
        }

        internal static SkillNodeAuthoring CreateNode(
            SkillTreeAuthoring tree, Vector2? anchoredPosition = null)
        {
            if (tree == null)
                return null;
            GameObject gameObject = tree.NodePrefab != null
                ? UnityEngine.Object.Instantiate(tree.NodePrefab.gameObject)
                : new GameObject("Skill Node", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image),
                    typeof(SkillNodeAuthoring));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Skill Node");
            gameObject.transform.SetParent(tree.NodesRoot != null
                ? tree.NodesRoot
                : tree.transform, false);
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(160f, 80f);
            rect.anchoredPosition = anchoredPosition ?? Vector2.zero;
            if (tree.NodePrefab == null)
            {
                Image image = gameObject.GetComponent<Image>();
                image.color = new Color(0.15f, 0.2f, 0.3f, 0.85f);
            }

            SkillNodeAuthoring node = gameObject.GetComponent<SkillNodeAuthoring>();
            SetNodeIdentity(node, GenerateUniqueId(tree, "skill_node"), "Skill Node",
                tree.NodePrefab == null);
            gameObject.name = node.Id;
            Selection.activeGameObject = gameObject;
            return node;
        }

        internal static SkillNodeAuthoring DuplicateNode(
            SkillTreeAuthoring tree, SkillNodeAuthoring source)
        {
            if (tree == null || source == null || !BelongsTo(tree, source))
                return null;
            GameObject clone = UnityEngine.Object.Instantiate(
                source.gameObject, source.transform.parent);
            Undo.RegisterCreatedObjectUndo(clone, "Duplicate Skill Node");
            clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            SkillNodeAuthoring node = clone.GetComponent<SkillNodeAuthoring>();
            node.SetData(null);
            string id = GenerateUniqueId(tree,
                string.IsNullOrWhiteSpace(source.Id) ? "skill_node" : source.Id);
            SetNodeIdentity(node, id,
                string.IsNullOrWhiteSpace(source.DisplayName)
                    ? "Skill Node"
                    : source.DisplayName + " Copy", false);
            clone.name = id;
            Selection.activeGameObject = clone;
            return node;
        }

        internal static void DeleteNode(SkillTreeAuthoring tree, SkillNodeAuthoring node)
        {
            if (tree == null || node == null || !BelongsTo(tree, node))
                return;
            SkillConnectionAuthoring[] connections =
                tree.GetComponentsInChildren<SkillConnectionAuthoring>(true);
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].From == node || connections[i].To == node)
                    Undo.DestroyObjectImmediate(connections[i].gameObject);
            }

            Undo.DestroyObjectImmediate(node.gameObject);
        }

        internal static bool TryCreateConnection(
            SkillTreeAuthoring tree, SkillNodeAuthoring from,
            SkillNodeAuthoring to, SkillConnectionCondition condition,
            int minimumLevel, out string error)
        {
            if (tree == null || from == null || to == null ||
                !BelongsTo(tree, from) || !BelongsTo(tree, to))
            {
                error = "Source and target must belong to the active skill tree.";
                return false;
            }

            if (from == to)
            {
                error = "A node cannot connect to itself.";
                return false;
            }

            SkillConnectionAuthoring[] existing =
                tree.GetComponentsInChildren<SkillConnectionAuthoring>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].From == from && existing[i].To == to)
                {
                    error = $"Connection '{from.Id}' -> '{to.Id}' already exists.";
                    return false;
                }
            }

            if (WouldCreateCycle(existing, from, to))
            {
                error = $"Connection '{from.Id}' -> '{to.Id}' would create a cycle.";
                return false;
            }

            GameObject gameObject = new GameObject($"{from.Id} -> {to.Id}", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image),
                typeof(SkillConnectionAuthoring));
            gameObject.name = $"{from.Id} -> {to.Id}";
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Skill Connection");
            gameObject.transform.SetParent(tree.ConnectionsRoot != null
                ? tree.ConnectionsRoot
                : tree.transform, false);
            gameObject.transform.SetAsFirstSibling();
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(100f, tree.ConnectionWidth);
            Image image = gameObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0.45f, 0.65f, 1f, 0.7f);
                image.raycastTarget = false;
            }

            gameObject.GetComponent<SkillConnectionAuthoring>().Configure(
                from, to, condition, minimumLevel);
            Selection.activeGameObject = gameObject;
            error = null;
            return true;
        }

        internal static bool TryRefreshFromDefinition(SkillTreeAuthoring tree, out string error)
        {
            if (tree == null || tree.Output == null)
            {
                error = "Select an Authoring Root and assign its Output SkillTreeDefinition first.";
                return false;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                error = "Exit Play Mode before refreshing the authoring graph.";
                return false;
            }

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Refresh Skill Tree From Definition");
            try
            {
                SkillTreeDefinition definition = tree.Output;
                foreach (SkillConnectionAuthoring connection in
                         tree.GetComponentsInChildren<SkillConnectionAuthoring>(true))
                    Undo.DestroyObjectImmediate(connection.gameObject);
                foreach (SkillNodeAuthoring node in GetAuthoredNodes(tree))
                    Undo.DestroyObjectImmediate(node.gameObject);

                SerializedObject serialized = new(tree);
                serialized.FindProperty("_id").stringValue = definition.Id;
                serialized.ApplyModifiedProperties();
                Dictionary<string, SkillNodeAuthoring> byId = new(StringComparer.Ordinal);
                foreach (SkillNodeDefinition source in definition.Nodes)
                {
                    SkillNodeAuthoring node = CreateNode(tree, source.Position);
                    // Definition and authoring share serialized configuration field names.
                    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), node);
                    node.SetData(source.Data);
                    node.gameObject.name = source.Id;
                    byId.Add(source.Id, node);
                }

                foreach (SkillConnectionDefinition source in definition.Connections)
                {
                    SkillConnectionCondition condition;
                    int minimumLevel = 1;
                    if (source.Requirement is NodeLevelRequirement level && level.NodeId == source.FromNodeId)
                    {
                        condition = SkillConnectionCondition.MinimumLevel;
                        minimumLevel = level.MinimumLevel;
                    }
                    else if (source.Requirement is NodeUnlockedRequirement unlocked &&
                             unlocked.NodeId == source.FromNodeId)
                        condition = SkillConnectionCondition.Unlocked;
                    else
                        throw new InvalidOperationException(
                            "This definition contains a requirement the authoring tool cannot represent.");

                    if (!TryCreateConnection(tree, byId[source.FromNodeId], byId[source.ToNodeId],
                            condition, minimumLevel, out error))
                        throw new InvalidOperationException(error);
                }

                if (!TryBuild(tree, out _, out _, out error))
                    throw new InvalidOperationException(error);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = tree.gameObject;
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(group);
                error = exception.Message;
                return false;
            }
        }

        internal static void Bake(SkillTreeAuthoring authoring)
        {
            if (!TryBuild(authoring, out SkillNodeDefinition[] nodes,
                    out SkillConnectionDefinition[] connections, out string error))
            {
                EditorUtility.DisplayDialog("Skill Tree Bake Failed", error, "OK");
                return;
            }

            SkillTreeDefinition output = authoring.Output;
            if (output == null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Skill Tree", "SkillTreeDefinition", "asset",
                    "Choose the baked SkillTreeDefinition location.");
                if (string.IsNullOrEmpty(path))
                    return;
                output = ScriptableObject.CreateInstance<SkillTreeDefinition>();
                AssetDatabase.CreateAsset(output, path);
                Undo.RecordObject(authoring, "Assign Skill Tree Output");
                authoring.SetOutput(output);
                EditorUtility.SetDirty(authoring);
            }

            Undo.RecordObject(output, "Bake Skill Tree");
            SkillNodeAssetMigration.StoreNodes(output, nodes,
                GetAuthoredNodes(authoring));
            output.Configure(authoring.Id, nodes, connections);
            EditorUtility.SetDirty(output);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(output);
            Debug.Log($"Baked skill tree '{authoring.Id}' with {nodes.Length} nodes and " +
                      $"{connections.Length} connections.", output);
        }

        internal static bool TryBuild(
            SkillTreeAuthoring authoring, out SkillNodeDefinition[] nodes,
            out SkillConnectionDefinition[] connections, out string error)
        {
            nodes = Array.Empty<SkillNodeDefinition>();
            connections = Array.Empty<SkillConnectionDefinition>();
            if (authoring == null || string.IsNullOrWhiteSpace(authoring.Id))
            {
                error = "Tree ID is required.";
                return false;
            }

            SkillNodeAuthoring[] authoredNodes = GetAuthoredNodes(authoring);
            Dictionary<string, SkillNodeAuthoring> byId = new(StringComparer.Ordinal);
            List<SkillNodeDefinition> bakedNodes = new(authoredNodes.Length);
            for (int i = 0; i < authoredNodes.Length; i++)
            {
                SkillNodeAuthoring authored = authoredNodes[i];
                if (string.IsNullOrWhiteSpace(authored.Id))
                {
                    error = $"Node '{authored.name}' is missing an ID.";
                    Selection.activeGameObject = authored.gameObject;
                    return false;
                }

                if (!byId.TryAdd(authored.Id, authored))
                {
                    error = $"Duplicate node ID '{authored.Id}'.";
                    Selection.activeGameObject = authored.gameObject;
                    return false;
                }

                SkillNodeDefinition node = new();
                node.Configure(authored.Id, authored.DisplayName, authored.Description,
                    authored.Icon, authored.RectTransform.anchoredPosition,
                    authored.LevelsPerPhase, NormalizeCosts(authored.Costs, authored.MaxLevel),
                    authored.IncomingRequirementMode, authored.CopyStatEffects(),
                    authored.CopyEffects(), authored.CostResourceType, authored.PhaseCount,
                    authored.Color, authored.InstantFreeUpgrade, authored.NodeColor);
                node.SetPhaseEndLevels(authored.PhaseEndLevels);
                node.SetAdditionalCosts(authored.AdditionalCosts);
                node.SetNodeSize(authored.NodeSize);
                node.SetNodeScale(authored.NodeScale);
                node.SetResourceId(authored.ResourceId);
                bakedNodes.Add(node);
            }

            SkillConnectionAuthoring[] authoredConnections =
                authoring.GetComponentsInChildren<SkillConnectionAuthoring>(true);
            List<SkillConnectionDefinition> bakedConnections =
                new(authoredConnections.Length);
            HashSet<string> connectionKeys = new(StringComparer.Ordinal);
            for (int i = 0; i < authoredConnections.Length; i++)
            {
                SkillConnectionAuthoring authored = authoredConnections[i];
                if (authored.From == null || authored.To == null)
                {
                    error = $"Connection '{authored.name}' has a missing node reference.";
                    Selection.activeGameObject = authored.gameObject;
                    return false;
                }

                if (authored.From == authored.To || !byId.ContainsValue(authored.From) ||
                    !byId.ContainsValue(authored.To))
                {
                    error = $"Connection '{authored.name}' is invalid or points outside this tree.";
                    Selection.activeGameObject = authored.gameObject;
                    return false;
                }

                string key = $"{authored.From.Id}\n{authored.To.Id}";
                if (!connectionKeys.Add(key))
                {
                    error = $"Duplicate connection '{authored.From.Id}' -> '{authored.To.Id}'.";
                    Selection.activeGameObject = authored.gameObject;
                    return false;
                }

                SkillRequirement requirement = authored.Condition ==
                                               SkillConnectionCondition.MinimumLevel
                    ? new NodeLevelRequirement(authored.From.Id, authored.MinimumLevel)
                    : new NodeUnlockedRequirement(authored.From.Id);
                bakedConnections.Add(new SkillConnectionDefinition(
                    authored.From.Id, authored.To.Id, requirement));
            }

            if (ContainsCycle(authoredNodes, authoredConnections))
            {
                error = "The skill tree contains a directed cycle. Remove a connection before baking.";
                return false;
            }

            nodes = bakedNodes.ToArray();
            connections = bakedConnections.ToArray();
            error = null;
            return true;
        }

        // DataSO MaxLevel is authoritative. Preserve existing prices and extend with
        // the last configured price so increasing MaxLevel does not create free upgrades.
        internal static double[] NormalizeCosts(IReadOnlyList<double> source, int maxLevel)
        {
            double[] result = new double[Math.Max(1, maxLevel)];
            double lastCost = 0;
            for (int i = 0; i < result.Length; i++)
            {
                if (source != null && i < source.Count)
                    lastCost = Math.Max(0, source[i]);
                result[i] = lastCost;
            }

            return result;
        }

        private static bool BelongsTo(SkillTreeAuthoring tree, Component item) =>
            item != null && (item.transform == tree.transform ||
                             item.transform.IsChildOf(tree.transform));

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetNodeIdentity(
            SkillNodeAuthoring node, string id, string displayName,
            bool initializeCosts)
        {
            SerializedObject serialized = new(node);
            serialized.FindProperty("_id").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            if (initializeCosts)
                serialized.FindProperty("_costs").arraySize = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string GenerateUniqueId(SkillTreeAuthoring tree, string seed)
        {
            string normalized = string.IsNullOrWhiteSpace(seed)
                ? "skill_node"
                : seed.Trim().ToLowerInvariant().Replace(' ', '_');
            HashSet<string> ids = new(StringComparer.Ordinal);
            SkillNodeAuthoring[] nodes = GetAuthoredNodes(tree);
            for (int i = 0; i < nodes.Length; i++)
                ids.Add(nodes[i].Id ?? string.Empty);
            if (!ids.Contains(normalized))
                return normalized;
            for (int suffix = 2;; suffix++)
            {
                string candidate = $"{normalized}_{suffix}";
                if (!ids.Contains(candidate))
                    return candidate;
            }
        }

        internal static SkillNodeAuthoring[] GetAuthoredNodes(SkillTreeAuthoring tree)
        {
            if (tree == null) return Array.Empty<SkillNodeAuthoring>();
            Transform parent = tree.NodesRoot != null ? tree.NodesRoot.transform : tree.transform;
            List<SkillNodeAuthoring> nodes = new();
            for (int i = 0; i < parent.childCount; i++)
            {
                SkillNodeAuthoring node = parent.GetChild(i).GetComponent<SkillNodeAuthoring>();
                if (node != null) nodes.Add(node);
            }

            return nodes.ToArray();
        }

        private static bool WouldCreateCycle(
            SkillConnectionAuthoring[] connections,
            SkillNodeAuthoring from, SkillNodeAuthoring to)
        {
            HashSet<SkillNodeAuthoring> visited = new();
            Stack<SkillNodeAuthoring> pending = new();
            pending.Push(to);
            while (pending.Count > 0)
            {
                SkillNodeAuthoring current = pending.Pop();
                if (current == from)
                    return true;
                if (!visited.Add(current))
                    continue;
                for (int i = 0; i < connections.Length; i++)
                {
                    if (connections[i].From == current && connections[i].To != null)
                        pending.Push(connections[i].To);
                }
            }

            return false;
        }

        private static bool ContainsCycle(
            SkillNodeAuthoring[] nodes, SkillConnectionAuthoring[] connections)
        {
            Dictionary<SkillNodeAuthoring, int> state = new();
            for (int i = 0; i < nodes.Length; i++)
            {
                if (Visit(nodes[i], connections, state))
                    return true;
            }

            return false;
        }

        private static bool Visit(
            SkillNodeAuthoring node, SkillConnectionAuthoring[] connections,
            Dictionary<SkillNodeAuthoring, int> state)
        {
            if (state.TryGetValue(node, out int value))
                return value == 1;
            state[node] = 1;
            for (int i = 0; i < connections.Length; i++)
            {
                if (connections[i].From == node && connections[i].To != null &&
                    Visit(connections[i].To, connections, state))
                    return true;
            }

            state[node] = 2;
            return false;
        }
    }
}