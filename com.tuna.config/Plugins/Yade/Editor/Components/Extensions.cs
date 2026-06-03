//  Copyright (c) 2020-present amlovey
//  
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ExcelDataReader;
using UnityEditor;
using UnityEngine;
using Yade.Runtime;
using Yade.Runtime.Formula;

namespace Yade.Editor
{
    public abstract class Exporter
    {
        /// <summary>
        /// Process export logic
        /// </summary>
        /// <param name="state">State of sheet</param>
        /// <returns>Whether refresh ui or not</returns>
        public abstract bool Execute(AppState state);

        /// <summary>
        /// Get the name of importer
        /// </summary>
        /// <returns>Name of importer</returns>
        public abstract string GetMenuName();

        /// <summary>
        /// Is this exporter is available in Import/Export menu?
        /// </summary>
        public virtual bool IsAvailable(AppState state)
        {
            return true;
        }
    }

    public abstract class Importer
    {
        /// <summary>
        /// Process export logic
        /// </summary>
        /// <param name="state">State of sheet</param>
        /// <returns>Whether refresh ui or not</returns>
        public abstract bool Execute(AppState state);

        /// <summary>
        /// Get the name of exporter
        /// </summary>
        /// <returns>Name of exporter</returns>
        public abstract string GetMenuName();

        /// <summary>
        /// Is this importer is available in Import/Export menu?
        /// </summary>
        public virtual bool IsAvailable(AppState state)
        {
            return true;
        }
    }

    public abstract class ContextMenuItem
    {
        /// <summary>
        /// Process context menu action
        /// </summary>
        public abstract void Execute(AppState state);

        /// <summary>
        /// Whether the context menu item is enabled
        /// </summary>
        public virtual bool GetEnabledState(AppState state)
        {
            return true;
        }

        /// <summary>
        /// Get key of context menu item 
        /// </summary>
        /// <returns></returns>
        public abstract string GetMenuKey();

        /// <summary>
        /// Get name of context menu item
        /// </summary>
        /// <returns></returns>
        public abstract string GetMenuName();

        /// <summary>
        /// Is this menu is available in context menu list?
        /// </summary>
        public virtual bool IsAvailable(AppState state)
        {
            return true;
        }
    }

    public class ExcelImporter : Importer
    {
        public override bool Execute(AppState state)
        {
            var filePath = EditorUtility.OpenFilePanelWithFilters("Open Excel File", Utilities.GetLastOpenFolder(), new string[] { "Excel Files", "xls,xlsx" });
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var folder = Path.GetDirectoryName(filePath);
            if (Directory.Exists(folder))
            {
                Utilities.SetLastOpenFolder(folder);
            }

            ImportFromLocalExcel(filePath, state);

            return true;
        }

