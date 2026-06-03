//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Yade.Runtime.Formula;
using Yade.Runtime;
using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Yade.Runtime.BinarySerialization;

namespace Yade.Editor
{
    [InitializeOnLoad]
    public class YadeGlobal
    {
        public static List<FormulaFunction> CustomFunctions;
        public static List<Importer> importers;
        public static List<BulkImportMethod> bulkImportMethods;
        public static List<Exporter> exporters;
        public static List<ContextMenuItem> extendedContextMenuItem;
        public static List<object> enums;

        private static string[] assemblies = new string[] {
            "YadeEditor",
            "Assembly-CSharp-Editor"
        };

        static YadeGlobal()
        {
            GetFunctions();
            GetImporters();
            GetExporters();
            GetCustomDataTypes();
            GetEnums();
            GetExtendedContextMenuItems();
            GetBulkImportMethods();
        }

        private static void GetBulkImportMethods()
        {
            bulkImportMethods = new List<BulkImportMethod>();
            ForInTypeDo<BulkImportMethod>(importer => bulkImportMethods.Add(importer as BulkImportMethod), assemblies);
        }

        public static string[] GetEnumValues(string enumName)
        {
            if (enums == null)
            {
                return null;
            }

            foreach (var item in enums)
            {
                if (item.ToString() == enumName)
                {
                    return System.Enum.GetNames(item as Type);
                }
            }

            return null;
        }

        public static bool IsEnumExists(string enumName)
        {
            if (enums == null)
            {
                return false;
            }

            foreach (var item in enums)
            {
                if (item.ToString() == enumName)
                {
                    return true;
                }
            }

            return false;
        }

        private static void GetEnums()
        {
            enums = new List<object>();
            ForInTypeDo<System.Enum>(item =>
            {
                enums.Add(item);
                
                var t = item as Type;
                var attributes = t.GetCustomAttributes(typeof(TypeKey), false);
                if (attributes.Length > 0)
                {
                    DataTypeMapper.RegisterType((attributes[0] as TypeKey).Key, t);
                }
            });
        }

        private static void GetCustomDataTypes()
        {
            ForInTypeDo<Runtime.ICellParser>(data =>
            {
                var t = data.GetType();
                var attributes = t.GetCustomAttributes(typeof(TypeKey), false);
                if (attributes.Length > 0)
                {
                    DataTypeMapper.RegisterType((attributes[0] as TypeKey).Key, t);
                }
            });
        }

        private static void GetExporters()
        {
            exporters = new List<Exporter>();
            ForInTypeDo<Exporter>(exporter => exporters.Add(exporter as Exporter), assemblies);
        }

        private static void GetImporters()
        {
            importers = new List<Importer>();
            ForInTypeDo<Importer>(importer => importers.Add(importer as Importer), assemblies);
        }

        private static void GetExtendedContextMenuItems()
        {
            extendedContextMenuItem = new List<ContextMenuItem>();
            ForInTypeDo<ContextMenuItem>(menu => extendedContextMenuItem.Add(menu as ContextMenuItem), assemblies);
        }

        private static void GetFunctions()
        {
            CustomFunctions = new List<FormulaFunction>();
            ForInTypeDo<FormulaFunction>(f => CustomFunctions.Add(f as FormulaFunction), assemblies);
        }

        private static void ForEachType<T>(Type[] types, Action<object> callback)
        {
            foreach (Type t in types)
            {
                if (t.IsSubclassOf(typeof(T)))
                {
                    if (t.IsEnum)
                    {
                        callback(t);
                    }
                    else
                    {
                        var instance = CreateInstance(t);
                        if (instance != null)
                        {
                            callback(instance);
                        }
                    }
                }
                else if (t.IsClass && typeof(ICellParser).IsAssignableFrom(t))
                {
                    var attributes = t.GetCustomAttributes(typeof(TypeKey), false);
                    if (attributes.Length == 0)
                    {
                        continue;
                    }

                    var instance = CreateInstance(t);
                    if (instance != null)
                    {
                        callback(instance);
                    }
                }
            }
        }

        private static object CreateInstance(Type t)
        {
            if (t.IsSubclassOf(typeof(ScriptableObject)))
            {
                return ScriptableObject.CreateInstance(t);
            }

            return Activator.CreateInstance(t);
        }

        private static void ForInTypeDo<T>(Action<object> callback, params string[] assembliesToVerify)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assembliesToVerify.Length == 0)
            {
                foreach (var item in assemblies)
                {
                    // If fail in this assembily, we go to next one directly
                    try
                    {
                        ForEachType<T>(item.GetTypes(), callback);
                    }
                    catch
                    {

                    }
                }

                return;
            }

            foreach (var asm in assembliesToVerify)
            {
                foreach (var item in assemblies)
                {
                    if (item.GetName().Name.Equals(asm, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            ForEachType<T>(item.GetTypes(), callback);
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }
    }

    internal class BeforeBuildingExtension : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var sheets = Resources.FindObjectsOfTypeAll<Yade.Runtime.YadeSheetData>();
            foreach (var sheet in sheets)
            {
                var serializer = sheet.BinarySerializer as YadeSheetBinarySerializer;
                if (serializer != null && serializer.Logs.Count > 0)
                {
                    Resources.UnloadAsset(sheet);
                }
            }
        }
    }

    internal class UnityEditorExtension
    {
        [InitializeOnLoadMethod]
        public static void OnLoad()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange stateChange)
        {
            switch (stateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    UnloadSheetsAndRebindInEditorIfNeeds();
                    break;
            }
        }

        private static void UnloadSheetsAndRebindInEditorIfNeeds()
        {
            var sheets = Resources.FindObjectsOfTypeAll<Yade.Runtime.YadeSheetData>();
            var openedWindows = Resources.FindObjectsOfTypeAll<YadeEditor>();
            
            foreach (var sheet in sheets)
            {
                var serializer = sheet.BinarySerializer as YadeSheetBinarySerializer;
                if (serializer != null && serializer.Logs.Count > 0)
                {
                    Resources.UnloadAsset(sheet);
                    var assetPath = AssetDatabase.GetAssetPath(sheet);
                    var id = AssetDatabase.AssetPathToGUID(assetPath);

                    foreach (var item in openedWindows)
                    {
                        if (item.assetGuid == id)
                        {
                            item.UpdateUIByData(item.assetGuid);
                        }
                    }
                }
            }

            YadeQueryManager.Instance.Clear();
        }
    }
}
