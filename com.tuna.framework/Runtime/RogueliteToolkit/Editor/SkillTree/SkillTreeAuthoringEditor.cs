using UnityEditor;
using UnityEngine;
using RogueliteToolkit.SkillTree.Authoring;

namespace RogueliteToolkit.SkillTree.Editor
{
    [CustomEditor(typeof(SkillTreeAuthoring))]
    public sealed class SkillTreeAuthoringEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            SkillTreeAuthoring authoring = (SkillTreeAuthoring)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Open Skill Tree Authoring Window"))
                SkillTreeAuthoringWindow.Open(authoring);
            if (GUILayout.Button("Bake Skill Tree"))
                SkillTreeAuthoringUtility.Bake(authoring);
        }
    }
}
