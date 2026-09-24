using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RogueliteToolkit.SkillTree.Authoring;

namespace RogueliteToolkit.SkillTree.Editor
{
    public static class SkillNodeAssetMigration
    {
        [InitializeOnLoadMethod]
        private static void ScheduleMigration() => EditorApplication.delayCall += MigrateAll;

        [MenuItem("Tools/Roguelite Toolkit/Migrate Skill Nodes To Assets")]
        public static void MigrateAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            foreach (string guid in AssetDatabase.FindAssets("t:SkillTreeDefinition", new[] { "Assets" }))
            {
                SkillTreeDefinition tree =
                    AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                MigrateTree(tree);
            }
        }

        public static void MigrateTree(SkillTreeDefinition tree)
        {
            if (tree == null || !tree.Nodes.Any(node => node != null && node.Data == null))
                return;
            Undo.RecordObject(tree, "Split Skill Node Assets");
            foreach (SkillNodeDefinition node in tree.Nodes)
            {
                if (node == null || node.Data != null) continue;
                SkillNodeDataSO data = CreateData(tree, node);
                node.SetData(data);
            }

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssetIfDirty(tree);
            Debug.Log($"Split nodes into individual assets for '{tree.name}'.", tree);
        }

        internal static SkillNodeDataSO CreateData(SkillTreeDefinition tree, SkillNodeDefinition node)
        {
            string treePath = AssetDatabase.GetAssetPath(tree);
            string parent = Path.GetDirectoryName(treePath).Replace('\\', '/');
            string folderName = Path.GetFileNameWithoutExtension(treePath) + "_Nodes";
            string folder = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(parent, folderName);
            string safeName = GetAssetName(node);
            SkillNodeDataSO data = ScriptableObject.CreateInstance<SkillNodeDataSO>();
            data.CopyFrom(node);
            AssetDatabase.CreateAsset(data, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + safeName + ".asset"));
            return data;
        }

        private static string GetAssetName(SkillNodeDefinition node)
        {
            string label = string.IsNullOrWhiteSpace(node.DisplayName) ? node.Id : node.DisplayName;
            if (string.IsNullOrWhiteSpace(label)) return "SkillNode";
            string safeName = string.Concat(label.Trim().Select(c =>
                char.IsControl(c) || "<>:\"/\\|?*".Contains(c) ? '_' : c)).TrimEnd(' ', '.');
            return string.IsNullOrEmpty(safeName) ? "SkillNode" : safeName;
        }

        private static void RenameData(SkillNodeDataSO data, SkillNodeDefinition node)
        {
            string path = AssetDatabase.GetAssetPath(data);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsMainAsset(data)) return;
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            string assetName = GetAssetName(node);
            string guid = AssetDatabase.AssetPathToGUID(path);
            string candidate = assetName;
            for (int suffix = 2;; suffix++)
            {
                string candidatePath = folder + "/" + candidate + ".asset";
                string existingGuid = AssetDatabase.AssetPathToGUID(candidatePath);
                if (string.IsNullOrEmpty(existingGuid) || existingGuid == guid) break;
                candidate = assetName + " " + suffix;
            }

            if (Path.GetFileNameWithoutExtension(path) == candidate) return;
            string error = AssetDatabase.RenameAsset(path, candidate);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"Could not rename skill node asset '{path}': {error}", data);
        }

        internal static void StoreNodes(SkillTreeDefinition tree, SkillNodeDefinition[] nodes,
            SkillNodeAuthoring[] authored)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                SkillNodeDefinition node = nodes[i];
                SkillNodeDataSO data = authored[i].Data;
                if (data == null)
                    data = tree.Nodes.FirstOrDefault(old => old != null && old.Id == node.Id)?.Data;
                if (data == null)
                    data = CreateData(tree, node);
                // Existing DataSO is authoritative; baking only reconnects it.
                node.SetData(data);
                RenameData(data, node);
                Undo.RecordObject(authored[i], "Assign Skill Node Data");
                authored[i].SetData(data);
                EditorUtility.SetDirty(authored[i]);
            }
        }
    }
}