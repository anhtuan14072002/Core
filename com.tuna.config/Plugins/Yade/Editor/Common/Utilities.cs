//  Copyright (c) 2020-present amlovey
//  

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yade.Editor
{
    public class Utilities
    {
        internal static string GetFileName(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

#if UNITY_2021_2_OR_NEWER
        internal static UnityEngine.TextCore.Text.FontAsset GetFontAsset()
        {
            var sheetPath = GetAppStyleSheetPath();
            var fontAssetPath = Path.Combine(Path.GetDirectoryName(sheetPath), "MaterialIcons.asset");
            return AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(fontAssetPath);
        }
#endif

        internal static string GetAppStyleSheetPath()
        {
            var defaultPath = "Assets/Yade/Editor/Styles/yade.uss";

            var styleSheetsInProject = AssetDatabase.FindAssets("t:StyleSheet");
            foreach (var id in styleSheetsInProject)
            {
                var path = AssetDatabase.GUIDToAssetPath(id);
                var name = GetFileName(path);
                if (name.Contains("yade"))
                {
                    var content = File.ReadAllText(path);
                    if (content.Contains("ac29263b1d1e4330a4d11276fc6dcea9"))
                    {
                        return path;
                    }
                }
            }


            return defaultPath;
        }

        internal static string GetTypeClassName(string typeString)
        {
            if (string.IsNullOrEmpty(typeString))
            {
                return typeString;
            }

            var temp = typeString.Split(new char[] { '+', '.' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (temp.Length > 0)
            {
                return temp[temp.Length - 1];
            }

            return typeString;
        }

        internal static void SetClipboardData(string text)
        {
            TextEditor textEditor = new TextEditor();
            textEditor.OnFocus();
            textEditor.text = text;
            textEditor.SelectAll();
            textEditor.Copy();
        }

        [Obsolete("Obsolete")]
        internal static string GetClipboardData()
        {
            TextEditor textEditor = new TextEditor();
            textEditor.multiline = true;
            textEditor.text = "";
            textEditor.SelectAll();
            textEditor.Paste();
            return textEditor.text;
        }

        internal static string GetLastOpenFolder()
        {
            var folder = EditorPrefs.GetString("YADE_LAST_OPEN_FOLDER");
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return Application.dataPath;
            }

            return folder;
        }

        internal static void SetLastOpenFolder(string folder)
        {
            EditorPrefs.SetString("YADE_LAST_OPEN_FOLDER", folder);
        }
    }
}
