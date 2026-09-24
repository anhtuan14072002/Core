using UnityEditor;
using UnityEngine;
using RogueliteToolkit.SkillTree.Authoring;

namespace RogueliteToolkit.SkillTree.Editor
{
    [CustomEditor(typeof(SkillConnectionAuthoring))]
    public sealed class SkillConnectionAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_from"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_to"));
            SerializedProperty condition = serializedObject.FindProperty("_condition");
            EditorGUILayout.PropertyField(condition);
            if ((SkillConnectionCondition)condition.enumValueIndex ==
                SkillConnectionCondition.MinimumLevel)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("_minimumLevel"));
            }

            serializedObject.ApplyModifiedProperties();

            SkillConnectionAuthoring connection =
                (SkillConnectionAuthoring)target;
            EditorGUILayout.Space();
            string from = connection.From != null ? connection.From.Id : "Missing";
            string to = connection.To != null ? connection.To.Id : "Missing";
            EditorGUILayout.HelpBox($"Direction: {from} -> {to}", MessageType.Info);
            if (GUILayout.Button("Delete Connection"))
                Undo.DestroyObjectImmediate(connection.gameObject);
        }

        private void OnSceneGUI()
        {
            SkillConnectionAuthoring connection =
                (SkillConnectionAuthoring)target;
            if (connection.From == null || connection.To == null)
                return;
            Vector3 from = connection.From.RectTransform.position;
            Vector3 to = connection.To.RectTransform.position;
            Handles.color = new Color(0.45f, 0.65f, 1f, 0.9f);
            Handles.DrawAAPolyLine(5f, from, to);
            Vector3 direction = to - from;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion rotation = Quaternion.LookRotation(direction.normalized,
                    connection.From.RectTransform.forward);
                float size = HandleUtility.GetHandleSize(to) * 0.12f;
                Handles.ConeHandleCap(0, to, rotation, size, EventType.Repaint);
            }
        }
    }
}