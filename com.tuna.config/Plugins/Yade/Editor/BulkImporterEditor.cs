//  Copyright (c) 2022-present amlovey
//  
using UnityEngine;
using UnityEditor;
using Yade.Runtime;
using ExcelDataReader;
using System.Text;
using System.IO;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

namespace Yade.Editor
{
    public class BulkImporterEditor : EditorWindow
    {
        [SerializeField]
        private YadeSheetData config;

        [SerializeField]
        private string ignorePrefix = "Source_";

        public static string IgnorePrefix;

        [MenuItem("Tools/Yade/Bulk Importer", false, 10)]
        private static void ShowWindow()
        {
            var window = GetWindow<BulkImporterEditor>();
            window.titleContent = new GUIContent("Bulk Importer");
            window.Show();
        }

        public static List<string> Errors;

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Please select or create a config before bulk importing.");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Config file:");
            EditorGUILayout.BeginHorizontal();

            if (config == null)
            {
                var assetPath = AssetDatabase.GetAssetPath(config);
                if (File.Exists(assetPath))
                {
                    config = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);
                }
            }

            config = EditorGUILayout.ObjectField(config, typeof(YadeSheetData), false) as YadeSheetData;

            if (config == null)
            {
                if (GUILayout.Button("New"))
                {
                    CreateNewConfig();
                }
            }
            else
            {
                if (GUILayout.Button("Open"))
                {
                    Open();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Exclude sheet if its name has prefix:");
            ignorePrefix = EditorGUILayout.TextField(ignorePrefix);
            EditorGUILayout.Space();

            if (GUILayout.Button("Import"))
            {
                try
                {
                    Import();
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void Open()
        {
            if (config == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(config);
            YadeEditor.OpenSheet(assetPath);
        }

        private void Import()
        {
            if (config == null)
            {
                EditorUtility.DisplayDialog("Error", "Config file is empty", "Ok");
                return;
            }

            TryClearTempFiles();

            if (Errors == null)
            {
                Errors = new List<string>();
            }
            else
            {
                Errors.Clear();
            }

            var itemCount = config.GetRowCount();
            IgnorePrefix = ignorePrefix;

            for (int i = 0; i < itemCount; i++)
            {
                var sourceUrlCell = config.GetCell(i, 0);
                var saveToCell = config.GetCell(i, 1);

                if (sourceUrlCell == null && saveToCell == null)
                {
                    continue;
                }

                if (sourceUrlCell == null || saveToCell == null)
                {
                    Errors.Add("SourceUrl or SaveTo is null");
                    continue;
                }

                var sheetNameCell = config.GetCell(i, 2);
                var typeCell = config.GetCell(i, 3);
                bool isYadeSheet = saveToCell.GetRawValue().ToLower().StartsWith("=asset(");
                object saveTo = saveToCell.GetValue();
                if (isYadeSheet)
                {
                    saveTo = saveToCell.GetUnityObject<YadeSheetData>();
                }

                var item = new BulkConfigItem()
                {
                    SourceUrl = sourceUrlCell.GetValue(),
                    saveTo = saveTo,
                    sheetName = sheetNameCell == null ? string.Empty : sheetNameCell.GetValue(),
                    Type = typeCell == null ? string.Empty : typeCell.GetValue()
                };

                // Try fix yadesheet unload issue
                if (item.saveTo == null && isYadeSheet)
                {
                    var assetPath = AssetDatabase.GetAssetPath(item.saveTo as YadeSheetData);
                    item.saveTo = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);
                }

                // if still null, throw errors
                if (item.saveTo == null)
                {
                    Errors.Add(string.Format("Save to target is null for source url {0}", item.SourceUrl));
                    continue;
                }

                var methodName = item.Type;
                var url = item.SourceUrl;
                EditorUtility.DisplayProgressBar("Importing", string.Format("Importing {0}", url), (i + 1) * 1.0f / itemCount);
                if (string.IsNullOrEmpty(methodName))
                {
                    if (url.IndexOf("google.com") != -1)
                    {
                        methodName = "google";
                    }
                    else
                    {
                        var fileName = Path.GetFileName(url).ToLower();
                        if (fileName.EndsWith("csv"))
                        {
                            methodName = "csv";
                        }
                        else if (fileName.EndsWith("xls") || fileName.EndsWith("xlsx"))
                        {
                            methodName = "excel";
                        }
                    }
                }

                if (string.IsNullOrEmpty(methodName))
                {
                    Errors.Add(string.Format("Don't support unkown type for source url {0}", item.SourceUrl));
                    continue;
                }

                var importMethod = YadeGlobal.bulkImportMethods.FirstOrDefault(m => m.GetName().Equals(methodName, StringComparison.OrdinalIgnoreCase));
                if (importMethod == null)
                {
                    Errors.Add(string.Format("Cannot find type {0} for source url {1}", item.Type, url));
                    continue;
                }

                try
                {
                    importMethod.Invoke(item);

                    if (isYadeSheet)
                    {
                        var sheet = item.saveTo as YadeSheetData;
                        if (sheet)
                        {
                            EditorUtility.SetDirty(sheet);
                        }
                    }
                }
                catch (Exception e)
                {
                    Errors.Add(string.Format("Exception happen on {0}, {1}", item.SourceUrl, e));
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();

            if (Errors.Count > 0)
            {
                Errors.ForEach(Debug.Log);
                var msg = "Bulk importing is completed, but there are some errors happen. Please check the console window.";
                EditorUtility.DisplayDialog("Done!", msg, "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Done!", "Bulk importing is completed!", "Ok");
            }
        }

        private void TryClearTempFiles()
        {
            try
            {
                var files = Directory.GetFiles(Path.Combine(Application.dataPath, "..", "Temp"), "*.xlsx");
                foreach (var item in files)
                {
                    File.Delete(item);
                }
            }
            catch
            {

            }
        }

        private void CreateNewConfig()
        {
            var savePath = EditorUtility.SaveFilePanelInProject("Create New Config", "BulkImportConfig", "asset", "");
            if (!string.IsNullOrEmpty(savePath))
            {
                YadeSheetData newConfig = ScriptableObject.CreateInstance<YadeSheetData>();
                newConfig.SetColumnHeaderColumn(0, "SourceUrl", 0, "");
                newConfig.SetColumnHeaderColumn(1, "SaveTo", 0, "");
                newConfig.SetColumnHeaderColumn(2, "SheetName", 0, "");
                newConfig.SetColumnHeaderColumn(3, "Type", 0, "");
                newConfig.VisualHeaders = (int)VisualHeaderType.Alias;

                AssetDatabase.CreateAsset(newConfig, savePath);
                config = newConfig;
            }
        }

        public static List<BulkExecutionItem> ConvertTask(BulkConfigItem configItem, string[] nameMapping = null)
        {
            var execItems = new List<BulkExecutionItem>();

            if (configItem.saveTo is YadeSheetData)
            {
                execItems.Add(new BulkExecutionItem()
                {
                    SourceUrl = configItem.SourceUrl,
                    TargetSheet = configItem.saveTo as YadeSheetData,
                    SheetName = configItem.sheetName,
                    Type = configItem.Type
                });

                return execItems;
            }

            var path = configItem.saveTo as string;
            if (string.IsNullOrEmpty(path))
            {
                return execItems;
            }

            if (path.EndsWith(".asset"))
            {
                execItems.Add(new BulkExecutionItem()
                {
                    SourceUrl = configItem.SourceUrl,
                    TargetSheet = LoadYadeSheetOrCreate(path),
                    SheetName = configItem.sheetName,
                    Type = configItem.Type
                });
            }
            else
            {
                if (nameMapping == null)
                {
                    return execItems;
                }

                // Create folder
                Directory.CreateDirectory(path);

                if (string.IsNullOrEmpty(configItem.sheetName))
                {
                    foreach (var item in nameMapping)
                    {
                        var assetPath = Path.Combine(path, item + ".asset");
                        execItems.Add(new BulkExecutionItem()
                        {
                            SourceUrl = configItem.SourceUrl,
                            TargetSheet = LoadYadeSheetOrCreate(assetPath),
                            SheetName = item,
                            Type = configItem.Type
                        });
                    }
                }
                else
                {
                    var assetPath = Path.Combine(path, configItem.sheetName + ".asset");
                    execItems.Add(new BulkExecutionItem()
                    {
                        SourceUrl = configItem.SourceUrl,
                        TargetSheet = LoadYadeSheetOrCreate(assetPath),
                        SheetName = configItem.sheetName,
                        Type = configItem.Type
                    });
                }
            }

            return execItems;
        }

        private static YadeSheetData LoadYadeSheetOrCreate(string assetPath)
        {
            YadeSheetData targetSheet;

            if (File.Exists(assetPath))
            {
                targetSheet = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);
            }
            else
            {
                targetSheet = ScriptableObject.CreateInstance<YadeSheetData>();
                AssetDatabase.CreateAsset(targetSheet, assetPath);
                AssetDatabase.SaveAssets();
            }

            return targetSheet;
        }
    }

    public class BulkExecutionItem
    {
        public string SourceUrl;
        public YadeSheetData TargetSheet;
        public string SheetName;
        public string Type;
    }

    public class BulkConfigItem
    {
        [DataField(0)] public string SourceUrl;
        [DataField(1)] public object saveTo;
        [DataField(2)] public string sheetName;
        [DataField(3)] public string Type;

        public override string ToString()
        {
            return string.Format("{0}\n{1}\n{2}\n{3}", SourceUrl, saveTo, sheetName, Type);
        }
    }

    public abstract class BulkImportMethod
    {
        public abstract void Invoke(BulkConfigItem configItem);
        public abstract string GetName();
    }

    internal class CSVBulkImportMethod : BulkImportMethod
    {
        public override string GetName()
        {
            return "csv";
        }

        public override void Invoke(BulkConfigItem configItem)
        {
            var config = new ExcelReaderConfiguration()
            {
                FallbackEncoding = Encoding.GetEncoding(1252),
                AutodetectSeparators = new char[] { ',', ';', '#', '\t', '|' }
            };

            var src = configItem.SourceUrl;
            using (var stream = File.Open(src, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateCsvReader(stream, config))
                {
                    var result = reader.AsDataSet();
                    var table = result.Tables[0];
                    var rowCount = table.Rows.Count;
                    var columnCount = table.Columns.Count;
                    var executeItems = BulkImporterEditor.ConvertTask(configItem);
                    if (executeItems.Count == 0)
                    {
                        BulkImporterEditor.Errors.Add(string.Format("Failed at {0} due to no item to execute", configItem.SourceUrl));
                        return;
                    }

                    var target = executeItems[0];
                    int skipRowsCount = PreprocessingWithSkipRows(table, target);

                    target.TargetSheet.Clear();
                    for (int i = skipRowsCount; i < rowCount; i++)
                    {
                        for (int j = 0; j < columnCount; j++)
                        {
                            var valule = table.Rows[i].ItemArray[j];
                            if (valule != null)
                            {
                                target.TargetSheet.SetRawValue(i - skipRowsCount, j, valule.ToString());
                            }
                        }
                    }

                    target.TargetSheet.RecalculateValues();
                }
            }
        }

        protected virtual int PreprocessingWithSkipRows(DataTable table, BulkExecutionItem item)
        {
            return 0;
        }
    }

    internal class ExcelBulkImportMethod : BulkImportMethod
    {
        public override string GetName()
        {
            return "excel";
        }

        public override void Invoke(BulkConfigItem configItem)
        {
            ImportFromLocalExcel(configItem);
        }

        protected void ImportFromLocalExcel(BulkConfigItem configItem)
        {
            var config = new ExcelReaderConfiguration()
            {
                FallbackEncoding = Encoding.GetEncoding(1252)
            };

            var filePath = configItem.SourceUrl;
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream, config))
                {
                    var result = reader.AsDataSet();
                    var sheetTables = new List<DataTable>();
                    foreach (DataTable item in result.Tables)
                    {
                        var tableName = item.TableName;
                        if (string.IsNullOrEmpty(tableName) || tableName.StartsWith(BulkImporterEditor.IgnorePrefix))
                        {
                            continue;
                        }

                        sheetTables.Add(item);
                    }

                    var executeItems = BulkImporterEditor.ConvertTask(configItem, sheetTables.Select(t => t.TableName).ToArray());
                    if (executeItems.Count == 0)
                    {
                        BulkImporterEditor.Errors.Add(string.Format("Failed at {0} due to no item to execute", configItem.SourceUrl));
                        return;
                    }

                    foreach (var eItem in executeItems)
                    {
                        // Reload to ensure not null exception
                        var assetPath = AssetDatabase.GetAssetPath(eItem.TargetSheet);
                        eItem.TargetSheet = AssetDatabase.LoadAssetAtPath<YadeSheetData>(assetPath);

                        if (string.IsNullOrEmpty(eItem.SheetName))
                        {
                            ImportTable(result.Tables[0], eItem.TargetSheet);
                        }
                        else
                        {
                            foreach (var table in sheetTables)
                            {
                                if (table.TableName == eItem.SheetName)
                                {
                                    ImportTable(table, eItem.TargetSheet);
                                    if (eItem.TargetSheet)
                                    {
                                        EditorUtility.SetDirty(eItem.TargetSheet);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void ImportTable(DataTable table, YadeSheetData targetSheet)
        {
            var rowCount = table.Rows.Count;
            var columnCount = table.Columns.Count;

            int skipRowsCount = PreprocessingWithSkipRows(table, targetSheet);
            targetSheet.Clear();

            for (int i = skipRowsCount; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    var valule = table.Rows[i].ItemArray[j];
                    if (valule != null)
                    {
                        targetSheet.SetRawValue(i - skipRowsCount, j, valule.ToString());
                    }
                }
            }

            targetSheet.RecalculateValues();
        }

        protected virtual int PreprocessingWithSkipRows(DataTable table, YadeSheetData targetSheet)
        {
            return 0;
        }
    }

    internal class GoogleBulkImportMethod : ExcelBulkImportMethod
    {
        public override string GetName()
        {
            return "google";
        }

        public override void Invoke(BulkConfigItem configItem)
        {
            ImportGoogleSheet(configItem);
        }

        protected void ImportGoogleSheet(BulkConfigItem configItem)
        {
            var url = configItem.SourceUrl;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            var pattern = new Regex(@"/(?<Source>[a-zA-z]+)?/d/(?<Id>\S+)?/");
            var match = pattern.Match(url);
            if (match != null)
            {
                var source = match.Groups["Source"].Value;
                var id = match.Groups["Id"].Value;
                var convertedUrl = string.Format("https://docs.google.com/{0}/d/{1}/export?format=xlsx&id={1}&_{2}", source, id, UnityEngine.Random.Range(0, int.MaxValue));
                var tempFile = Path.Combine(Application.dataPath, "..", "Temp", id + ".xlsx");
                if (File.Exists(tempFile))
                {
                    var newConfigItem = new BulkConfigItem();
                    newConfigItem.saveTo = configItem.saveTo;
                    newConfigItem.SourceUrl = tempFile;
                    newConfigItem.sheetName = configItem.sheetName;
                    newConfigItem.Type = configItem.Type;
                    ImportFromLocalExcel(newConfigItem);
                    return;
                }

                try
                {
                    DownloaderWebClient client = new DownloaderWebClient();
                    var bytes = client.DownloadData(convertedUrl);
                    var content = Encoding.UTF8.GetString(bytes);
                    if (content.Contains("google-site-verification"))
                    {
                        if (BulkImporterEditor.Errors != null)
                        {
                            BulkImporterEditor.Errors.Add("Cannot download Google Sheets Data! May be your link is not public?");
                        }
                        return;
                    }

                    File.WriteAllBytes(tempFile, bytes);
                    var newConfigItem = new BulkConfigItem();
                    newConfigItem.saveTo = configItem.saveTo;
                    newConfigItem.SourceUrl = tempFile;
                    newConfigItem.sheetName = configItem.sheetName;
                    newConfigItem.Type = configItem.Type;
                    ImportFromLocalExcel(newConfigItem);
                }
                catch (Exception e)
                {
                    if (BulkImporterEditor.Errors != null)
                    {
                        BulkImporterEditor.Errors.Add(string.Format("Exception happen on {0}, {1}", url, e));
                    }
                }
            }
        }
    }
}