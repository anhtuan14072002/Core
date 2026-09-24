using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using RogueliteToolkit.Effects;
using RogueliteToolkit.Stats;
using RogueliteToolkit.SkillTree.Authoring;
using UnityEditor;
using UnityEngine;

namespace RogueliteToolkit.SkillTree.Editor
{
    [CustomEditor(typeof(SkillNodeAuthoring))]
    public sealed class SkillNodeAuthoringEditor : UnityEditor.Editor
    {
        private SkillNodeDataSO _observedData;
        private int _observedDirtyCount = -1;
        private Vector2 _observedPosition;
        private double _nextRefreshCheck;

        private void OnEnable()
        {
            EditorApplication.update += RefreshWhenDataChanges;
            EditorApplication.projectChanged += Repaint;
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RefreshWhenDataChanges;
            EditorApplication.projectChanged -= Repaint;
            Undo.undoRedoPerformed -= Repaint;
        }

        private void RefreshWhenDataChanges()
        {
            if (target == null || EditorApplication.timeSinceStartup < _nextRefreshCheck)
                return;
            _nextRefreshCheck = EditorApplication.timeSinceStartup + 0.1d;
            SkillNodeDataSO data = ((SkillNodeAuthoring)target).Data;
            int dirtyCount = data != null ? EditorUtility.GetDirtyCount(data) : 0;
            Vector2 position = ((SkillNodeAuthoring)target).RectTransform.anchoredPosition;
            if (data == _observedData && dirtyCount == _observedDirtyCount && position == _observedPosition)
                return;
            _observedPosition = position;
            _observedData = data;
            _observedDirtyCount = dirtyCount;
            // OnInspectorGUI reads the same asset again, including edits made in
            // another (possibly locked) Inspector, without copying stale local fields.
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            SkillNodeAuthoring node = (SkillNodeAuthoring)target;
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

            EditorGUI.BeginChangeCheck();
            SkillNodeDataSO data = (SkillNodeDataSO)EditorGUILayout.ObjectField(
                "Data", node.Data, typeof(SkillNodeDataSO), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(node, "Assign Skill Node Data");
                // Detaching keeps the current asset values as the local fallback.
                node.SetData(data);
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                EditorUtility.SetDirty(node);
                serializedObject.Update();
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_incomingRequirementMode"));
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();

            if (node.Data != null)
            {
                EditorGUILayout.HelpBox(
                    "These values come from the referenced DataSO. Editing them updates the asset and every node that uses it.",
                    MessageType.Info);
                using (SerializedObject asset = new(node.Data))
                {
                    asset.Update();
                    SkillNodeDataInspector.Draw(asset, node);
                    asset.ApplyModifiedProperties();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No DataSO assigned. Using this node's local data.", MessageType.Info);
                serializedObject.Update();
                SkillNodeDataInspector.Draw(serializedObject, node);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    internal sealed class SkillNodeEditWindow : EditorWindow
    {
        [SerializeField] private SkillNodeAuthoring _node;
        private Vector2 _scroll;

        internal static void Open(SkillNodeAuthoring node)
        {
            SkillNodeEditWindow window = GetWindow<SkillNodeEditWindow>("Edit Skill Node");
            window._node = node;
            window._scroll = Vector2.zero;
            window.minSize = new Vector2(440f, 480f);
            window.Show();
            window.Focus();
        }

        private void OnEnable() => Undo.undoRedoPerformed += Repaint;
        private void OnDisable() => Undo.undoRedoPerformed -= Repaint;
        private void OnInspectorUpdate() => Repaint();

        private void OnGUI()
        {
            if (_node == null)
            {
                EditorGUILayout.HelpBox("Select Edit on a skill node to open its data.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_node.DisplayName ?? _node.Id, EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (SerializedObject data = new(_node.DataTarget))
            {
                data.Update();
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(data.FindProperty("m_Script"));
                SkillNodeDataInspector.Draw(data, _node);
                data.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
        }
    }

    internal static class SkillNodeDataInspector
    {
        internal static void Draw(SerializedObject data, SkillNodeAuthoring authoring = null)
        {
            // Allocate one full-width row, then split it explicitly. Nested GUILayout
            // groups can otherwise shrink the fields to their minimum width.
            Rect header = EditorGUILayout.GetControlRect(false, 96f, GUILayout.ExpandWidth(true));
            Rect preview = new(header.x, header.y, 112f, header.height);
            DrawIconPreview(data.FindProperty("_icon"), preview);
            float fieldX = preview.xMax + 6f;
            Rect field = new(fieldX, header.y, Mathf.Max(1f, header.xMax - fieldX), EditorGUIUtility.singleLineHeight);
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Min(90f, field.width * 0.4f);
            EditorGUI.PropertyField(field, data.FindProperty("_id"), new GUIContent("Id"));
            field.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(field, data.FindProperty("_displayName"));
            field.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(field, data.FindProperty("_icon"), new GUIContent("Icon"));
            EditorGUIUtility.labelWidth = previousLabelWidth;
            SerializedProperty nodeSize = data.FindProperty("_nodeSize");
            nodeSize.intValue = EditorGUILayout.Popup("Node Size", nodeSize.intValue, new[] { "Small", "Large" });
            EditorGUILayout.PropertyField(data.FindProperty("_nodeScale"), new GUIContent("Node Scale"));
            EditorGUILayout.PropertyField(data.FindProperty("_color"));
            EditorGUILayout.PropertyField(data.FindProperty("_nodeColor"), new GUIContent("Node Color"));
            if (authoring != null) DrawPosition(authoring);
            else
                foreach (SkillNodeAuthoring node in UnityEngine.Object.FindObjectsByType<SkillNodeAuthoring>(
                                 FindObjectsInactive.Include, FindObjectsSortMode.None)
                             .Where(node => node.Data == data.targetObject))
                    DrawPosition(node);
            EditorGUILayout.PropertyField(data.FindProperty("_description"));
            EditorGUILayout.PropertyField(data.FindProperty("_instantFreeUpgrade"));
            EditorGUILayout.Space();
            DrawPhases(data);
            int total = GetMaxLevel(data);
            DrawStats(data, total);
            DrawEffects(data);
            DrawPrices(data, total);
        }

        private static void DrawPosition(SkillNodeAuthoring node)
        {
            EditorGUI.BeginChangeCheck();
            Vector2 position = EditorGUILayout.Vector2Field(new GUIContent("Position", node.name),
                node.RectTransform.anchoredPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(node.RectTransform, "Move Skill Node");
                node.RectTransform.anchoredPosition = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(node.RectTransform);
                EditorUtility.SetDirty(node.RectTransform);
            }
        }

        private static void DrawIconPreview(SerializedProperty icon, Rect frame)
        {
            GUI.Box(frame, GUIContent.none, EditorStyles.helpBox);
            Sprite sprite = icon.objectReferenceValue as Sprite;
            if (sprite == null)
            {
                GUIStyle placeholder = new(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(frame, "No Icon", placeholder);
                return;
            }

            // AssetPreview handles cropped/packed sprites without showing the entire atlas.
            Texture2D preview = AssetPreview.GetAssetPreview(sprite);
            if (preview == null) preview = AssetPreview.GetMiniThumbnail(sprite);
            if (preview != null)
                GUI.DrawTexture(new Rect(frame.x + 4f, frame.y + 4f, frame.width - 8f, frame.height - 8f),
                    preview, ScaleMode.ScaleToFit, true);
            GUI.Label(frame, new GUIContent(string.Empty, sprite.name));
        }

        private static void DrawIconDimensions(SerializedProperty property, string label)
        {
            EditorGUI.BeginChangeCheck();
            Vector2 value = EditorGUILayout.Vector2Field(label, property.vector2Value);
            if (EditorGUI.EndChangeCheck() && float.IsFinite(value.x) && float.IsFinite(value.y))
                property.vector2Value = new Vector2(Mathf.Max(1f, value.x), Mathf.Max(1f, value.y));
        }

        private static int GetMaxLevel(SerializedObject data)
        {
            SerializedProperty ends = data.FindProperty("_phaseEndLevels");
            return ends.arraySize > 0
                ? ends.GetArrayElementAtIndex(ends.arraySize - 1).intValue
                : Mathf.Max(1, data.FindProperty("_maxLevel").intValue) *
                  Mathf.Clamp(data.FindProperty("_phaseCount").intValue, 1, 4);
        }

        private static void DrawPhases(SerializedObject data)
        {
            SerializedProperty ends = data.FindProperty("_phaseEndLevels");
            int legacySize = Mathf.Max(1, data.FindProperty("_maxLevel").intValue);
            int count = ends.arraySize > 0
                ? ends.arraySize
                : Mathf.Clamp(data.FindProperty("_phaseCount").intValue, 1, 4);
            int[] values = Enumerable.Range(0, count).Select(i => ends.arraySize > 0
                ? ends.GetArrayElementAtIndex(i).intValue
                : (i + 1) * legacySize).ToArray();
            EditorGUILayout.LabelField("Phases", EditorStyles.boldLabel);
            bool changed = false;
            for (int i = 0; i < values.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Phase {i + 1} · {i + 1} sao", GUILayout.Width(106));
                EditorGUILayout.LabelField("From", GUILayout.Width(32));
                // Level zero is the initial state. Reaching an end starts the next phase.
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.IntField(i == 0 ? 0 : values[i - 1], GUILayout.MinWidth(35));
                EditorGUILayout.LabelField(
                    new GUIContent("To",
                        "Reaching this level starts the next phase; the final phase includes Max Level."),
                    GUILayout.Width(20));
                EditorGUI.BeginChangeCheck();
                int end = EditorGUILayout.DelayedIntField(values[i], GUILayout.MinWidth(35));
                if (EditorGUI.EndChangeCheck())
                {
                    values[i] = Mathf.Max(i == 0 ? 1 : values[i - 1] + 1, end);
                    changed = true;
                }

                using (new EditorGUI.DisabledScope(values.Length == 1))
                {
                    if (GUILayout.Button("−", GUILayout.Width(24)))
                    {
                        values = values.Where((_, index) => index != i).ToArray();
                        changed = true;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Phase"))
            {
                int last = values[values.Length - 1];
                Array.Resize(ref values, values.Length + 1);
                values[values.Length - 1] = last + 1;
                changed = true;
            }

            if (changed)
            {
                ends.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = Mathf.Max(i == 0 ? 1 : values[i - 1] + 1, values[i]);
                    ends.GetArrayElementAtIndex(i).intValue = values[i];
                }

                ResizePrices(data.FindProperty("_costs"), values[values.Length - 1]);
                SerializedProperty additional = data.FindProperty("_additionalCosts");
                for (int i = 0; i < additional.arraySize; i++)
                    ResizePrices(additional.GetArrayElementAtIndex(i).FindPropertyRelative("_costs"),
                        values[values.Length - 1]);
            }

            EditorGUILayout.LabelField("Max Level", GetMaxLevel(data).ToString());
        }

        private static void ResizePrices(SerializedProperty prices, int count)
        {
            int oldCount = prices.arraySize;
            double last = oldCount > 0 ? prices.GetArrayElementAtIndex(oldCount - 1).doubleValue : 0;
            prices.arraySize = count;
            for (int i = oldCount; i < count; i++) prices.GetArrayElementAtIndex(i).doubleValue = last;
        }

        private static void DrawPasteButton(SerializedProperty array, int count, string column, float fallback)
        {
            if (!GUILayout.Button(new GUIContent("Paste " + column + " from Excel",
                    "Copy cells including Level and " + column +
                    " headers, or a single column of numbers. Ctrl+Z undoes the paste."))) return;
            if (!SkillNodeTablePaste.TryParse(EditorGUIUtility.systemCopyBuffer, column, count,
                    out Dictionary<int, double> values, out string error))
            {
                EditorUtility.DisplayDialog("Paste " + column + " failed", error, "OK");
                return;
            }

            // Validate the entire clipboard before touching serialized data.
            if (column == "Price") ResizePrices(array, count);
            else
            {
                int oldSize = array.arraySize;
                array.arraySize = Mathf.Max(oldSize, count);
                for (int i = oldSize; i < array.arraySize; i++)
                    array.GetArrayElementAtIndex(i).floatValue = fallback;
            }

            foreach (KeyValuePair<int, double> item in values)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(item.Key - 1);
                if (column == "Price") element.doubleValue = item.Value;
                else element.floatValue = (float)item.Value;
            }

            array.isExpanded = true;
        }

        internal static void AddResourceColumn(SerializedObject data, int count)
        {
            SerializedProperty additional = data.FindProperty("_additionalCosts");
            int index = additional.arraySize++;
            SerializedProperty column = additional.GetArrayElementAtIndex(index);
            column.FindPropertyRelative("_resource").intValue = (int)SkillTreeResourcesType.None;
            SerializedProperty prices = column.FindPropertyRelative("_costs");
            prices.arraySize = count;
            for (int i = 0; i < count; i++) prices.GetArrayElementAtIndex(i).doubleValue = 0;
            data.FindProperty("_costs").isExpanded = true;
        }

        private static void DrawPrices(SerializedObject data, int count)
        {
            SerializedProperty primary = data.FindProperty("_costs");
            SerializedProperty additional = data.FindProperty("_additionalCosts");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("+", "Add a resource price column"), GUILayout.Width(24)))
                AddResourceColumn(data, count);
            EditorGUILayout.EndHorizontal();

            int columns = additional.arraySize + 1;
            SerializedProperty[] prices = new SerializedProperty[columns];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(64f);
            int remove = -1;
            for (int column = 0; column < columns; column++)
            {
                SerializedProperty entry = column == 0 ? null : additional.GetArrayElementAtIndex(column - 1);
                SerializedProperty resource = column == 0
                    ? data.FindProperty("_costResourceType")
                    : entry.FindPropertyRelative("_resource");
                prices[column] = column == 0 ? primary : entry.FindPropertyRelative("_costs");
                EditorGUILayout.BeginVertical(GUILayout.MinWidth(0));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(resource, GUIContent.none, GUILayout.MinWidth(0));
                if (column > 0 && GUILayout.Button(new GUIContent("−", "Remove this resource column"),
                        GUILayout.Width(20)))
                    remove = column - 1;
                EditorGUILayout.EndHorizontal();
                DrawPasteButton(prices[column], count, "Price", 0f);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            primary.isExpanded = EditorGUILayout.Foldout(primary.isExpanded, $"Price ({count} levels)", true);
            if (primary.isExpanded)
            {
                for (int level = 0; level < count; level++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Level {level + 1}", GUILayout.Width(60f));
                    for (int column = 0; column < columns; column++)
                    {
                        SerializedProperty costs = prices[column];
                        double old = level < costs.arraySize ? costs.GetArrayElementAtIndex(level).doubleValue : 0;
                        EditorGUI.BeginChangeCheck();
                        double value = EditorGUILayout.DoubleField(old, GUILayout.MinWidth(0));
                        if (EditorGUI.EndChangeCheck())
                        {
                            ResizePrices(costs, count);
                            costs.GetArrayElementAtIndex(level).doubleValue = Math.Max(0, value);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (remove >= 0) additional.DeleteArrayElementAtIndex(remove);
        }

        private static void DrawStats(SerializedObject data, int count)
        {
            SerializedProperty stats = data.FindProperty("_statEffects");
            for (int i = 0; i < stats.arraySize; i++)
            {
                SerializedProperty effect = stats.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                DrawStatDropdown(effect.FindPropertyRelative("_stat"));
                bool remove = GUILayout.Button("−", GUILayout.Width(24));
                EditorGUILayout.EndHorizontal();
                if (remove)
                {
                    stats.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }

                SerializedProperty operation = effect.FindPropertyRelative("_operation");
                EditorGUI.BeginChangeCheck();
                bool percentage = EditorGUILayout.Toggle("Is Percentage",
                    operation.intValue == (int)StatModifierOperation.AddPercent);
                if (EditorGUI.EndChangeCheck())
                    operation.intValue =
                        (int)(percentage ? StatModifierOperation.AddPercent : StatModifierOperation.AddFlat);
                if (operation.intValue != (int)StatModifierOperation.AddFlat &&
                    operation.intValue != (int)StatModifierOperation.AddPercent)
                    EditorGUILayout.PropertyField(operation, new GUIContent("Operation"));
                SerializedProperty gains = effect.FindPropertyRelative("_gainsPerLevel");
                float fallback = effect.FindPropertyRelative("_valuePerStack").floatValue;
                DrawPasteButton(gains, count, "Value", fallback);
                gains.isExpanded = EditorGUILayout.Foldout(gains.isExpanded, $"Value ({count} levels)", true);
                if (gains.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    for (int level = 0; level < count; level++)
                    {
                        float old = level < gains.arraySize ? gains.GetArrayElementAtIndex(level).floatValue : fallback;
                        EditorGUI.BeginChangeCheck();
                        float value = EditorGUILayout.FloatField($"Level {level + 1}", old);
                        if (EditorGUI.EndChangeCheck())
                        {
                            int oldSize = gains.arraySize;
                            gains.arraySize = Mathf.Max(count, oldSize);
                            for (int j = oldSize; j < gains.arraySize; j++)
                                gains.GetArrayElementAtIndex(j).floatValue = fallback;
                            gains.GetArrayElementAtIndex(level).floatValue = value;
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Stat"))
            {
                int index = stats.arraySize++;
                SerializedProperty effect = stats.GetArrayElementAtIndex(index);
                effect.FindPropertyRelative("_stat").objectReferenceValue = null;
                effect.FindPropertyRelative("_operation").enumValueIndex = 0;
                effect.FindPropertyRelative("_valuePerStack").floatValue = 0;
                effect.FindPropertyRelative("_priority").intValue = 0;
                SerializedProperty gains = effect.FindPropertyRelative("_gainsPerLevel");
                gains.arraySize = count;
                for (int j = 0; j < count; j++) gains.GetArrayElementAtIndex(j).floatValue = 0;
                gains.isExpanded = true;
            }
        }

        private static void DrawStatDropdown(SerializedProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, new GUIContent("Stat"));
            StatDefinition current = property.objectReferenceValue as StatDefinition;
            if (!EditorGUI.DropdownButton(rect, new GUIContent(current != null ? current.name : "None"),
                    FocusType.Keyboard)) return;
            UnityEngine.Object target = property.serializedObject.targetObject;
            string path = property.propertyPath;
            GenericMenu menu = new();

            void Add(string label, StatDefinition stat)
            {
                menu.AddItem(new GUIContent(label), current == stat, () =>
                {
                    if (target == null) return;
                    using SerializedObject serialized = new(target);
                    serialized.Update();
                    SerializedProperty selected = serialized.FindProperty(path);
                    if (selected == null) return;
                    selected.objectReferenceValue = stat;
                    serialized.ApplyModifiedProperties();
                });
            }

            Add("None", null);
            foreach (StatDefinition stat in AssetDatabase.FindAssets("t:StatDefinition", new[] { "Assets" })
                         .Select(guid =>
                             AssetDatabase.LoadAssetAtPath<StatDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                         .Where(stat => stat != null).OrderBy(stat => stat.name))
                Add(stat.name + " (" + stat.Id + ")", stat);
            menu.DropDown(rect);
        }

        private static void DrawEffects(SerializedObject data)
        {
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
            SerializedProperty effects = data.FindProperty("_effects");
            for (int i = 0; i < effects.arraySize; i++)
            {
                SerializedProperty effect = effects.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                string label = effect.managedReferenceValue == null
                    ? "None"
                    : ObjectNames.NicifyVariableName(effect.managedReferenceValue.GetType().Name);
                Rect rect = EditorGUILayout.GetControlRect();
                if (EditorGUI.DropdownButton(rect, new GUIContent(label), FocusType.Keyboard))
                    ShowEffectMenu(rect, effect);
                bool remove = GUILayout.Button("−", GUILayout.Width(24));
                EditorGUILayout.EndHorizontal();
                if (remove)
                {
                    effects.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndVertical();
                    break;
                }

                if (effect.managedReferenceValue != null)
                {
                    SerializedProperty child = effect.Copy();
                    SerializedProperty end = child.GetEndProperty();
                    bool enterChildren = true;
                    while (child.NextVisible(enterChildren) && !SerializedProperty.EqualContents(child, end))
                    {
                        EditorGUILayout.PropertyField(child, true);
                        enterChildren = false;
                    }
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Effect"))
                effects.GetArrayElementAtIndex(effects.arraySize++).managedReferenceValue = null;
        }

        private static void ShowEffectMenu(Rect rect, SerializedProperty property)
        {
            UnityEngine.Object target = property.serializedObject.targetObject;
            string path = property.propertyPath;
            Type current = property.managedReferenceValue?.GetType();
            GenericMenu menu = new();

            void Add(string label, Type type)
            {
                menu.AddItem(new GUIContent(label), current == type, () =>
                {
                    if (target == null) return;
                    using SerializedObject serialized = new(target);
                    serialized.Update();
                    SerializedProperty selected = serialized.FindProperty(path);
                    if (selected == null) return;
                    selected.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
                    serialized.ApplyModifiedProperties();
                });
            }

            Add("None", null);
            menu.AddSeparator("");
            foreach (Type type in TypeCache.GetTypesDerivedFrom<GameplayEffect>())
            {
                if (type == typeof(StatGameplayEffect) || type.IsAbstract || type.IsGenericType ||
                    !type.IsSerializable || (!type.IsPublic && !type.IsNestedPublic) ||
                    type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                Add(ObjectNames.NicifyVariableName(type.Name), type);
            }

            menu.DropDown(rect);
        }
    }

    public static class SkillNodeTablePaste
    {
        // Excel copies cells as tab-separated text. Headers may include both data columns.
        public static bool TryParse(string text, string column, int maxLevel,
            out Dictionary<int, double> values, out string error)
        {
            values = new Dictionary<int, double>();
            error = null;
            string[] lines = (text ?? string.Empty).Replace("\r", string.Empty).Split('\n');
            int first = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
            if (first < 0)
            {
                error = "Clipboard is empty. Copy the Excel cells first.";
                return false;
            }

            string[] header = lines[first].Split('\t').Select(cell => cell.Trim().Trim('"', '\uFEFF')).ToArray();
            int valueColumn = Array.FindIndex(header,
                cell => string.Equals(cell, column, StringComparison.OrdinalIgnoreCase));
            int levelColumn = Array.FindIndex(header,
                cell => string.Equals(cell, "Level", StringComparison.OrdinalIgnoreCase));
            bool hasHeader = valueColumn >= 0;
            if (!hasHeader)
            {
                if (header.Length != 1 || !TryNumber(header[0], out _))
                {
                    error = "Expected Level and " + column +
                            " headers, or a single numeric column. Copy the cells directly from Excel.";
                    return false;
                }

                valueColumn = 0;
                levelColumn = -1;
            }

            int sequentialLevel = 1;
            for (int row = first + (hasHeader ? 1 : 0); row < lines.Length; row++)
            {
                if (string.IsNullOrWhiteSpace(lines[row])) continue;
                string[] cells = lines[row].Split('\t');
                int level = sequentialLevel++;
                if (valueColumn >= cells.Length || (levelColumn >= 0 &&
                                                    (levelColumn >= cells.Length ||
                                                     !int.TryParse(cells[levelColumn].Trim(), out level))))
                {
                    error = $"Row {row + 1}: missing data or invalid Level.";
                    return false;
                }

                if (level < 1 || level > maxLevel)
                {
                    error =
                        $"Row {row + 1}: Level must be between 1 and {maxLevel}. Adjust Phases first if you need more levels.";
                    return false;
                }

                if (values.ContainsKey(level))
                {
                    error = $"Row {row + 1}: duplicate Level {level}.";
                    return false;
                }

                if (!TryNumber(cells[valueColumn], out double value) || double.IsNaN(value) ||
                    double.IsInfinity(value) ||
                    (column == "Price" ? value < 0 : Math.Abs(value) > float.MaxValue))
                {
                    error = $"Row {row + 1}: invalid {column}. " + (column == "Price"
                        ? "Price must be a finite non-negative number."
                        : "Copy numeric results, without units or percent signs.");
                    return false;
                }

                values.Add(level, value);
            }

            if (values.Count == 0)
            {
                error = "The table has no data rows.";
                return false;
            }

            return true;
        }

        private static bool TryNumber(string cell, out double value)
        {
            string number = cell.Trim().Trim('"');
            // Accept Excel decimal commas as well as decimal points; no grouping separators.
            if (number.Contains(',') && !number.Contains('.')) number = number.Replace(',', '.');
            return double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }

    [CustomEditor(typeof(SkillNodeDataSO))]
    public sealed class SkillNodeDataEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            SkillNodeDataInspector.Draw(serializedObject);
            serializedObject.ApplyModifiedProperties();
        }
    }
}