        protected void ImportFromLocalExcel(string filePath, AppState state)
        {
            var config = new ExcelReaderConfiguration()
            {
                FallbackEncoding = Encoding.GetEncoding(1252)
            };

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream, config))
                {
                    var result = reader.AsDataSet();
                    var tableCount = result.Tables.Count;

                    if (tableCount > 1)
                    {
                        List<string> options = new List<string>();
                        for (int i = 0; i < tableCount; i++)
                        {
                            options.Add(result.Tables[i].TableName);
                        }

                        state.BindingSheet.ShowSelectionDialog(
                            "Select Sheet to import",
                            options,
                            (idx) =>
                            {
                                if (idx == -1)
                                {
                                    return;
                                }

                                ImportTable(result.Tables[idx], state);
                                state.BindingSheet.UpdateUIBaseOnState();
                            }
                        );
                    }
                    else if (tableCount == 1)
                    {
                        ImportTable(result.Tables[0], state);
                    }
                }
            }
        }

        protected virtual void ImportTable(DataTable table, AppState state)
        {
            var rowCount = table.Rows.Count;
            var columnCount = table.Columns.Count;

            state.ClearData();
            
            if (state.rowCount < rowCount)
            {
                state.SetRowCount(rowCount);
            }

            if (state.columnCount < columnCount)
            {
                state.SetColumnCount(columnCount);
            }

            int skipRowsCount = PreprocessingWithSkipRows(table, state);

            for (int i = skipRowsCount; i < rowCount; i++)
            {
                for (int j = 0; j < columnCount; j++)
                {
                    var valule = table.Rows[i].ItemArray[j];
                    if (valule != null)
                    {
                        state.SetCellRawValue(i - skipRowsCount, j, valule.ToString());
                    }
                }
            }

            state.data.RecalculateValues();
        }

        protected virtual int PreprocessingWithSkipRows(DataTable table, AppState state)
        {
            return 0;
        }

        public override string GetMenuName()
        {
            return "Import From Excel";
        }
    }

    public class CSVImporter : Importer
    {
        public override bool Execute(AppState state)
        {
            var filePath = EditorUtility.OpenFilePanel("Open CSV File", Utilities.GetLastOpenFolder(), "csv");
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var folder = Path.GetDirectoryName(filePath);
            if (Directory.Exists(folder))
            {
                Utilities.SetLastOpenFolder(folder);
            }

            var config = new ExcelReaderConfiguration()
            {
                FallbackEncoding = Encoding.GetEncoding(1252),
                AutodetectSeparators = new char[] { ',', ';', '#', '\t', '|' }
            };

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateCsvReader(stream, config))
                {
                    var result = reader.AsDataSet();
                    var table = result.Tables[0];
                    var rowCount = table.Rows.Count;
                    var columnCount = table.Columns.Count;

                    state.ClearData();

                    if (state.rowCount < rowCount)
                    {
                        state.SetRowCount(rowCount);
                    }

                    if (state.columnCount < columnCount)
                    {
                        state.SetRowCount(columnCount);
                    }

                    int skipRowsCount = PreprocessingWithSkipRows(table, state);

                    for (int i = skipRowsCount; i < rowCount; i++)
                    {
                        for (int j = 0; j < columnCount; j++)
                        {
                            var valule = table.Rows[i].ItemArray[j];
                            if (valule != null)
                            {
                                state.SetCellRawValue(i - skipRowsCount, j, valule.ToString());
                            }
                        }
                    }

                    state.data.RecalculateValues();
                }
            }

            return true;
        }

        protected virtual int PreprocessingWithSkipRows(DataTable table, AppState state)
        {
            return 0;
        }

        public override string GetMenuName()
        {
            return "Import From CSV";
        }
    }

    partial class DownloaderWebClient : WebClient
    {
        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest w = base.GetWebRequest(uri);
            w.Timeout = 15 * 1000;
            return w;
        }
    }

    public class GoogleSheetsImporter : ExcelImporter
    {
        public override bool Execute(AppState state)
        {
            state.BindingSheet.ShowTextInputDialog(
                "Import From Google Sheets",
                "Public Links:",
                url =>
                {
                    try
                    {
                        ImportGoogleSheet(url, state);
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error", e.Message, "Ok");
                    }
                }
            );

            return true;
        }

        protected void ImportGoogleSheet(string url, AppState state)
        {
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
                DownloaderWebClient client = new DownloaderWebClient();
                client.DownloadDataCompleted += (s, e) =>
                {
                    if (e.Error != null)
                    {
                        EditorUtility.DisplayDialog("Cannot download Google Sheets Data", e.Error.Message, "Ok");
                        state.BindingSheet.HideLoading();
                        return;
                    }

                    var bytes = e.Result;
                    var content = Encoding.UTF8.GetString(bytes);

                    if (content.Contains("google-site-verification"))
                    {
                        EditorUtility.DisplayDialog("Cannot download Google Sheets Data", "May be your link is not public?", "Ok");
                        state.BindingSheet.HideLoading();
                        return;
                    }
                    else
                    {
                        File.WriteAllBytes(tempFile, bytes);
                        ImportFromLocalExcel(tempFile, state);
                        state.BindingSheet.HideLoading();
                    }
                };

                state.BindingSheet.ShowLoading("Downloading Google Sheets...");
                client.DownloadDataAsync(new Uri(convertedUrl));
            }
        }

        public override string GetMenuName()
        {
            return "Import From Google Sheets";
        }
    }

    public class CSVExporter : CSVRawExporter
    {
        public override bool Execute(AppState state)
        {
            var filePath = EditorUtility.SaveFilePanel("Export To CSV File", Utilities.GetLastOpenFolder(), state.data.name, "csv");
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var folder = Path.GetDirectoryName(filePath);
            if (Directory.Exists(folder))
            {
                Utilities.SetLastOpenFolder(folder);
            }

            var rowsCount = state.data.GetRowCount();
            var columnsCount = state.data.GetColumnCount();

            if (rowsCount <= 0 || columnsCount <= 0)
            {
                return false;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rowsCount; i++)
            {
                var row = state.data.data.GetRow(i);
                if (row == null)
                {
                    sb.AppendLine(new string(',', columnsCount - 1));
                }
                else
                {
                    List<string> columns = new List<string>();
                    for (int j = 0; j < columnsCount; j++)
                    {
                        var cell = row.GetCell(j);
                        if (cell == null)
                        {
                            columns.Add(string.Empty);
                        }
                        else
                        {
                            columns.Add(GetCellValue(cell));
                        }
                    }

                    sb.AppendLine(string.Join(",", columns));
                }
            }

            Encoding utf8WithoutBom = new UTF8Encoding(true);
            File.WriteAllText(filePath, sb.ToString(), utf8WithoutBom);

            return false;
        }

        protected override string GetCellValue(Cell cell)
        {
            return cell.HasUnityObject() ? string.Empty : Escape(cell.GetValue());
        }

        public override string GetMenuName()
        {
            return "Export Value To CSV";
        }
    }

    public class CSVRawExporter : Exporter
    {
        public override bool Execute(AppState state)
        {
            var filePath = EditorUtility.SaveFilePanel("Export To CSV File", Utilities.GetLastOpenFolder(), state.data.name, "csv");

            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            var folder = Path.GetDirectoryName(filePath);
            if (Directory.Exists(folder))
            {
                Utilities.SetLastOpenFolder(folder);
            }

            var rowsCount = state.data.GetRowCount();
            var columnsCount = state.data.GetColumnCount();

            if (rowsCount <= 0 || columnsCount <= 0)
            {
                return false;
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < rowsCount; i++)
            {
                var row = state.data.data.GetRow(i);
                if (row == null)
                {
                    sb.AppendLine(new string(',', columnsCount - 1));
                }
                else
                {
                    List<string> columns = new List<string>();
                    for (int j = 0; j < columnsCount; j++)
                    {
                        var cell = row.GetCell(j);
                        if (cell == null)
                        {
                            columns.Add(string.Empty);
                        }
                        else
                        {
                            columns.Add(GetCellValue(cell));
                        }
                    }
                    sb.AppendLine(string.Join(",", columns));
                }
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

            return false;
        }

        protected virtual string GetCellValue(Cell cell)
        {
            return Escape(cell.GetRawValue());
        }

        protected string Escape(string s)
        {
            bool mustQuoted = (s.Contains(",") || s.Contains("\"") || s.Contains("\r") || s.Contains("\n"));
            if (mustQuoted)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in s)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                    {
                        sb.Append("\"");
                    }
                }
                sb.Append("\"");
                return sb.ToString();
            }

            return s;
        }

        public override string GetMenuName()
        {
            return "Export Raw To CSV";
        }
    }

    public class Asset : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return null;
            }
            var assetPath = this.Parameters[0].ToString();
            object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return asset;
        }

        public override string GetName()
        {
            return "ASSET";
        }
    }

    public class AssetsFormula : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count == 0)
            {
                return null;
            }

            List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
            foreach (var assetPath in this.Parameters)
            {
                if (assetPath == null)
                {
                    continue;
                }

                var path = assetPath.ToString();

                if (!File.Exists(path))
                {
                    continue;
                }

                assets.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            }

            return assets.ToArray();
        }

        private static string[] ToPaths(UnityEngine.Object[] assets)
        {
            if (assets == null || assets.Length == 0)
            {
                return new string[0];
            }

            List<string> paths = new List<string>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] == null)
                {
                    continue;
                }

                paths.Add(string.Format("\"{0}\"", AssetDatabase.GetAssetPath(assets[i])));
            }

            return paths.ToArray();
        }

        public static string ToRaw(UnityEngine.Object[] assets)
        {
            return string.Format("=ASSETS({0})", string.Join(",", ToPaths(assets)));
        }

        public override string GetName()
        {
            return "ASSETS";
        }
    }

    public class Enum : FormulaFunction
    {
        public override object Evalute()
        {
            if (this.Parameters == null || this.Parameters.Count < 2)
            {
                return null;
            }

            var typeString = this.Parameters[0].ToString();
            var value = this.Parameters[1].ToString();

            try
            {
                bool found = YadeGlobal.IsEnumExists(typeString);
                return found ? value : string.Empty;
            }
            catch
            {
                return null;
            }
        }

        public override string GetName()
        {
            return "ENUM";
        }
    }
}