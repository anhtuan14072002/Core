using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

public class FolderTextureSettingsWindow : EditorWindow
{
    private enum InputMode { Folder, Textures }
    [SerializeField] private InputMode _mode;
    [SerializeField] private DefaultAsset _folder;
    [SerializeField] private UnityEngine.Object[] _sources = Array.Empty<UnityEngine.Object>();
    [SerializeField] private bool _includeSubfolders = true;
    private Editor _textureEditor;
    private FolderTexturePreviewWindow _previewWindow;
    private Vector2 _scroll;
    private int _textureCount;

    [MenuItem("Tools/Textures/Folder Texture Settings")]
    [MenuItem("Assets/Folder Texture Settings", false, 2000)]
    public static void Open()
    {
        var window = GetWindow<FolderTextureSettingsWindow>("Folder Textures");
        window.minSize = new Vector2(420, 400);
        if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject)))
        {
            window._mode = InputMode.Folder;
            window._folder = Selection.activeObject as DefaultAsset;
        }
        else if (Selection.activeObject is Texture)
        {
            window._mode = InputMode.Textures;
            window._sources = Selection.objects;
        }
        window.ReloadTextures();
    }

    private void OnEnable() { ReloadTextures(); }
    private void OnDisable()
    {
        DestroyImmediate(_textureEditor);
        if (_previewWindow != null) _previewWindow.Close();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        _mode = (InputMode)GUILayout.Toolbar((int)_mode, new[] { "Select Folder", "Drag Textures" });
        bool changed = EditorGUI.EndChangeCheck();
        if (_mode == InputMode.Folder)
        {
            EditorGUI.BeginChangeCheck();
            _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder (drag here)", _folder, typeof(DefaultAsset), false);
            if (AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(_folder)))
                _includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", _includeSubfolders);
            changed |= EditorGUI.EndChangeCheck();
        }
        else
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 64, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop textures here\nEach drop replaces the current selection");
            Event current = Event.current;
            if (dropArea.Contains(current.mousePosition) &&
                (current.type == EventType.DragUpdated || current.type == EventType.DragPerform))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    _sources = (UnityEngine.Object[])DragAndDrop.objectReferences.Clone();
                    changed = true;
                }
                current.Use();
            }
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload Textures") || changed) ReloadTextures();
        if (GUILayout.Button("Clear"))
        {
            if (_mode == InputMode.Folder) _folder = null;
            else _sources = Array.Empty<UnityEngine.Object>();
            ReloadTextures();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Textures", _textureCount.ToString());
        EditorGUILayout.HelpBox("Edit the shared import settings below, then click Apply. A dash means the textures have different values. Only edited settings are applied.", MessageType.Info);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        if (_textureEditor != null) _textureEditor.OnInspectorGUI();
        else EditorGUILayout.HelpBox("Select a folder or drop textures to show their import settings.", MessageType.Info);
        EditorGUILayout.EndScrollView();
        using (new EditorGUI.DisabledScope(_textureEditor == null))
            if (GUILayout.Button("Apply", GUILayout.Height(30)))
            {
                ApplySettings((AssetImporterEditor)_textureEditor);
                ShowNotification(new GUIContent($"Applied to {_textureCount} textures"));
            }

    }

    private static void ApplySettings(AssetImporterEditor editor)
    {
        editor.SaveChanges();
        // Embedded importers skip Unity's normal Apply/Reimport step.
        foreach (var target in editor.targets)
            ((TextureImporter)target).SaveAndReimport();
    }


    private void ReloadTextures()
    {
        // Destroying the native importer editor lets Unity handle pending Apply/Revert edits.
        DestroyImmediate(_textureEditor);
        _textureEditor = null;
        var textures = _mode == InputMode.Folder
            ? CollectTextures(AssetDatabase.GetAssetPath(_folder), _includeSubfolders)
            : CollectSources(_sources);
        _textureCount = textures.Length;
        if (_textureCount > 0) _textureEditor = Editor.CreateEditor(textures);
        if (_mode == InputMode.Textures && _textureCount > 0)
        {
            var assets = new UnityEngine.Object[_textureCount];
            for (int i = 0; i < assets.Length; i++)
                assets[i] = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GetAssetPath(textures[i]));
            if (_previewWindow == null)
            {
                _previewWindow = CreateInstance<FolderTexturePreviewWindow>();
                _previewWindow.titleContent = new GUIContent("Texture Preview");
                _previewWindow.position = new Rect(position.xMax + 8, position.y, 520, 340);
                _previewWindow.minSize = new Vector2(320, 200);
                _previewWindow.ShowUtility();
            }
            _previewWindow.SetTextures(assets);
        }
        else if (_previewWindow != null) _previewWindow.Close();
    }

    private static UnityEngine.Object[] CollectSources(UnityEngine.Object[] sources)
    {
        var textures = new List<UnityEngine.Object>();
        var seen = new HashSet<UnityEngine.Object>();
        foreach (var source in sources)
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (source is Texture && path.StartsWith("Assets/", StringComparison.Ordinal) &&
                AssetImporter.GetAtPath(path) is TextureImporter importer && seen.Add(importer))
                textures.Add(importer);
        }
        return textures.ToArray();
    }

    private static UnityEngine.Object[] CollectTextures(string folder, bool recursive)
    {
        var textures = new List<UnityEngine.Object>();
        if (!AssetDatabase.IsValidFolder(folder) || (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal)))
            return textures.ToArray();

        foreach (string guid in AssetDatabase.FindAssets("t:Texture", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!recursive && path.LastIndexOf('/') != folder.Length) continue;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer) textures.Add(importer);
        }
        return textures.ToArray();
    }

}

public class FolderTexturePreviewWindow : EditorWindow
{
    private Editor _previewEditor;

    public void SetTextures(UnityEngine.Object[] textures)
    {
        DestroyImmediate(_previewEditor);
        _previewEditor = Editor.CreateEditor(textures);
        Repaint();
    }

    private void OnDisable() { DestroyImmediate(_previewEditor); }

    private void OnGUI()
    {
        if (_previewEditor == null) return;
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(_previewEditor.GetPreviewTitle(), EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        _previewEditor.OnPreviewSettings();
        EditorGUILayout.EndHorizontal();
        Rect previewArea = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        _previewEditor.DrawPreview(previewArea);
    }
}
