using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using RogueliteToolkit.Cards;
using RogueliteToolkit.Effects;
using RogueliteToolkit.SkillTree;
using RogueliteToolkit.SkillTree.Authoring;
using RogueliteToolkit.SkillTree.Editor;
using RogueliteToolkit.Stats;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RogueliteToolkit.Tests
{
    public sealed class RogueliteToolkitTests
    {
        [Test]
        public void SkillTreeAuthoring_SelectedNodeHighlightsConnectedEdges()
        {
            GameObject root = new("Edge Highlight Test", typeof(RectTransform), typeof(SkillTreeAuthoring));
            SkillTreeAuthoringWindow window = ScriptableObject.CreateInstance<SkillTreeAuthoringWindow>();
            try
            {
                SkillNodeAuthoring[] authored = new SkillNodeAuthoring[3];
                for (int i = 0; i < authored.Length; i++)
                {
                    GameObject child = new("Node", typeof(RectTransform), typeof(SkillNodeAuthoring));
                    child.transform.SetParent(root.transform, false);
                    authored[i] = child.GetComponent<SkillNodeAuthoring>();
                    authored[i].RectTransform.anchoredPosition = new Vector2(i * 400, 0);
                    SetField(authored[i], "_id", "node_" + i);
                }
                for (int i = 0; i < 2; i++)
                {
                    GameObject child = new("Edge", typeof(RectTransform), typeof(SkillConnectionAuthoring));
                    child.transform.SetParent(root.transform, false);
                    child.GetComponent<SkillConnectionAuthoring>().Configure(authored[i], authored[i + 1], SkillConnectionCondition.Unlocked, 1);
                }
                SetField(window, "_tree", root.GetComponent<SkillTreeAuthoring>());
                window.Show();
                window.CreateGUI();
                GraphView graph = window.rootVisualElement.Q<GraphView>();
                List<Node> nodes = graph.nodes.ToList();
                List<Edge> edges = graph.edges.ToList();
                foreach (Edge edge in edges) edge.UpdateEdgeControl();
                Color normal = edges[0].edgeControl.inputColor;
                int width = edges[0].edgeControl.edgeWidth;
                int normalLayer = edges[0].layer;
                graph.AddToSelection(nodes[1]);
                foreach (Edge edge in edges)
                {
                    Assert.That(edge.edgeControl.edgeWidth, Is.EqualTo(4));
                    Assert.That(edge.layer, Is.GreaterThan(nodes[0].layer));
                    Assert.That(edge.parent.parent.IndexOf(edge.parent),
                        Is.GreaterThan(nodes[0].parent.parent.IndexOf(nodes[0].parent)));
                    Assert.That(edge.edgeControl.inputColor, Is.EqualTo(new Color(1f, 0.85f, 0.2f)));
                }
                graph.AddToSelection(nodes[0]);
                graph.RemoveFromSelection(nodes[1]);
                Assert.That(edges[0].edgeControl.edgeWidth, Is.EqualTo(4));
                Assert.That(edges[1].edgeControl.edgeWidth, Is.EqualTo(width));
                graph.ClearSelection();
                Assert.That(edges[0].edgeControl.inputColor, Is.EqualTo(normal));
                Assert.That(edges[0].layer, Is.EqualTo(normalLayer));
                foreach (Edge edge in edges)
                {
                    Assert.That(edge.layer, Is.LessThan(nodes[0].layer));
                    Assert.That(edge.parent.parent.IndexOf(edge.parent),
                        Is.LessThan(nodes[0].parent.parent.IndexOf(nodes[0].parent)));
                }
                graph.AddToSelection(nodes[2]);
                Assert.That(edges[0].layer, Is.LessThan(nodes[0].layer));
                Assert.That(edges[1].layer, Is.GreaterThan(nodes[0].layer));
                graph.ClearSelection();
                Assert.That(edges[0].edgeControl.edgeWidth, Is.EqualTo(width));
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkillTreeAuthoring_OneAlignsGridAroundFirstSelection()
        {
            GameObject root = new("Grid Test", typeof(RectTransform), typeof(SkillTreeAuthoring));
            SkillTreeAuthoringWindow window = ScriptableObject.CreateInstance<SkillTreeAuthoringWindow>();
            Vector2[] positions = { new(20, 30), new(420, 45), new(35, 330), new(435, 345) };
            try
            {
                for (int i = 0; i < positions.Length; i++)
                {
                    GameObject child = new("Node", typeof(RectTransform), typeof(SkillNodeAuthoring));
                    child.transform.SetParent(root.transform, false);
                    child.GetComponent<RectTransform>().anchoredPosition = new Vector2(positions[i].x, -positions[i].y);
                    SetField(child.GetComponent<SkillNodeAuthoring>(), "_id", "node_" + i);
                }
                SetField(window, "_tree", root.GetComponent<SkillTreeAuthoring>());
                window.Show();
                window.CreateGUI();
                GraphView graph = window.rootVisualElement.Q<GraphView>();
                List<Node> nodes = graph.nodes.ToList();
                foreach (Node node in nodes) graph.AddToSelection(node);
                TextField input = new();
                graph.Add(input);
                using (KeyDownEvent typing = KeyDownEvent.GetPooled('1', KeyCode.Alpha1, EventModifiers.None))
                {
                    typing.target = input;
                    input.SendEvent(typing);
                }
                Assert.That(nodes[1].GetPosition().position, Is.EqualTo(positions[1]));
                for (int repeat = 0; repeat < 2; repeat++)
                {
                    using (KeyDownEvent key = KeyDownEvent.GetPooled('1', KeyCode.Alpha1, EventModifiers.None))
                    {
                        key.target = graph;
                        graph.SendEvent(key);
                    }
                    Assert.That(nodes[0].GetPosition().position, Is.EqualTo(positions[0]));
                    Assert.That(nodes[1].GetPosition().position, Is.EqualTo(new Vector2(420, 30)));
                    Assert.That(nodes[2].GetPosition().position, Is.EqualTo(new Vector2(20, 330)));
                    Assert.That(nodes[3].GetPosition().position, Is.EqualTo(new Vector2(420, 330)));
                    Undo.FlushUndoRecordObjects();
                }
                Undo.PerformUndo();
                SkillNodeAuthoring[] authored = root.GetComponentsInChildren<SkillNodeAuthoring>();
                for (int i = 0; i < positions.Length; i++)
                    Assert.That(authored[i].RectTransform.anchoredPosition,
                        Is.EqualTo(new Vector2(positions[i].x, -positions[i].y)));
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [UnityEngine.TestTools.UnityTest]
        public System.Collections.IEnumerator SkillTreeAuthoring_DragMovesSelectedGroup()
        {
            GameObject root = new("Group Drag Test", typeof(RectTransform), typeof(SkillTreeAuthoring));
            SkillTreeAuthoring tree = root.GetComponent<SkillTreeAuthoring>();
            SkillTreeAuthoringWindow window = ScriptableObject.CreateInstance<SkillTreeAuthoringWindow>();
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject child = new("Node", typeof(RectTransform), typeof(SkillNodeAuthoring));
                    child.transform.SetParent(root.transform, false);
                    child.GetComponent<RectTransform>().anchoredPosition = new Vector2(i * 400f, 0f);
                    SetField(child.GetComponent<SkillNodeAuthoring>(), "_id", "node_" + i);
                }
                SetField(window, "_tree", tree);
                window.Show();
                window.CreateGUI();
                yield return null;
                yield return null;
                GraphView graph = window.rootVisualElement.Q<GraphView>();
                List<Node> nodes = graph.nodes.ToList();
                foreach (Node node in nodes) graph.AddToSelection(node);
                Vector2 first = nodes[0].GetPosition().position;
                Vector2 second = nodes[1].GetPosition().position;
                VisualElement handle = nodes[0].Q("node-drag-handle");
                using (MouseDownEvent down = MouseDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(30, 30) }))
                {
                    down.target = handle;
                    handle.SendEvent(down);
                }
                Assert.That(graph.selection.Count, Is.EqualTo(2));
                yield return null;
                Assert.That(handle.HasMouseCapture(), Is.True, "Drag handle must capture mouse");
                using (MouseMoveEvent move = MouseMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = new Vector2(90, 70), modifiers = EventModifiers.Alt }))
                {
                    move.target = handle;
                    handle.SendEvent(move);
                }
                using (MouseUpEvent up = MouseUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = new Vector2(90, 70) }))
                {
                    up.target = handle;
                    handle.SendEvent(up);
                }
                Vector2 delta = nodes[0].GetPosition().position - first;
                Assert.That(delta.sqrMagnitude, Is.GreaterThan(0f));
                Assert.That(nodes[1].GetPosition().position - second, Is.EqualTo(delta));
                SkillNodeAuthoring[] authored = tree.GetComponentsInChildren<SkillNodeAuthoring>();
                Assert.That(authored[0].RectTransform.anchoredPosition, Is.EqualTo(new Vector2(first.x + delta.x, -first.y - delta.y)));
                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                Assert.That(authored[0].RectTransform.anchoredPosition, Is.EqualTo(new Vector2(first.x, -first.y)));
                Assert.That(authored[1].RectTransform.anchoredPosition, Is.EqualTo(new Vector2(second.x, -second.y)));
            }
            finally
            {
                window.Close();
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkillTreeRefresh_RestoresDefinitionAndSupportsUndo()
        {
            GameObject root = new("Refresh Test", typeof(RectTransform), typeof(SkillTreeAuthoring));
            SkillTreeAuthoring tree = root.GetComponent<SkillTreeAuthoring>();
            SkillTreeDefinition definition = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            SkillNodeDataSO data = ScriptableObject.CreateInstance<SkillNodeDataSO>();
            GameObject old = new("Old", typeof(RectTransform), typeof(SkillNodeAuthoring));
            old.transform.SetParent(root.transform, false);
            SetField(old.GetComponent<SkillNodeAuthoring>(), "_id", "old");
            SetField(tree, "_id", "old_tree");
            SkillNodeDefinition first = Node("first", 3, new[] { 2, 4, 6 });
            SetField(first, "_position", new Vector2(120f, -70f));
            data.CopyFrom(first);
            first.SetData(data);
            definition.Configure("restored", new[] { first, Node("second", 1, new[] { 9 }) },
                new[] { new SkillConnectionDefinition("first", "second", new NodeLevelRequirement("first", 2)) });
            tree.SetOutput(definition);
            MethodInfo refresh = typeof(SkillTreeAuthoringWindow).Assembly
                .GetType("RogueliteToolkit.SkillTree.Editor.SkillTreeAuthoringUtility")
                .GetMethod("TryRefreshFromDefinition", BindingFlags.Static | BindingFlags.NonPublic);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    object[] args = { tree, null };
                    Assert.That(refresh.Invoke(null, args), Is.EqualTo(true), args[1] as string);
                    SkillNodeAuthoring[] nodes = tree.GetComponentsInChildren<SkillNodeAuthoring>(true);
                    Assert.That(nodes.Length, Is.EqualTo(2));
                    Assert.That(nodes[0].Data, Is.SameAs(data));
                    Assert.That(nodes[0].RectTransform.anchoredPosition, Is.EqualTo(first.Position));
                    Assert.That(nodes[1].Id, Is.EqualTo("second"));
                    Assert.That(nodes[1].Costs[0], Is.EqualTo(9));
                    SkillConnectionAuthoring edge = tree.GetComponentInChildren<SkillConnectionAuthoring>();
                    Assert.That(edge.From, Is.SameAs(nodes[0]));
                    Assert.That(edge.To, Is.SameAs(nodes[1]));
                    Assert.That(edge.MinimumLevel, Is.EqualTo(2));
                    Assert.That(edge.Condition, Is.EqualTo(SkillConnectionCondition.MinimumLevel));
                }
                Undo.PerformUndo();
                Undo.PerformUndo();
                Assert.That(tree.Id, Is.EqualTo("old_tree"));
                Assert.That(tree.GetComponentsInChildren<SkillNodeAuthoring>().Single().Id, Is.EqualTo("old"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void SkillDescription_UsesLevelGainAndPreservesUnrelatedBraces()
        {
            StatGameplayEffect health = new();
            SetField(health, "_gainsPerLevel", new[] { 10f, 25f });
            StatGameplayEffect percent = new();
            SetField(percent, "_gainsPerLevel", new[] { -5f, 0f });
            SetField(percent, "_operation", StatModifierOperation.AddPercent);
            SkillNodeDefinition node = Node("description", 2, new[] { 1, 2 });
            SetField(node, "_description", "{0} Max HP | {1} Speed | {%} | {other}");
            SetField(node, "_statEffects", new[] { health, percent });
            Assert.That(node.GetDescriptionForLevel(1), Is.EqualTo("+ 10 Max HP | - 5 Speed | + 10% | {other}"));
            Assert.That(node.GetDescriptionForLevel(2), Is.EqualTo("+ 25 Max HP | 0 Speed | + 25% | {other}"));
            Assert.That(node.GetDescriptionForLevel(99), Is.EqualTo(node.GetDescriptionForLevel(2)));
            Assert.That(node.Description, Is.EqualTo("{0} Max HP | {1} Speed | {%} | {other}"));
        }

        [Test]
        public void NodeSize_SurvivesBakingAndDetachingData()
        {
            SkillTreeDefinition tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            SkillNodeDefinition node = Node("large", 1, new[] { 1 });
            Assert.That(node.NodeSize, Is.EqualTo(SkillNodeSize.Small));
            node.SetNodeSize(SkillNodeSize.Large);
            node.SetNodeScale(2f);
            SkillNodeDataSO data = ScriptableObject.CreateInstance<SkillNodeDataSO>();
            GameObject go = new("Node size test", typeof(RectTransform), typeof(SkillNodeAuthoring));
            try
            {
                data.CopyFrom(node);
                node.SetData(data);
                Assert.That(node.NodeSize, Is.EqualTo(SkillNodeSize.Large));
                Assert.That(node.NodeScale, Is.EqualTo(2f));
                SkillNodeAuthoring authored = go.GetComponent<SkillNodeAuthoring>();
                authored.SetData(data);
                Assert.That(authored.NodeSize, Is.EqualTo(SkillNodeSize.Large));
                Assert.That(tree.GetNodeDimensions(authored.NodeSize, authored.NodeScale),
                    Is.EqualTo(new Vector2(480f, 240f)));
                authored.SetData(null);
                Assert.That(authored.NodeScale, Is.EqualTo(2f));
                Assert.That(authored.NodeSize, Is.EqualTo(SkillNodeSize.Large));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(data);
                UnityEngine.Object.DestroyImmediate(tree);
            }
        }

        [Test]
        public void ExcelPaste_SelectsNamedColumnAndMapsLevels()
        {
            Assert.That(SkillNodeTablePaste.TryParse("Level\tValue\tPrice\r\n3\t-1.5\t30\r\n1\t2\t10\r\n",
                "Price", 10, out var prices, out _), Is.True);
            Assert.That(prices[3], Is.EqualTo(30m));
            Assert.That(prices[1], Is.EqualTo(10m));
            Assert.That(prices.ContainsKey(2), Is.False);
            Assert.That(SkillNodeTablePaste.TryParse("Value\n1,5\n-2.25", "Value", 2, out var values, out _), Is.True);
            Assert.That(values[1], Is.EqualTo(1.5m));
            Assert.That(values[2], Is.EqualTo(-2.25m));
            Assert.That(SkillNodeTablePaste.TryParse("10\n20", "Price", 2, out prices, out _), Is.True);
            Assert.That(prices[2], Is.EqualTo(20m));
        }

        [TestCase("Level\tPrice\n1\t10\n1\t20")]
        [TestCase("Level\tPrice\n0\t10")]
        [TestCase("Level\tPrice\n11\t10")]
        [TestCase("Level\tPrice\n1\t-1")]
        [TestCase("Level\tPrice\n1\t1.5")]
        [TestCase("Level\tPrice\n1\t2147483648")]
        [TestCase("Level\tPrice\n1\t#VALUE!")]
        [TestCase("Level\tValue\n1\t10")]
        [TestCase("Level\tPrice")]
        public void ExcelPaste_RejectsInvalidPriceTables(string text)
        {
            Assert.That(SkillNodeTablePaste.TryParse(text, "Price", 10, out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void UnevenPhases_PreserveBoundariesAndPricesThroughDataAndAuthoring()
        {
            SkillNodeDefinition node = Node("uneven", 1, Enumerable.Range(1, 12).ToArray());
            node.SetPhaseEndLevels(new[] { 3, 8, 12 });
            SkillNodeDataSO data = ScriptableObject.CreateInstance<SkillNodeDataSO>();
            GameObject go = new("Phase test", typeof(RectTransform), typeof(SkillNodeAuthoring));
            try
            {
                data.CopyFrom(node);
                node.SetData(data);
                Assert.That(node.MaxLevel, Is.EqualTo(12));
                Assert.That(node.PhaseCount, Is.EqualTo(3));
                Assert.That(node.GetPhase(0), Is.Zero);
                Assert.That(node.GetPhaseStartLevel(0), Is.Zero);
                Assert.That(node.GetLevelInPhase(0), Is.Zero);
                Assert.That(node.GetPhaseStartLevel(1), Is.EqualTo(3));
                Assert.That(node.GetPhaseStartLevel(2), Is.EqualTo(8));
                Assert.That(node.GetPhase(2), Is.EqualTo(0));
                Assert.That(node.GetPhase(3), Is.EqualTo(1));
                Assert.That(node.GetLevelInPhase(3), Is.Zero);
                Assert.That(node.GetPhaseLevelCount(1), Is.EqualTo(5));
                Assert.That(node.GetPhase(8), Is.EqualTo(2));
                Assert.That(node.GetLevelInPhase(12), Is.EqualTo(4));
                Assert.That(node.GetProgressLabel(12), Is.EqualTo("4/4 | 2 sao"));
                Assert.That(node.GetCostForLevel(12), Is.EqualTo(12));
                SkillNodeAuthoring authored = go.GetComponent<SkillNodeAuthoring>();
                authored.SetData(data);
                authored.SetData(null);
                Assert.That(authored.PhaseEndLevels, Is.EqualTo(new[] { 3, 8, 12 }));
                Assert.That(authored.MaxLevel, Is.EqualTo(12));
                Assert.That(authored.Costs.Count, Is.EqualTo(12));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void PhaseRanges_KeepLegacyEqualPhasesAndNormalizeInvalidEnds()
        {
            SkillNodeDefinition node = Node("legacy", 5, new int[10]);
            SetField(node, "_phaseCount", 2);
            Assert.That(node.MaxLevel, Is.EqualTo(10));
            Assert.That(node.GetPhase(5), Is.EqualTo(1));
            Assert.That(node.GetPhaseLevelCount(1), Is.EqualTo(5));
            node.SetPhaseEndLevels(new[] { 0, 0, -2 });
            Assert.That(node.PhaseEndLevels, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(node.MaxLevel, Is.EqualTo(3));
            Assert.That(node.GetPhase(100), Is.EqualTo(2));
            Assert.That(node.GetLevelInPhase(-1), Is.Zero);
        }

        [Test]
        public void CostResourceType_MigratesFirstLegacyResourceForAllLevels()
        {
            SkillNodeDefinition node = Node("resources", 3, new[] { 10, 20, 30 });
            SetField(node, "_costResourceTypes", new[] { SkillTreeResourcesType.Dirt, SkillTreeResourcesType.Gold });
            SkillNodeDataSO data = ScriptableObject.CreateInstance<SkillNodeDataSO>();
            try
            {
                data.CopyFrom(node);
                node.SetData(data);
                Assert.That(node.GetCostResourceTypeForLevel(1), Is.EqualTo(SkillTreeResourcesType.Dirt));
                Assert.That(node.GetCostResourceTypeForLevel(2), Is.EqualTo(SkillTreeResourcesType.Dirt));
                Assert.That(node.GetCostResourceTypeForLevel(3), Is.EqualTo(SkillTreeResourcesType.Dirt));
                Assert.That(node.GetCostForLevel(2), Is.EqualTo(20));
                Assert.Throws<ArgumentOutOfRangeException>(() => node.GetCostResourceTypeForLevel(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void SkillNodeColor_IsPreservedInNodeData()
        {
            Color color = new(0.2f, 0.4f, 0.8f, 1f);
            SkillNodeDefinition node = new();
            node.Configure("color", "color", null, null, Vector2.zero, 1,
                new[] { 1 }, RequirementMode.All, Array.Empty<StatGameplayEffect>(),
                Array.Empty<GameplayEffect>(), color: color);
            SkillNodeDataSO data = ScriptableObject.CreateInstance<SkillNodeDataSO>();
            try
            {
                data.CopyFrom(node);
                node.SetData(data);
                Assert.That(node.Color, Is.EqualTo(color));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(data);
            }
        }

        [Test]
        public void Migration_CreatesSeparateNodeAssetsAndIsIdempotent()
        {
            string folder = "Assets/SkillNodeMigrationTest_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            try
            {
                SkillTreeDefinition tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
                SkillNodeDefinition first = Node("first", 2, new[] { 10, 20 });
                SkillNodeDefinition second = Node("second", 1, new[] { 30 });
                tree.Configure("test", new[] { first, second }, Array.Empty<SkillConnectionDefinition>());
                AssetDatabase.CreateAsset(tree, folder + "/Tree.asset");
                SkillNodeAssetMigration.MigrateTree(tree);
                Assert.That(first.Data, Is.Not.Null);
                Assert.That(second.Data, Is.Not.SameAs(first.Data));
                Assert.That(first.Id, Is.EqualTo("first"));
                Assert.That(first.GetCostForLevel(2), Is.EqualTo(20));
                Assert.That(AssetDatabase.IsMainAsset(first.Data), Is.True);
                string path = AssetDatabase.GetAssetPath(first.Data);
                SkillNodeAssetMigration.MigrateTree(tree);
                Assert.That(AssetDatabase.GetAssetPath(first.Data), Is.EqualTo(path));
                Assert.That(AssetDatabase.FindAssets("t:SkillNodeDataSO", new[] { folder }).Length, Is.EqualTo(2));
                AssetDatabase.SaveAssets();
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        [Test]
        public void StatEffect_LevelGainsAccumulateAndPreserveLegacyFallback()
        {
            StatGameplayEffect effect = new();
            SetField(effect, "_valuePerStack", 10f);
            Assert.That(effect.GetValueAtLevel(3), Is.EqualTo(30f));
            SetField(effect, "_gainsPerLevel", new[] { 10f, 15f });
            Assert.That(effect.GetValueAtLevel(0), Is.Zero);
            Assert.That(effect.GetValueAtLevel(1), Is.EqualTo(10f));
            Assert.That(effect.GetValueAtLevel(2), Is.EqualTo(25f));
            Assert.That(effect.GetValueAtLevel(3), Is.EqualTo(35f));
        }

        [Test]
        public void EffectSource_CreatesDeterministicIds()
        {
            EffectSource card = new("card", "rapid_fire");
            EffectSource skill = new("skill", "damage_mastery");

            Assert.That(card.CreateEffectId(0), Is.EqualTo("card:rapid_fire:effect:0"));
            Assert.That(skill.CreateEffectId(2), Is.EqualTo("skill:damage_mastery:effect:2"));
        }

        [Test]
        public void SkillTreeAuthoring_CanAttachToSceneGameObject()
        {
            GameObject root = new("SkillTree Authoring Test", typeof(RectTransform));
            try
            {
                SkillTreeAuthoring authoring = root.AddComponent<SkillTreeAuthoring>();
                Assert.That(authoring, Is.Not.Null);
                Assert.That(root.GetComponent<SkillTreeAuthoring>(), Is.SameAs(authoring));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkillTreeAuthoringWindow_ShowsNodeColorField()
        {
            GameObject root = new("Color Field Test", typeof(RectTransform));
            SkillTreeAuthoring tree = root.AddComponent<SkillTreeAuthoring>();
            GameObject nodeObject = new("Node", typeof(RectTransform),
                typeof(SkillNodeAuthoring));
            nodeObject.transform.SetParent(root.transform, false);
            SetField(nodeObject.GetComponent<SkillNodeAuthoring>(), "_id", "node");
            SkillTreeAuthoringWindow window =
                ScriptableObject.CreateInstance<SkillTreeAuthoringWindow>();
            try
            {
                SetField(window, "_tree", tree);
                window.CreateGUI();
                Assert.That(window.rootVisualElement.Query<PropertyField>().ToList()
                    .Any(field => field.label == "Color"), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SkillTreeAuthoringWindow_CreatesGraphInterface()
        {
            GameObject root = new("Graph Test", typeof(RectTransform));
            SkillTreeAuthoring tree = root.AddComponent<SkillTreeAuthoring>();
            GameObject connections = new("Connections", typeof(RectTransform));
            connections.transform.SetParent(root.transform, false);
            GameObject nodes = new("Nodes", typeof(RectTransform));
            nodes.transform.SetParent(root.transform, false);
            GameObject nodeObject = new("Node", typeof(RectTransform),
                typeof(SkillNodeAuthoring));
            nodeObject.transform.SetParent(nodes.transform, false);
            SkillNodeAuthoring node = nodeObject.GetComponent<SkillNodeAuthoring>();
            SetField(tree, "_id", "graph_test");
            SetField(tree, "_connectionsRoot", (RectTransform)connections.transform);
            SetField(tree, "_nodesRoot", (RectTransform)nodes.transform);
            SetField(node, "_id", "node_01");
            SetField(node, "_displayName", "Node 01");
            SetField(node, "_costs", new[] { 1 });
            SetField(node, "_statEffects", new[] { new StatGameplayEffect() });
            SetField(node, "_effects", new GameplayEffect[] { new RecordingEffect() });

            SkillTreeAuthoringWindow window =
                ScriptableObject.CreateInstance<SkillTreeAuthoringWindow>();
            try
            {
                SetField(window, "_tree", tree);
                Assert.DoesNotThrow(window.CreateGUI);
                Assert.That(window.rootVisualElement.childCount, Is.GreaterThanOrEqualTo(2));
                List<ToolbarButton> buttons =
                    window.rootVisualElement.Query<ToolbarButton>().ToList();
                Assert.That(buttons.Exists(button => button.text == "Collapse All"), Is.True);
                Assert.That(buttons.Exists(button => button.text == "Expand All"), Is.True);

                Node graphNode = window.rootVisualElement.Query<Node>().First();
                Vector2 stablePosition = new(517f, -239f);
                graphNode.SetPosition(new Rect(stablePosition, new Vector2(400f, 500f)));
                for (int repeat = 0; repeat < 5; repeat++)
                {
                    Invoke(window, "SetAllNodesExpanded", false);
                    Label collapsedSummary = graphNode.Q<Label>("collapsed-value-price");
                    Assert.That(collapsedSummary.text, Does.Contain("Price: 1"));
                    Assert.That(collapsedSummary.text, Does.Contain("Value: 0"));
                    Assert.That(collapsedSummary.GetFirstAncestorOfType<ScrollView>(), Is.Not.Null);
                    Assert.That(graphNode.GetPosition().position, Is.EqualTo(stablePosition));
                    Invoke(window, "SetAllNodesExpanded", true);
                    foreach (Foldout foldout in graphNode.Query<Foldout>().ToList())
                        foldout.value = !foldout.value;
                    Invoke(graphNode, "ApplyExpandedLayout");
                    Assert.That(graphNode.GetPosition().position, Is.EqualTo(stablePosition));
                    Assert.That(graphNode.style.left.value.value, Is.EqualTo(stablePosition.x));
                    Assert.That(graphNode.style.top.value.value, Is.EqualTo(stablePosition.y));
                    Assert.That(graphNode.style.width.value.value, Is.GreaterThan(0f));
                }
                EnumField incomingMode = graphNode.Query<EnumField>(
                    className: "skill-node-incoming-field").First();
                VisualElement outputGroup = incomingMode.parent;
                Assert.That(outputGroup.ClassListContains("skill-node-output-group"),
                    Is.True);
                Assert.That(outputGroup.childCount, Is.EqualTo(2));
                Assert.That(outputGroup[0], Is.SameAs(incomingMode),
                    "Incoming Mode must sit immediately before the output port.");
                Assert.That(outputGroup[1], Is.SameAs(graphNode.Query<Port>().ToList()
                    .Single(port => port.direction == Direction.Output)));
                Port inputPort = graphNode.Query<Port>().ToList()
                    .Single(port => port.direction == Direction.Input);
                Port outputPort = graphNode.Query<Port>().ToList()
                    .Single(port => port.direction == Direction.Output);
                Assert.That(inputPort.portName, Is.EqualTo("IN"));
                Assert.That(outputPort.portName, Is.EqualTo("OUT"));
                Assert.That(inputPort.portColor, Is.Not.EqualTo(outputPort.portColor));

                Invoke(window, "SetAllNodesExpanded", false);
                Assert.That(node.EditorExpanded, Is.False);
                Assert.That(graphNode.expanded, Is.False);
                Assert.That(graphNode.extensionContainer.style.display.value,
                    Is.EqualTo(DisplayStyle.None));
                Invoke(window, "SetAllNodesExpanded", true);
                Assert.That(node.EditorExpanded, Is.True);
                Assert.That(graphNode.expanded, Is.True);
                Assert.That(graphNode.extensionContainer.style.display.value,
                    Is.EqualTo(DisplayStyle.Flex));
                Assert.That(graphNode.Query<Foldout>().ToList()
                    .Where(foldout => !foldout.ClassListContains("skill-effect-advanced"))
                    .All(foldout => foldout.value), Is.True,
                    "Expand All must also open every node subsection.");
                node.SetEditorSize(new Vector2(305f, 260f));
                Assert.That(node.EditorSize.x, Is.EqualTo(305f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Card_AddRemoveClearAndRestore_ReconstructsWithoutDuplicates()
        {
            StatDefinition stat = CreateStat("damage");
            StatGameplayEffect statEffect = new();
            SetField(statEffect, "_stat", stat);
            SetField(statEffect, "_operation", StatModifierOperation.AddFlat);
            SetField(statEffect, "_valuePerStack", 10f);

            CardDefinition card = ScriptableObject.CreateInstance<CardDefinition>();
            SetField(card, "_id", "rapid_fire");
            SetField(card, "_maxStacks", 3);
            SetField(card, "_statEffects", new[] { statEffect });
            CardDatabase database = ScriptableObject.CreateInstance<CardDatabase>();
            SetField(database, "_cards", new[] { card });

            GameObject owner = new("Card Test");
            StatSet stats = owner.AddComponent<StatSet>();
            GameplayEffectContext context = new GameplayEffectContext().Register(stats);
            CardCollection cards = new();
            List<CardStackData> save = new();

            try
            {
                Assert.That(cards.Add(card, context, 2), Is.True);
                Assert.That(stats.GetValue(stat), Is.EqualTo(10f * 2f));
                Assert.That(cards.Remove(card, context), Is.True);
                Assert.That(stats.GetValue(stat), Is.EqualTo(10f));
                cards.WriteSaveData(save);

                cards.Clear(database, context);
                Assert.That(stats.GetValue(stat), Is.Zero);
                cards.Restore(save, database, context);
                Assert.That(stats.GetValue(stat), Is.EqualTo(10f));

                card.NotifyStacksChanged(context, 0, 1);
                Assert.That(stats.GetValue(stat), Is.EqualTo(10f),
                    "AddOrUpdate with a deterministic ID must not duplicate the modifier.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(database);
                UnityEngine.Object.DestroyImmediate(card);
                UnityEngine.Object.DestroyImmediate(stat);
            }
        }

        [Test]
        public void SkillTree_AllRequirementsPurchaseAndRestore_WorkAcrossLevels()
        {
            RecordingEffect recording = new();
            SkillNodeDefinition rootA = Node("a", 2, new[] { 1, 2 }, recording);
            SkillNodeDefinition rootB = Node("b", 2, new[] { 1, 1 });
            SkillNodeDefinition target = Node("target", 1, new[] { 3 });
            target.Configure("target", "target", null, null, Vector2.zero, 1,
                new[] { 3 }, RequirementMode.All,
                Array.Empty<StatGameplayEffect>(), Array.Empty<GameplayEffect>());
            SkillTreeDefinition definition = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            definition.Configure("mastery", new[] { rootA, rootB, target }, new[]
            {
                new SkillConnectionDefinition("a", "target",
                    new NodeUnlockedRequirement("a")),
                new SkillConnectionDefinition("b", "target",
                    new NodeLevelRequirement("b", 2))
            });
            TestWallet wallet = new(10);
            GameplayEffectContext context = new GameplayEffectContext().Register<ISkillPointWallet>(wallet);
            SkillTreeState state = new(definition);

            try
            {
                Assert.That(state.CanPurchase(target, context), Is.False);
                Assert.That(state.Purchase(rootA, context), Is.True);
                Assert.That(recording.LastPrevious, Is.Zero);
                Assert.That(recording.LastCurrent, Is.EqualTo(1));
                Assert.That(state.Purchase(rootB, context), Is.True);
                Assert.That(state.CanPurchase(target, context), Is.False);
                Assert.That(state.Purchase(rootB, context), Is.True);
                Assert.That(state.CanPurchase(target, context), Is.True);
                Assert.That(state.Purchase(target, context), Is.True);
                Assert.That(wallet.Points, Is.EqualTo(4));

                SkillTreeSaveData save = new();
                state.WriteSaveData(save);
                RecordingEffect restoredRecording = new();
                rootA.Configure("a", "a", null, null, Vector2.zero, 2,
                    new[] { 1, 2 }, RequirementMode.All,
                    Array.Empty<StatGameplayEffect>(), new GameplayEffect[] { restoredRecording });
                SkillTreeState restored = new(definition);
                restored.Restore(save, context);
                Assert.That(restored.GetLevel(rootA), Is.EqualTo(1));
                Assert.That(restored.GetLevel(rootB), Is.EqualTo(2));
                Assert.That(restored.GetLevel(target), Is.EqualTo(1));
                Assert.That(restoredRecording.LastPrevious, Is.Zero);
                Assert.That(restoredRecording.LastCurrent, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        private static SkillNodeDefinition Node(
            string id, int maxLevel, int[] costs, GameplayEffect effect = null)
        {
            SkillNodeDefinition node = new();
            node.Configure(id, id, null, null, Vector2.zero, maxLevel, costs,
                RequirementMode.All, Array.Empty<StatGameplayEffect>(),
                effect != null ? new[] { effect } : Array.Empty<GameplayEffect>());
            return node;
        }

        private static StatDefinition CreateStat(string id)
        {
            StatDefinition stat = ScriptableObject.CreateInstance<StatDefinition>();
            SetField(stat, "_id", id);
            SetField(stat, "_defaultBaseValue", 0f);
            return stat;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(
                name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {name}");
            field.SetValue(target, value);
        }

        private static object Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {name}");
            return method.Invoke(target, arguments);
        }

        [Serializable]
        private sealed class RecordingEffect : GameplayEffect
        {
            public int LastPrevious { get; private set; }
            public int LastCurrent { get; private set; }

            public override void OnValueChanged(
                GameplayEffectContext context, EffectSource source,
                int effectIndex, int previousValue, int currentValue)
            {
                LastPrevious = previousValue;
                LastCurrent = currentValue;
            }
        }

        private sealed class TestWallet : ISkillPointWallet
        {
            public int Points { get; private set; }

            public TestWallet(int points) => Points = points;
            public bool CanSpend(int amount, SkillTreeResourcesType resourceType = SkillTreeResourcesType.None) => amount >= 0 && Points >= amount;

            public bool TrySpend(int amount, SkillTreeResourcesType resourceType = SkillTreeResourcesType.None)
            {
                if (!CanSpend(amount))
                    return false;
                Points -= amount;
                return true;
            }
        }
    }
}
