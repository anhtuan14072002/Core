//  Copyright (c) 2020-present amlovey
//  
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Yade.Runtime;
using Yade.Runtime.Formula;

namespace Yade.Editor
{
    public class YadeEditor : EditorWindow
    {
        /// <summary>
        /// Open sheet edit window
        /// </summary>
        /// <param name="assetPath">Path of yade sheet asset</param>
        public static void OpenSheet(string assetPath)
        {
            assetPathToOpen = assetPath;

            // If already opened the sheet, don't open a new one.
            var windows = Resources.FindObjectsOfTypeAll<YadeEditor>();
            for (int i = 0; i < windows.Length; i++)
            {
                var id = AssetDatabase.AssetPathToGUID(assetPath);
                if (windows[i].assetGuid == id)
                {
                    windows[i].Show();
                    windows[i].Focus();
                    return;
                }
            }

            var window = EditorWindow.CreateInstance<YadeEditor>();
            if (string.IsNullOrEmpty(assetPath))
            {
                window.titleContent = new GUIContent("Empty Sheet");
            }
            else
            {
                window.titleContent = new GUIContent(Utilities.GetFileName(assetPath));
            }

            window.Show();
            window.Focus();
        }

        [SerializeField]
        private AppState state;

        public string assetGuid;
        public static string assetPathToOpen;

        public static FormulaEngine engine;

        private YadeSheet sheet;

        public void UpdateUIByData(string id)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(id);
            var data = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);

            if (state != null && sheet != null)
            {
                state.SetData(data);
                state.fixedHeaderHeight = Constants.FIXED_HEADER_HEIGHT + state.GetExtraHeaderHeight();
                sheet.UpdateUIBaseOnState();
            }
        }

        public void UpdateContentsLater()
        {
            sheet.schedule.Execute(sheet.UpdateUIBaseOnState).ExecuteLater(100);
        }

        private void OnEnable()
        {
            if (state == null)
            {
                if (string.IsNullOrEmpty(assetGuid))
                {
                    assetGuid = AssetDatabase.AssetPathToGUID(assetPathToOpen);
                }

                state = ScriptableObject.CreateInstance<AppState>();
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                var data = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);

                if (!EditorUtility.IsDirty(data))
                {
                    Resources.UnloadAsset(data);
                    data = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);
                }

                state.Init(data);
                state.RecordUndo("create yade state");
                assetPathToOpen = null;
            }

            var styleSheetPath = Utilities.GetAppStyleSheetPath();
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);

            rootVisualElement.styleSheets.Add(stylesheet);
            rootVisualElement.style.backgroundColor = ColorHelper.Parse(Theme.Current.Background);
            rootVisualElement.schedule.Execute(() =>
            {
                Theme.DetectTheme();
                rootVisualElement.ToggleInClassList(EditorGUIUtility.isProSkin ? "pro" : "");
            });

            BindingCustomizeFormulas();
            sheet = new YadeSheet(state);
            rootVisualElement.Add(sheet);

            Undo.undoRedoPerformed += OnUndoRedo;

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (EditorUtility.IsDirty(state.data))
                    {
                        AssetDatabase.SaveAssets();
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    BindingCustomizeFormulas();
                    sheet.UpdateUIBaseOnState();
                    break;
            }
        }

        private void BindingCustomizeFormulas()
        {
            FormulaEngine engine = new FormulaEngine(state.data);
            if (YadeGlobal.CustomFunctions != null)
            {
                YadeGlobal.CustomFunctions.ForEach(f => engine.AddFunction(f));
            }

            state.data.SetFormulaEngine(engine);
        }

        private void OnUndoRedo()
        {
            sheet.UpdateUIBaseOnState();
        }

        private void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssetDatabase.SaveAssets();
            DestroyImmediate(state);
        }

        [UnityEditor.Callbacks.OnOpenAssetAttribute(0)]
        private static bool OpenInUEByDoubleClick(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset is YadeSheetData)
            {
                OpenSheet(AssetDatabase.GetAssetPath(asset));
                return true;
            }

            return false;
        }

        [MenuItem("Tools/Yade/Online Documents")]
        private static void GoToOnlineDocuments()
        {
            Application.OpenURL(Constants.DOC_URL);
        }

        [MenuItem("Tools/Yade/Rate and Review")]
        private static void RateAndReview()
        {
            Application.OpenURL(Constants.UAS_URL);
        }

        [MenuItem("Tools/Yade/Report Issues")]
        private static void ReportIssues()
        {
            Application.OpenURL(Constants.ISSUE_RQ_URL);
        }

        [MenuItem("Tools/Yade/Features Request")]
        private static void FeatureRequest()
        {
            Application.OpenURL(Constants.ISSUE_RQ_URL);
        }
    }
}
