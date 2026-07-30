using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sheet;
using Sheet.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Wizard.Editor
{
    public sealed class GoogleSheetBulkImporterWindow : EditorWindow
    {
        [SerializeField] private GoogleSheetBulkImportConfig config;
        [SerializeField] private string ignorePrefix = "Source_";

        private bool isImporting;

        [MenuItem("Tools/Google Sheet/Bulk Importer")]
        private static void Open()
        {
            GetWindow<GoogleSheetBulkImporterWindow>("Bulk Importer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Please select or create a config before bulk importing.");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Config file:");

            EditorGUILayout.BeginHorizontal();
            config = (GoogleSheetBulkImportConfig)EditorGUILayout.ObjectField(
                config,
                typeof(GoogleSheetBulkImportConfig),
                false);

            if (config == null)
            {
                if (GUILayout.Button("New", GUILayout.Width(70f)))
                    CreateConfig();
            }
            else if (GUILayout.Button("Open", GUILayout.Width(70f)))
            {
                GoogleSheetBulkImportConfigWindow.Open(config);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Exclude sheet if its name has prefix:");
            ignorePrefix = EditorGUILayout.TextField(ignorePrefix);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(config == null || isImporting))
            {
                if (GUILayout.Button(isImporting ? "Importing..." : "Import"))
                    ImportAsync();
            }
        }

        private void CreateConfig()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Bulk Import Config",
                "BulkImportConfig",
                "asset",
                string.Empty,
                "Assets/_Project/Resources");

            if (string.IsNullOrEmpty(path))
                return;

            config = CreateInstance<GoogleSheetBulkImportConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            GoogleSheetBulkImportConfigWindow.Open(config);
        }

        private async void ImportAsync()
        {
            isImporting = true;
            Repaint();

            int importedCount = 0;
            var errors = new StringBuilder();
            GoogleSheetData lastImportedSheet = null;

            try
            {
                for (int i = 0; i < config.Items.Count; i++)
                {
                    GoogleSheetBulkImportItem item = config.Items[i];
                    if (item == null ||
                        string.IsNullOrWhiteSpace(item.SourceUrl) ||
                        IsExcluded(item))
                        continue;

                    string label = string.IsNullOrWhiteSpace(item.SheetName)
                        ? $"Row {i + 1}"
                        : item.SheetName;
                    EditorUtility.DisplayProgressBar(
                        "Google Sheet Bulk Importer",
                        label,
                        (i + 1f) / config.Items.Count);

                    try
                    {
                        lastImportedSheet = await ImportItemAsync(item, i);
                        importedCount++;
                    }
                    catch (Exception exception)
                    {
                        errors.AppendLine($"{label}: {exception.Message}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isImporting = false;
                Repaint();
            }

            AssetDatabase.SaveAssets();
            if (lastImportedSheet != null)
            {
                Selection.activeObject = lastImportedSheet;
                GoogleSheetDataWindow.Open(lastImportedSheet);
            }

            if (errors.Length == 0)
            {
                ShowNotification(
                    new GUIContent($"Imported {importedCount} sheet(s)."));
            }
            else
            {
                Debug.LogError($"Google Sheet bulk import completed with errors:\n{errors}");
                ShowNotification(
                    new GUIContent(
                        $"Imported {importedCount} sheet(s) with errors. Check Console."));
            }
        }

        private bool IsExcluded(GoogleSheetBulkImportItem item)
        {
            return !string.IsNullOrEmpty(ignorePrefix) &&
                   !string.IsNullOrEmpty(item.SheetName) &&
                   item.SheetName.StartsWith(ignorePrefix, StringComparison.Ordinal);
        }

        private static async Task<GoogleSheetData> ImportItemAsync(
            GoogleSheetBulkImportItem item,
            int index)
        {
            if (!TryBuildDownloadUrl(item, out string downloadUrl, out string error))
                throw new InvalidOperationException(error);

            if (!TryGetOutputPaths(item, index, out string assetPath, out string fullPath, out error))
                throw new InvalidOperationException(error);

            using var request = UnityWebRequest.Get(downloadUrl);
            request.timeout = 30;

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(request.error);

            string csv = request.downloadHandler.text;
            if (string.IsNullOrEmpty(csv))
                throw new InvalidOperationException("The downloaded CSV is empty.");

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            GoogleSheetData sheet = AssetDatabase.LoadAssetAtPath<GoogleSheetData>(assetPath);
            if (sheet == null)
            {
                if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
                {
                    throw new InvalidOperationException(
                        $"SaveTo already contains another asset type: {assetPath}");
                }

                sheet = CreateInstance<GoogleSheetData>();
                AssetDatabase.CreateAsset(sheet, assetPath);
            }

            List<List<string>> rows = ParseCsv(csv);
            sheet.Rows.Clear();
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                var row = new GoogleSheetDataRow();
                row.Cells.AddRange(rows[rowIndex]);
                sheet.Rows.Add(row);
            }

            EditorUtility.SetDirty(sheet);
            AssetDatabase.SaveAssetIfDirty(sheet);
            return sheet;
        }

        internal static bool TryBuildDownloadUrl(
            GoogleSheetBulkImportItem item,
            out string downloadUrl,
            out string error)
        {
            downloadUrl = null;
            error = null;

            if (item == null ||
                !Uri.TryCreate(item.SourceUrl?.Trim(), UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            {
                error = "SourceUrl must be a valid HTTP or HTTPS URL.";
                return false;
            }

            SheetImportType type = item.Type;
            if (type == SheetImportType.Auto)
            {
                type = string.Equals(
                    uri.Host,
                    "docs.google.com",
                    StringComparison.OrdinalIgnoreCase)
                    ? SheetImportType.Google
                    : SheetImportType.Csv;
            }

            if (type == SheetImportType.Csv)
            {
                downloadUrl = uri.AbsoluteUri;
                return true;
            }

            if (!string.Equals(uri.Host, "docs.google.com", StringComparison.OrdinalIgnoreCase))
            {
                error = "Google imports require a docs.google.com URL.";
                return false;
            }

            const string marker = "/spreadsheets/d/";
            int idStart = uri.AbsolutePath.IndexOf(marker, StringComparison.Ordinal);
            if (idStart < 0)
            {
                error = "The URL does not contain a Google Sheets document ID.";
                return false;
            }

            idStart += marker.Length;
            int idEnd = uri.AbsolutePath.IndexOf('/', idStart);
            string sheetId = idEnd < 0
                ? uri.AbsolutePath[idStart..]
                : uri.AbsolutePath[idStart..idEnd];

            if (string.IsNullOrWhiteSpace(sheetId) || sheetId == "e")
            {
                error = "Use the normal Google Sheets share URL, not a published HTML URL.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(item.SheetName))
            {
                downloadUrl =
                    $"https://docs.google.com/spreadsheets/d/{sheetId}/gviz/tq" +
                    $"?tqx=out:csv&sheet={Uri.EscapeDataString(item.SheetName.Trim())}";
                return true;
            }

            string gid = GetParameter(uri, "gid") ?? "0";
            if (!ulong.TryParse(gid, out _))
            {
                error = "The sheet URL contains an invalid gid.";
                return false;
            }

            downloadUrl =
                $"https://docs.google.com/spreadsheets/d/{sheetId}/export?format=csv&gid={gid}";
            return true;
        }

        private static string GetParameter(Uri uri, string name)
        {
            string value = GetParameter(uri.Query, name);
            return value ?? GetParameter(uri.Fragment, name);
        }

        private static string GetParameter(string parameters, string name)
        {
            if (string.IsNullOrEmpty(parameters))
                return null;

            string[] pairs = parameters.TrimStart('?', '#').Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                int separator = pairs[i].IndexOf('=');
                if (separator < 0 ||
                    !string.Equals(
                        pairs[i][..separator],
                        name,
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                return Uri.UnescapeDataString(pairs[i][(separator + 1)..]);
            }

            return null;
        }

        internal static bool TryGetOutputPaths(
            GoogleSheetBulkImportItem item,
            int index,
            out string assetPath,
            out string fullPath,
            out string error)
        {
            assetPath = item?.SaveTo?.Trim().Replace('\\', '/');
            fullPath = null;
            error = null;

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                error = "SaveTo is empty.";
                return false;
            }

            if (string.IsNullOrEmpty(Path.GetExtension(assetPath)))
            {
                string fileName = FirstNotEmpty(
                    item.SheetName,
                    $"Sheet_{index + 1}");
                assetPath = $"{assetPath.TrimEnd('/')}/{SanitizeFileName(fileName)}.asset";
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                error = "SaveTo must be an .asset file or folder inside Assets.";
                return false;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            string assetsRoot =
                Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                error = "SaveTo must stay inside the project's Assets folder.";
                fullPath = null;
                return false;
            }

            return true;
        }

        private static string FirstNotEmpty(string value, string fallback)
        {
            return !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
        }

        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                fileName = fileName.Replace(invalidChars[i], '_');

            return fileName;
        }

        internal static List<List<string>> ParseCsv(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char character = csv[i];

                if (character == '"')
                {
                    if (insideQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }

                    continue;
                }

                if (!insideQuotes && character == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }

                if (!insideQuotes && (character == '\r' || character == '\n'))
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(row);
                    row = new List<string>();

                    if (character == '\r' &&
                        i + 1 < csv.Length &&
                        csv[i + 1] == '\n')
                    {
                        i++;
                    }

                    continue;
                }

                cell.Append(character);
            }

            if (insideQuotes)
                throw new FormatException("CSV contains an unclosed quoted cell.");

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }

        [MenuItem("Tools/Google Sheet/Run Bulk Importer Self Check")]
        private static void RunSelfCheck()
        {
            var item = new GoogleSheetBulkImportItem
            {
                SourceUrl =
                    "https://docs.google.com/spreadsheets/d/test-sheet/edit#gid=123",
                SaveTo = "Assets/_Project/Resources/Config/",
                SheetName = "Level Config",
            };

            const string expectedUrl =
                "https://docs.google.com/spreadsheets/d/test-sheet/gviz/tq" +
                "?tqx=out:csv&sheet=Level%20Config";

            if (!TryBuildDownloadUrl(item, out string url, out _) ||
                url != expectedUrl ||
                !TryGetOutputPaths(item, 0, out string path, out _, out _) ||
                path != "Assets/_Project/Resources/Config/Level Config.asset")
            {
                throw new InvalidOperationException("Bulk importer self-check failed.");
            }

            List<List<string>> csvRows = ParseCsv(
                "Id,Name\n1,\"Fire, Ball\"\n2,\"Double \"\"Quote\"\"\"");
            if (csvRows.Count != 3 ||
                csvRows[1][1] != "Fire, Ball" ||
                csvRows[2][1] != "Double \"Quote\"")
            {
                throw new InvalidOperationException("CSV parser self-check failed.");
            }

            if (GoogleSheetGridGUI.ResizeColumn(100f, 25f) != 150f ||
                GoogleSheetGridGUI.ResizeColumn(50f, -25f) != 40f)
            {
                throw new InvalidOperationException(
                    "Column resize self-check failed.");
            }

            if (GoogleSheetGridGUI.ResizeRow(25f, 10f) != 45f ||
                GoogleSheetGridGUI.ResizeRow(25f, -10f) != 20f)
            {
                throw new InvalidOperationException(
                    "Row resize self-check failed.");
            }

            Debug.Log("Google Sheet Bulk Importer self-check passed.");
        }
    }

    public sealed class GoogleSheetBulkImportConfigWindow : EditorWindow
    {
        private const int ColumnCount = 15;
        private const int EditableColumnCount = 4;
        private const int MinimumRowCount = 20;
        private const float RowNumberWidth = 62f;
        private const float DefaultColumnWidth = 140f;
        private const float DefaultRowHeight = 25f;

        private static readonly string[] Aliases =
        {
            "SourceUrl",
            "SaveTo",
            "SheetName",
            "Type",
        };

        [SerializeField] private GoogleSheetBulkImportConfig config;
        [SerializeField] private int selectedRow;
        [SerializeField] private int selectedColumn;
        [SerializeField] private string search;
        [SerializeField] private List<float> columnWidths = new();
        [SerializeField] private List<float> rowHeights = new();

        private Vector2 scroll;

        public static void Open(GoogleSheetBulkImportConfig target)
        {
            var window = GetWindow<GoogleSheetBulkImportConfigWindow>(target.name);
            window.titleContent = new GUIContent(target.name);
            window.config = target;
            window.minSize = new Vector2(900f, 480f);
            window.Show();
        }

        private void OnGUI()
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("Select a bulk import config.", MessageType.Info);
                return;
            }

            GoogleSheetGridGUI.DrawTabBar(config.name);
            DrawToolbar();
            DrawFormulaBar();
            DrawGrid();
            GoogleSheetGridGUI.DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(28f));

            if (GoogleSheetGridGUI.ToolbarButton("+", "Add row"))
                AddRow();

            if (GoogleSheetGridGUI.ToolbarButton("−", "Remove selected row"))
                RemoveSelectedRow();

            GUILayout.Space(4f);

            if (GoogleSheetGridGUI.ToolbarButton("↶", "Undo"))
                Undo.PerformUndo();

            if (GoogleSheetGridGUI.ToolbarButton("↷", "Redo"))
                Undo.PerformRedo();

            if (GoogleSheetGridGUI.ToolbarButton("▣", "Copy selected cell"))
                EditorGUIUtility.systemCopyBuffer = GetCellText(selectedRow, selectedColumn);

            if (GoogleSheetGridGUI.ToolbarButton("✂", "Cut selected cell"))
            {
                EditorGUIUtility.systemCopyBuffer = GetCellText(selectedRow, selectedColumn);
                SetCellText(selectedRow, selectedColumn, string.Empty);
            }

            if (GoogleSheetGridGUI.ToolbarButton("▢", "Paste into selected cell"))
            {
                SetCellText(
                    selectedRow,
                    selectedColumn,
                    EditorGUIUtility.systemCopyBuffer);
            }

            if (GoogleSheetGridGUI.ToolbarButton("Σ", "Save asset"))
                AssetDatabase.SaveAssets();

            if (GoogleSheetGridGUI.ToolbarButton("?", "Bulk importer config help"))
            {
                ShowNotification(
                    new GUIContent(
                        "A: SourceUrl   B: SaveTo   C: SheetName   D: Type"));
            }

            GUILayout.FlexibleSpace();
            search = GUILayout.TextField(
                search,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200f));
            EditorGUILayout.EndHorizontal();
        }

        private void AddRow()
        {
            Undo.RecordObject(config, "Add bulk import row");
            config.Items.Add(new GoogleSheetBulkImportItem());
            selectedRow = config.Items.Count - 1;
            EditorUtility.SetDirty(config);
        }

        private void RemoveSelectedRow()
        {
            if (selectedRow < 0 || selectedRow >= config.Items.Count)
                return;

            Undo.RecordObject(config, "Remove bulk import row");
            config.Items.RemoveAt(selectedRow);
            if (selectedRow < rowHeights.Count)
                rowHeights.RemoveAt(selectedRow);
            selectedRow = Mathf.Clamp(selectedRow, 0, config.Items.Count - 1);
            EditorUtility.SetDirty(config);
        }

        private void DrawFormulaBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                GetCellName(selectedRow, selectedColumn),
                EditorStyles.helpBox,
                GUILayout.Width(RowNumberWidth));

            using (new EditorGUI.DisabledScope(selectedColumn >= EditableColumnCount))
            {
                string value = GetCellText(selectedRow, selectedColumn);
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.TextField(
                    value,
                    GoogleSheetGridGUI.FormulaStyle,
                    GUILayout.Height(DefaultRowHeight));
                if (EditorGUI.EndChangeCheck())
                    SetCellText(selectedRow, selectedColumn, value);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            int rowCount = Mathf.Max(MinimumRowCount, config.Items.Count + 1);
            GoogleSheetGridGUI.EnsureColumnWidths(
                columnWidths,
                ColumnCount,
                DefaultColumnWidth);
            GoogleSheetGridGUI.EnsureRowHeights(
                rowHeights,
                rowCount,
                DefaultRowHeight);
            float width = RowNumberWidth +
                          GoogleSheetGridGUI.GetTotalWidth(
                              columnWidths,
                              ColumnCount);
            float height = DefaultRowHeight * 2f +
                           GoogleSheetGridGUI.GetTotalHeight(
                               rowHeights,
                               rowCount);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect canvas = GUILayoutUtility.GetRect(width, height);

            DrawHeaderCell(
                new Rect(
                    canvas.x,
                    canvas.y,
                    RowNumberWidth,
                    DefaultRowHeight),
                "Alias");

            for (int column = 0; column < ColumnCount; column++)
            {
                string alias = column < Aliases.Length ? Aliases[column] : string.Empty;
                DrawHeaderCell(GetCellRect(canvas, -2, column), alias);
            }

            DrawHeaderCell(
                new Rect(
                    canvas.x,
                    canvas.y + DefaultRowHeight,
                    RowNumberWidth,
                    DefaultRowHeight),
                string.Empty);

            for (int column = 0; column < ColumnCount; column++)
            {
                Rect headerRect = GetCellRect(canvas, -1, column);
                GoogleSheetGridGUI.DrawHeaderCell(
                    headerRect,
                    ((char)('A' + column)).ToString(),
                    column == selectedColumn);
                GoogleSheetGridGUI.HandleColumnResize(
                    new Rect(
                        headerRect.x,
                        canvas.y,
                        headerRect.width,
                        height),
                    column,
                    columnWidths,
                    Repaint);
            }

            for (int row = 0; row < rowCount; row++)
            {
                float rowY = canvas.y + DefaultRowHeight * 2f +
                             GoogleSheetGridGUI.GetRowOffset(
                                 rowHeights,
                                 row);
                Rect rowRect = new(
                    canvas.x,
                    rowY,
                    width,
                    rowHeights[row]);
                GoogleSheetGridGUI.DrawHeaderCell(
                    new Rect(
                        canvas.x,
                        rowY,
                        RowNumberWidth,
                        rowHeights[row]),
                    (row + 1).ToString(),
                    row == selectedRow);
                GoogleSheetGridGUI.HandleRowResize(
                    rowRect,
                    row,
                    rowHeights,
                    Repaint);

                for (int column = 0; column < ColumnCount; column++)
                    DrawDataCell(GetCellRect(canvas, row, column), row, column);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDataCell(Rect rect, int row, int column)
        {
            string text = GetCellText(row, column);
            bool selected = row == selectedRow && column == selectedColumn;
            bool matchesSearch =
                !string.IsNullOrWhiteSpace(search) &&
                text.Contains(search, StringComparison.OrdinalIgnoreCase);
            GoogleSheetGridGUI.DrawCell(rect, selected, matchesSearch);

            if (Event.current.type == EventType.MouseDown &&
                rect.Contains(Event.current.mousePosition))
            {
                selectedRow = row;
                selectedColumn = column;
                Repaint();
            }

            Rect fieldRect = new Rect(
                rect.x + 1f,
                rect.y + 1f,
                rect.width - 2f,
                rect.height - 2f);

            if (column >= EditableColumnCount)
                return;

            EditorGUI.BeginChangeCheck();
            text = EditorGUI.TextField(
                fieldRect,
                text,
                GoogleSheetGridGUI.CellStyle);
            if (EditorGUI.EndChangeCheck())
                SetCellText(row, column, text);
        }

        private static void DrawHeaderCell(Rect rect, string text)
        {
            GoogleSheetGridGUI.DrawHeaderCell(rect, text);
        }

        private Rect GetCellRect(Rect canvas, int row, int column)
        {
            float y = row < 0
                ? canvas.y + (row + 2) * DefaultRowHeight
                : canvas.y + DefaultRowHeight * 2f +
                  GoogleSheetGridGUI.GetRowOffset(rowHeights, row);
            float height = row < 0
                ? DefaultRowHeight
                : rowHeights[row];

            return new Rect(
                canvas.x + RowNumberWidth +
                GoogleSheetGridGUI.GetColumnOffset(columnWidths, column),
                y,
                columnWidths[column],
                height);
        }

        private string GetCellText(int row, int column)
        {
            if (row < 0 || row >= config.Items.Count || column < 0)
                return string.Empty;

            GoogleSheetBulkImportItem item = config.Items[row];
            if (item == null)
                return string.Empty;

            return column switch
            {
                0 => item.SourceUrl ?? string.Empty,
                1 => item.SaveTo ?? string.Empty,
                2 => item.SheetName ?? string.Empty,
                3 => item.Type switch
                {
                    SheetImportType.Google => "google",
                    SheetImportType.Csv => "csv",
                    _ => string.Empty,
                },
                _ => string.Empty,
            };
        }

        private void SetCellText(int row, int column, string value)
        {
            if (row < 0 || column < 0 || column >= EditableColumnCount)
                return;

            Undo.RecordObject(config, "Edit bulk import cell");
            while (config.Items.Count <= row)
                config.Items.Add(new GoogleSheetBulkImportItem());

            GoogleSheetBulkImportItem item = config.Items[row] ??
                                             new GoogleSheetBulkImportItem();
            config.Items[row] = item;

            switch (column)
            {
                case 0:
                    item.SourceUrl = value;
                    break;
                case 1:
                    item.SaveTo = value;
                    break;
                case 2:
                    item.SheetName = value;
                    break;
                case 3:
                    item.Type = ParseImportType(value);
                    break;
            }

            TrimEmptyTrailingRows();
            EditorUtility.SetDirty(config);
        }

        private void TrimEmptyTrailingRows()
        {
            while (config.Items.Count > 1 &&
                   IsEmpty(config.Items[config.Items.Count - 1]))
            {
                config.Items.RemoveAt(config.Items.Count - 1);
            }
        }

        private static bool IsEmpty(GoogleSheetBulkImportItem item)
        {
            return item == null ||
                   (string.IsNullOrWhiteSpace(item.SourceUrl) &&
                    string.IsNullOrWhiteSpace(item.SheetName) &&
                    item.Type == SheetImportType.Auto &&
                    (string.IsNullOrWhiteSpace(item.SaveTo) ||
                     item.SaveTo == "Assets/_Project/Resources/Config/"));
        }

        private static SheetImportType ParseImportType(string value)
        {
            if (string.Equals(value, "google", StringComparison.OrdinalIgnoreCase))
                return SheetImportType.Google;

            return string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase)
                ? SheetImportType.Csv
                : SheetImportType.Auto;
        }

        private static string GetCellName(int row, int column)
        {
            int safeColumn = Mathf.Clamp(column, 0, ColumnCount - 1);
            return $"{(char)('A' + safeColumn)}{Mathf.Max(0, row) + 1}";
        }

        private void OnDestroy()
        {
            if (config != null)
                AssetDatabase.SaveAssets();
        }
    }

    internal static class GoogleSheetGridGUI
    {
        private const float MinimumColumnWidth = 40f;
        private const float MinimumRowHeight = 20f;
        private const float ResizeHandleWidth = 14f;
        private const float ResizeSensitivity = 2f;
        private static readonly int ColumnResizeHint =
            "GoogleSheetColumnResize".GetHashCode();
        private static readonly int RowResizeHint =
            "GoogleSheetRowResize".GetHashCode();

        private static readonly Color TabBarColor = new(0.055f, 0.055f, 0.055f, 1f);
        private static readonly Color TabColor = new(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color CellColor = new(0.195f, 0.195f, 0.195f, 1f);
        private static readonly Color GridColor = new(0.27f, 0.27f, 0.27f, 1f);
        private static readonly Color HeaderColor = new(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color HeaderSelectedColor = new(0.22f, 0.27f, 0.35f, 1f);
        private static readonly Color SelectionColor = new(0.16f, 0.48f, 0.95f, 1f);
        private static readonly Color SearchColor = new(0.55f, 0.42f, 0.08f, 0.55f);

        private static GUIStyle cellStyle;
        private static GUIStyle formulaStyle;
        private static GUIStyle tabStyle;
        private static GUIStyle headerStyle;

        public static GUIStyle CellStyle => cellStyle ??= CreateCellStyle();
        public static GUIStyle FormulaStyle => formulaStyle ??= CreateFormulaStyle();

        public static void EnsureColumnWidths(
            List<float> widths,
            int count,
            float defaultWidth)
        {
            while (widths.Count < count)
                widths.Add(defaultWidth);
        }

        public static void EnsureRowHeights(
            List<float> heights,
            int count,
            float defaultHeight)
        {
            while (heights.Count < count)
                heights.Add(defaultHeight);
        }

        public static float GetTotalWidth(List<float> widths, int count)
        {
            float width = 0f;
            for (int i = 0; i < count; i++)
                width += widths[i];

            return width;
        }

        public static float GetColumnOffset(List<float> widths, int column)
        {
            float offset = 0f;
            for (int i = 0; i < column; i++)
                offset += widths[i];

            return offset;
        }

        public static float GetTotalHeight(List<float> heights, int count)
        {
            float height = 0f;
            for (int i = 0; i < count; i++)
                height += heights[i];

            return height;
        }

        public static float GetRowOffset(List<float> heights, int row)
        {
            float offset = 0f;
            for (int i = 0; i < row; i++)
                offset += heights[i];

            return offset;
        }

        public static void HandleColumnResize(
            Rect columnRect,
            int column,
            List<float> widths,
            Action repaint)
        {
            Rect resizeRect = new(
                columnRect.xMax - ResizeHandleWidth * 0.5f,
                columnRect.y,
                ResizeHandleWidth,
                columnRect.height);
            EditorGUIUtility.AddCursorRect(
                resizeRect,
                MouseCursor.ResizeHorizontal);

            int controlId = GUIUtility.GetControlID(
                ColumnResizeHint + column,
                FocusType.Passive,
                resizeRect);
            Event current = Event.current;

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown
                    when current.button == 0 &&
                         resizeRect.Contains(current.mousePosition):
                    GUIUtility.hotControl = controlId;
                    current.Use();
                    break;

                case EventType.MouseDrag
                    when GUIUtility.hotControl == controlId:
                    widths[column] = ResizeColumn(
                        widths[column],
                        current.delta.x);
                    GUI.changed = true;
                    repaint();
                    current.Use();
                    break;

                case EventType.MouseUp
                    when GUIUtility.hotControl == controlId:
                case EventType.Ignore
                    when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
        }

        internal static float ResizeColumn(float width, float delta)
        {
            return Mathf.Max(
                MinimumColumnWidth,
                width + delta * ResizeSensitivity);
        }

        public static void HandleRowResize(
            Rect rowRect,
            int row,
            List<float> heights,
            Action repaint)
        {
            Rect resizeRect = new(
                rowRect.x,
                rowRect.yMax - ResizeHandleWidth * 0.5f,
                rowRect.width,
                ResizeHandleWidth);
            EditorGUIUtility.AddCursorRect(
                resizeRect,
                MouseCursor.ResizeVertical);

            int controlId = GUIUtility.GetControlID(
                RowResizeHint + row,
                FocusType.Passive,
                resizeRect);
            Event current = Event.current;

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown
                    when current.button == 0 &&
                         resizeRect.Contains(current.mousePosition):
                    GUIUtility.hotControl = controlId;
                    current.Use();
                    break;

                case EventType.MouseDrag
                    when GUIUtility.hotControl == controlId:
                    heights[row] = ResizeRow(
                        heights[row],
                        current.delta.y);
                    GUI.changed = true;
                    repaint();
                    current.Use();
                    break;

                case EventType.MouseUp
                    when GUIUtility.hotControl == controlId:
                case EventType.Ignore
                    when GUIUtility.hotControl == controlId:
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
        }

        internal static float ResizeRow(float height, float delta)
        {
            return Mathf.Max(
                MinimumRowHeight,
                height + delta * ResizeSensitivity);
        }

        public static void DrawTabBar(string title)
        {
            Rect bar = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bar, TabBarColor);

            float width = Mathf.Max(
                115f,
                EditorStyles.label.CalcSize(new GUIContent(title)).x + 24f);
            Rect tab = new(bar.x, bar.y, width, bar.height);
            EditorGUI.DrawRect(tab, TabColor);
            GUI.Label(tab, title, tabStyle ??= CreateTabStyle());
        }

        public static bool ToolbarButton(string text, string tooltip)
        {
            return GUILayout.Button(
                new GUIContent(text, tooltip),
                EditorStyles.toolbarButton,
                GUILayout.Width(27f),
                GUILayout.Height(25f));
        }

        public static void DrawCell(
            Rect rect,
            bool selected,
            bool searchMatch)
        {
            EditorGUI.DrawRect(rect, CellColor);
            DrawGridLines(rect);

            if (searchMatch)
                EditorGUI.DrawRect(rect, SearchColor);

            if (!selected)
                return;

            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, rect.width, 2f),
                SelectionColor);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - 2f, rect.width, 2f),
                SelectionColor);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, 2f, rect.height),
                SelectionColor);
            EditorGUI.DrawRect(
                new Rect(rect.xMax - 2f, rect.y, 2f, rect.height),
                SelectionColor);

            Rect handle = new(rect.xMax - 4f, rect.yMax - 4f, 7f, 7f);
            EditorGUI.DrawRect(handle, Color.white);
            EditorGUI.DrawRect(
                new Rect(handle.x + 1f, handle.y + 1f, 5f, 5f),
                SelectionColor);
        }

        public static void DrawHeaderCell(
            Rect rect,
            string text,
            bool selected = false)
        {
            EditorGUI.DrawRect(rect, selected ? HeaderSelectedColor : HeaderColor);
            DrawGridLines(rect);
            GUI.Label(rect, text, headerStyle ??= CreateHeaderStyle());
        }

        public static void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: 1.0.0  \u263A", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawGridLines(Rect rect)
        {
            EditorGUI.DrawRect(
                new Rect(rect.xMax - 1f, rect.y, 1f, rect.height),
                GridColor);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                GridColor);
        }

        private static GUIStyle CreateCellStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(6, 4, 0, 0),
            };
        }

        private static GUIStyle CreateFormulaStyle()
        {
            return new GUIStyle(EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                border = new RectOffset(),
                margin = new RectOffset(),
                padding = new RectOffset(8, 4, 0, 0),
            };
        }

        private static GUIStyle CreateTabStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 4, 0, 0),
            };
        }

        private static GUIStyle CreateHeaderStyle()
        {
            return new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                clipping = TextClipping.Clip,
            };
        }
    }
}
