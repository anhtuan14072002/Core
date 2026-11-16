using System;
using System.Collections.Generic;
using RogueliteToolkit.Resources;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace RogueliteToolkit.Editor
{
    internal sealed class ResourceDefinitionProcessor : AssetPostprocessor, IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)) continue;
                ResourceDefinition resource = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(path);
                if (resource == null) continue;
                // Moving/renaming keeps the ID; duplicating an asset gets a new ID from its new GUID.
                int id = GetId(AssetDatabase.AssetPathToGUID(path));
                if (resource.Id == id) continue;
                using (SerializedObject serialized = new(resource))
                {
                    serialized.FindProperty("_id").intValue = id;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                AssetDatabase.SaveAssetIfDirty(resource);
            }
        }

        internal static int GetId(string guid) =>
            (int)(Convert.ToUInt32(guid.Substring(0, 8), 16) % int.MaxValue) + 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            Dictionary<int, string> ids = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ResourceDefinition resource = AssetDatabase.LoadAssetAtPath<ResourceDefinition>(path);
                if (resource.Id != GetId(guid))
                    throw new BuildFailedException($"Reimport resource '{path}' to initialize its ID.");
                if (ids.TryGetValue(resource.Id, out string existing))
                    throw new BuildFailedException($"Resource ID collision: '{existing}' and '{path}'. Recreate one resource before release.");
                ids.Add(resource.Id, path);
            }
        }
    }
}
