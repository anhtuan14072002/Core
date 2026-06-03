//  Copyright (c) 2021-present amlovey
//  
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yade.Runtime;

namespace Yade.Editor
{
    public class AssetWatcher : AssetPostprocessor
    {
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var openedWindows = Resources.FindObjectsOfTypeAll<YadeEditor>();

            // Save, Redo or Revert Change in git will marked as 'imported' asset
            // We don't need to handle move and move from because it already works
            //
            foreach (var item in openedWindows)
            {
                var importedOne = ArrayUtility.Find(importedAssets, assetPath => AssetDatabase.AssetPathToGUID(assetPath) == item.assetGuid);
                if (importedOne != null)
                {
                    item.UpdateUIByData(item.assetGuid);
                    continue;
                }

                var deleteOne = ArrayUtility.Find(deletedAssets, assetPath => AssetDatabase.AssetPathToGUID(assetPath) == item.assetGuid);
                if (deleteOne != null)
                {
                    item.Close();
                    continue;
                }

                // Sync moethods will case Unity Edito hang on if there are
                // very large sheet afte recompile. We we use an delayed method
                // to slove it.
                item.UpdateContentsLater();
            }

            // Update database if its sheets is renamed
            UpdateYadeDatabaseIfNeeds(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        private static void UpdateYadeDatabaseIfNeeds(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            List<YadeSheetData> importedSheets = new List<YadeSheetData>();

            foreach (var item in importedAssets)
            {
                var asset = AssetDatabase.LoadAssetAtPath<YadeSheetData>(item);
                if (asset)
                {
                    importedSheets.Add(asset);
                }
            }

            if (importedSheets.Count > 0)
            {
                var dbIds = AssetDatabase.FindAssets("t:YadeDatabase");
                bool isDirty = false;
                for (int i = 0; i < dbIds.Length; i++)
                {
                    var db = AssetDatabase.LoadAssetAtPath<YadeDatabase>(AssetDatabase.GUIDToAssetPath(dbIds[i]));
                    var keys = db.Sheets.Keys;
                    foreach (var sheet in importedSheets)
                    {
                        var sheetId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sheet));
                        for (int idx = 0; idx < keys.Length; idx++)
                        {
                            var sheetInDb = db.Sheets[keys[idx]];
                            if (sheetInDb == null)
                            {
                                continue;
                            }

                            var sheetDBId = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sheetInDb));
                            if (sheetDBId == sheetId)
                            {
                                if (sheet.name != keys[idx])
                                {
                                    db.Sheets.Remove(keys[idx]);
                                    db.Sheets.Add(sheet.name, sheet);
                                    EditorUtility.SetDirty(db);
                                    isDirty = true;
                                }
                            }
                        }
                    }
                }

                if (isDirty)
                {
                    var inspectors = Resources.FindObjectsOfTypeAll<YadeSheetDataInspector>();
                    foreach (var inspector in inspectors)
                    {
                        inspector.Repaint();
                    }

                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}