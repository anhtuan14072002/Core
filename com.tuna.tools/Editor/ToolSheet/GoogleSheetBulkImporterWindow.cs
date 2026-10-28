using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Sheet.Editor
{
    public sealed class GoogleSheetBulkImporterWindow : EditorWindow
    {
        [SerializeField] private GoogleSheetBulkImportConfig config;
        [SerializeField] private string ignorePrefix = "Source_";

        private bool isImporting;

        // Mở cửa sổ import nhiều Google Sheet từ menu Tools.
        [MenuItem("Tools/Google Sheet/Bulk Importer")]
        private static void Open()
        {
            GetWindow<GoogleSheetBulkImporterWindow>("Bulk Importer");
        }

        // Vẽ giao diện chọn config và bắt đầu quá trình bulk import.
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

        // Tạo asset cấu hình bulk import mới và mở cửa sổ chỉnh sửa.
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

        // Import lần lượt các sheet hợp lệ và tổng hợp lỗi để báo sau cùng.
        private async void ImportAsync()
        {
            isImporting = true;
            Repaint();

            int importedCount = 0;
            var errors = new StringBuilder();

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
                        await ImportItemAsync(item, i);
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

        // Kiểm tra sheet có bị bỏ qua theo prefix đã cấu hình hay không.
        private bool IsExcluded(GoogleSheetBulkImportItem item)
        {
            return !string.IsNullOrEmpty(ignorePrefix) &&
                   !string.IsNullOrEmpty(item.SheetName) &&
                   item.SheetName.StartsWith(ignorePrefix, StringComparison.Ordinal);
        }

        // Tải một sheet, parse CSV và lưu kết quả vào GoogleSheetData asset.
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

        // Chuyển URL Google Sheet hoặc CSV thành URL tải dữ liệu trực tiếp.
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

        // Lấy tham số từ cả query và fragment của URL.
        private static string GetParameter(Uri uri, string name)
        {
            string value = GetParameter(uri.Query, name);
            return value ?? GetParameter(uri.Fragment, name);
        }

        // Tìm và giải mã một tham số trong chuỗi query.
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

        // Tạo asset path và full path an toàn cho sheet sau khi import.
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

        // Trả về giá trị chính nếu có, nếu không dùng giá trị dự phòng.
        private static string FirstNotEmpty(string value, string fallback)
        {
            return !string.IsNullOrWhiteSpace(value) ? value.Trim() : fallback;
        }

        // Loại bỏ các ký tự không hợp lệ khỏi tên file output.
        private static string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
                fileName = fileName.Replace(invalidChars[i], '_');

            return fileName;
        }

        // Parse nội dung CSV, hỗ trợ dấu phẩy, xuống dòng và dấu nháy trong ô.
        internal static List<List<string>> ParseCsv(string csv, char delimiter = ',')
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char character = csv[i];

                if (character == '"' && (insideQuotes || cell.Length == 0))
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

                if (!insideQuotes && character == delimiter)
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

            if (cell.Length > 0 || row.Count > 0 || (csv.Length > 0 && csv[csv.Length - 1] == '"'))
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }

            return rows;
        }

        // Chạy kiểm tra nhanh cho URL, output path, CSV parser và resize helper.
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
        private readonly List<float> columnOffsets = new();
        private readonly List<float> rowOffsets = new();
        private int anchorRow;
        private int anchorColumn;
        private bool draggingSelection;
        private bool movingSelection;
        private bool fillingSelection;
        private bool fillVertical;
        private Vector2Int fillEnd;
        private Vector2Int dragOrigin;
        private Vector2Int dropCell;
        private bool editingCell;
        private bool focusCell;
        private bool gridFocused;
        private bool selectingRows;
        private bool selectingColumns;
        private static readonly char[] QuotedCharacters = { '\t', '\r', '\n', '"' };
        private static readonly Color SelectedCellColor = new(0.24f, 0.40f, 0.33f, 1f);
        private static readonly Color ActiveCellColor = new(0.28f, 0.28f, 0.28f, 1f);

        private int FirstRow => Mathf.Min(anchorRow, selectedRow);
        private int LastRow => Mathf.Max(anchorRow, selectedRow);
        private int FirstColumn => Mathf.Min(anchorColumn, selectedColumn);
        private int LastColumn => Mathf.Max(anchorColumn, selectedColumn);

        private void OnEnable() => Undo.undoRedoPerformed += Repaint;
        private void OnDisable() => Undo.undoRedoPerformed -= Repaint;

        // Mở cửa sổ chỉnh sửa cho asset bulk import config được chọn.
        public static void Open(GoogleSheetBulkImportConfig target)
        {
            var window = GetWindow<GoogleSheetBulkImportConfigWindow>(target.name);
            window.titleContent = new GUIContent(target.name);
            window.config = target;
            window.SelectCell(0, 0, false);
            window.minSize = new Vector2(900f, 480f);
            window.Show();
        }

        // Vẽ giao diện dạng bảng để chỉnh sửa các dòng cấu hình import.
        private void OnGUI()
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("Select a bulk import config.", MessageType.Info);
                return;
            }

            HandleKeyboard();
            GoogleSheetGridGUI.DrawTabBar(config.name);
            DrawToolbar();
            DrawFormulaBar();
            DrawGrid();
            GoogleSheetGridGUI.DrawFooter();
        }

        // Vẽ thanh công cụ tìm kiếm, thêm và xóa dòng config.
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

            if (GoogleSheetGridGUI.ToolbarButton("▣", "Copy selection (Ctrl+C)"))
                CopySelection(false);

            if (GoogleSheetGridGUI.ToolbarButton("✂", "Cut selection (Ctrl+X)"))
                CopySelection(true);

            if (GoogleSheetGridGUI.ToolbarButton("▢", "Paste selection (Ctrl+V)"))
                PasteSelection();

            if (GoogleSheetGridGUI.ToolbarButton("Σ", "Save asset"))
                AssetDatabase.SaveAssets();

            if (GoogleSheetGridGUI.ToolbarButton("?", "Bulk importer config help"))
            {
                ShowNotification(
                    new GUIContent(
                        "A: SourceUrl   B: SaveTo   C: SheetName   D: Type"));
            }

            GUILayout.FlexibleSpace();
            GUI.SetNextControlName("SheetSearch");
            search = GUILayout.TextField(
                search,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200f));
            EditorGUILayout.EndHorizontal();
            if (GUI.GetNameOfFocusedControl() == "SheetSearch")
                gridFocused = false;
        }

        // Thêm một dòng import mới vào cuối config.
        private void AddRow()
        {
            Undo.RecordObject(config, "Add bulk import row");
            config.Items.Add(new GoogleSheetBulkImportItem());
            SelectCell(config.Items.Count - 1, selectedColumn, false);
            EditorUtility.SetDirty(config);
        }

        // Xóa dòng config đang được chọn.
        private void RemoveSelectedRow()
        {
            if (selectedRow < 0 || selectedRow >= config.Items.Count)
                return;

            Undo.RecordObject(config, "Remove bulk import row");
            config.Items.RemoveAt(selectedRow);
            if (selectedRow < rowHeights.Count)
                rowHeights.RemoveAt(selectedRow);
            SelectCell(Mathf.Clamp(selectedRow, 0, Mathf.Max(0, config.Items.Count - 1)), selectedColumn, false);
            EditorUtility.SetDirty(config);
        }

        // Vẽ thanh chỉnh sửa giá trị của ô đang chọn.
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
                GUI.SetNextControlName("SheetFormula");
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.TextField(
                    value,
                    GoogleSheetGridGUI.FormulaStyle,
                    GUILayout.Height(DefaultRowHeight));
                if (EditorGUI.EndChangeCheck())
                    SetCellText(selectedRow, selectedColumn, value);
            }

            EditorGUILayout.EndHorizontal();
            if (GUI.GetNameOfFocusedControl() == "SheetFormula")
            {
                gridFocused = false;
                editingCell = false;
            }
        }

        // Vẽ toàn bộ bảng config, header, cell và vùng resize.
        private void DrawGrid()
        {
            int rowCount = Mathf.Max(MinimumRowCount, Mathf.Max(config.Items.Count + 1, LastRow + 1));
            GoogleSheetGridGUI.EnsureColumnWidths(
                columnWidths,
                ColumnCount,
                DefaultColumnWidth);
            GoogleSheetGridGUI.EnsureRowHeights(
                rowHeights,
                rowCount,
                DefaultRowHeight);
            GoogleSheetGridGUI.BuildOffsets(columnWidths, columnOffsets, ColumnCount);
            GoogleSheetGridGUI.BuildOffsets(rowHeights, rowOffsets, rowCount);
            float width = RowNumberWidth + columnOffsets[ColumnCount];
            float height = DefaultRowHeight * 2f + rowOffsets[rowCount];

            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect canvas = GUILayoutUtility.GetRect(width, height);
            int selectionControl = GUIUtility.GetControlID("SheetSelection".GetHashCode(), FocusType.Passive);
            int firstVisibleRow = Mathf.Max(0, GoogleSheetGridGUI.GetItemAtOffset(rowOffsets, scroll.y - DefaultRowHeight * 2f) - 1);
            int lastVisibleRow = Mathf.Min(rowCount - 1, GoogleSheetGridGUI.GetItemAtOffset(rowOffsets, scroll.y + position.height - DefaultRowHeight * 2f) + 1);
            int firstVisibleColumn = Mathf.Max(0, GoogleSheetGridGUI.GetItemAtOffset(columnOffsets, scroll.x - RowNumberWidth) - 1);
            int lastVisibleColumn = Mathf.Min(ColumnCount - 1, GoogleSheetGridGUI.GetItemAtOffset(columnOffsets, scroll.x + position.width - RowNumberWidth) + 1);


            DrawHeaderCell(
                new Rect(
                    canvas.x,
                    canvas.y,
                    RowNumberWidth,
                    DefaultRowHeight),
                "Alias");

            for (int column = firstVisibleColumn; column <= lastVisibleColumn; column++)
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

            for (int column = firstVisibleColumn; column <= lastVisibleColumn; column++)
            {
                Rect headerRect = GetCellRect(canvas, -1, column);
                GoogleSheetGridGUI.DrawHeaderCell(
                    headerRect,
                    ((char)('A' + column)).ToString(),
                    column >= FirstColumn && column <= LastColumn);
                GoogleSheetGridGUI.HandleColumnResize(
                    new Rect(
                        headerRect.x,
                        canvas.y,
                        headerRect.width,
                        DefaultRowHeight * 2f),
                    column,
                    columnWidths,
                    Repaint);
            }

            for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
            {
                float rowY = canvas.y + DefaultRowHeight * 2f +
                             rowOffsets[row];
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
                    row >= FirstRow && row <= LastRow);
                GoogleSheetGridGUI.HandleRowResize(
                    new Rect(rowRect.x, rowRect.y, RowNumberWidth, rowRect.height),
                    row,
                    rowHeights,
                    Repaint);

                for (int column = firstVisibleColumn; column <= lastVisibleColumn; column++)
                    DrawDataCell(GetCellRect(canvas, row, column), row, column);
            }

            HandleGridMouse(canvas, rowCount, EditableColumnCount, selectionControl);
            EditorGUILayout.EndScrollView();
        }

        // Vẽ và xử lý nhập liệu cho một ô config.
        private void DrawDataCell(Rect rect, int row, int column)
        {
            if (Event.current.type != EventType.Repaint &&
                !(editingCell && row == selectedRow && column == selectedColumn))
                return;

            string value = GetCellText(row, column);
            bool selected = row >= FirstRow && row <= LastRow &&
                            column >= FirstColumn && column <= LastColumn;
            bool matchesSearch =
                !string.IsNullOrWhiteSpace(search) &&
                value.Contains(search, StringComparison.OrdinalIgnoreCase);

            GoogleSheetGridGUI.DrawCell(rect, false, matchesSearch);
            if (selected)
            {
                bool active = row == selectedRow && column == selectedColumn;
                EditorGUI.DrawRect(
                    new Rect(rect.x, rect.y, rect.width - 1f, rect.height - 1f),
                    active ? ActiveCellColor : SelectedCellColor);
            }

            Rect fieldRect = new Rect(
                rect.x + 1f,
                rect.y + 1f,
                rect.width - 2f,
                rect.height - 2f);

            if (editingCell && row == selectedRow && column == selectedColumn)
            {
                GUI.SetNextControlName("SheetCell");
                EditorGUI.BeginChangeCheck();
                value = EditorGUI.TextField(fieldRect, value, GoogleSheetGridGUI.CellStyle);
                if (EditorGUI.EndChangeCheck())
                    SetCellText(row, column, value);
                if (focusCell)
                {
                    GUI.FocusControl("SheetCell");
                    focusCell = false;
                }
            }
            else
                GUI.Label(fieldRect, value, GoogleSheetGridGUI.CellStyle);
        }

        private void SelectCell(int row, int column, bool extend)
        {
            selectedRow = Mathf.Max(0, row);
            selectedColumn = Mathf.Clamp(column, 0, EditableColumnCount - 1);
            if (!extend)
            {
                anchorRow = selectedRow;
                anchorColumn = selectedColumn;
            }
            editingCell = false;
            gridFocused = true;
            if (Event.current != null)
                GUI.FocusControl(null);
            Repaint();
        }

        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (!gridFocused)
                return;

            if (editingCell)
            {
                if (current.type == EventType.KeyDown &&
                    (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter ||
                     current.keyCode == KeyCode.Tab || current.keyCode == KeyCode.Escape))
                {
                    int step = current.shift ? -1 : 1;
                    SelectCell(selectedRow + (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter ? step : 0),
                        selectedColumn + (current.keyCode == KeyCode.Tab ? step : 0), false);
                    current.Use();
                }
                return;
            }

            string command = null;
            if (current.type == EventType.ValidateCommand || current.type == EventType.ExecuteCommand)
                command = current.commandName;
            else if (current.type == EventType.KeyDown && (current.control || current.command))
            {
                switch (current.keyCode)
                {
                    case KeyCode.C: command = "Copy"; break;
                    case KeyCode.X: command = "Cut"; break;
                    case KeyCode.V: command = "Paste"; break;
                    case KeyCode.A: command = "SelectAll"; break;
                    case KeyCode.Z: command = current.shift ? "Redo" : "Undo"; break;
                    case KeyCode.Y: command = "Redo"; break;
                }
            }

            if (command == "Copy" || command == "Cut" || command == "Paste" ||
                command == "SelectAll" || command == "Undo" || command == "Redo")
            {
                if (current.type != EventType.ValidateCommand)
                {
                    switch (command)
                    {
                        case "Copy": CopySelection(false); break;
                        case "Cut": CopySelection(true); break;
                        case "Paste": PasteSelection(); break;
                        case "Undo": Undo.PerformUndo(); break;
                        case "Redo": Undo.PerformRedo(); break;
                        case "SelectAll":
                            anchorRow = anchorColumn = 0;
                            SelectCell(Mathf.Max(0, config.Items.Count - 1), Mathf.Max(0, EditableColumnCount - 1), true);
                            break;
                    }
                }
                current.Use();
                return;
            }

            if (current.type != EventType.KeyDown)
                return;
            int row = selectedRow;
            int column = selectedColumn;
            switch (current.keyCode)
            {
                case KeyCode.LeftArrow: column--; break;
                case KeyCode.RightArrow: column++; break;
                case KeyCode.UpArrow: row--; break;
                case KeyCode.DownArrow: row++; break;
                case KeyCode.Tab: column += current.shift ? -1 : 1; break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter: row += current.shift ? -1 : 1; break;
                case KeyCode.Delete:
                case KeyCode.Backspace:
                    ClearSelection();
                    current.Use();
                    return;
                case KeyCode.F2:
                    editingCell = focusCell = true;
                    current.Use();
                    return;
                default:
                    if (current.character >= ' ' && !current.control && !current.command && !current.alt)
                    {
                        SetCellText(row, column, current.character.ToString());
                        editingCell = focusCell = true;
                        current.Use();
                    }
                    return;
            }
            SelectCell(row, column, current.shift && current.keyCode != KeyCode.Tab);
            current.Use();
        }

        private void HandleGridMouse(Rect canvas, int rowCount, int columnCount, int id)
        {
            Event current = Event.current;
            if (current.type == EventType.Layout || current.type == EventType.Used)
                return;
            Rect first = GetCellRect(canvas, FirstRow, FirstColumn);
            Rect last = GetCellRect(canvas, LastRow, LastColumn);
            Rect selection = Rect.MinMaxRect(first.x, first.y, last.xMax, last.yMax);
            DrawSelectionBorder(selection);
            Rect fillHandle = new(selection.xMax - 8f, selection.yMax - 8f, 8f, 8f);
            if (!editingCell)
            {
                EditorGUI.DrawRect(fillHandle, Color.white);
                EditorGUI.DrawRect(new Rect(fillHandle.x + 1f, fillHandle.y + 1f, 6f, 6f), new Color(0.15f, 0.65f, 0.4f));
                EditorGUIUtility.AddCursorRect(fillHandle, MouseCursor.ArrowPlus);
            }
            if (fillingSelection)
            {
                Rect fillFirst = GetCellRect(canvas, Mathf.Min(FirstRow, fillEnd.y), Mathf.Min(FirstColumn, fillEnd.x));
                Rect fillLast = GetCellRect(canvas, Mathf.Max(LastRow, fillEnd.y), Mathf.Max(LastColumn, fillEnd.x));
                DrawSelectionBorder(Rect.MinMaxRect(fillFirst.x, fillFirst.y, fillLast.xMax, fillLast.yMax));
            }
            if (movingSelection)
            {
                float x = canvas.x + RowNumberWidth + columnOffsets[dropCell.x];
                float y = canvas.y + DefaultRowHeight * 2f + rowOffsets[dropCell.y];
                DrawSelectionBorder(new Rect(x, y, selection.width, selection.height));
            }
            Rect inside = new(selection.x + 4f, selection.y + 4f, selection.width - 8f, selection.height - 8f);
            bool onBorder = selection.Contains(current.mousePosition) && !inside.Contains(current.mousePosition);
            if (onBorder && !editingCell && !fillHandle.Contains(current.mousePosition))
                EditorGUIUtility.AddCursorRect(selection, MouseCursor.MoveArrow);

            int row = GoogleSheetGridGUI.GetItemAtOffset(rowOffsets, current.mousePosition.y - canvas.y - DefaultRowHeight * 2f);
            int column = Mathf.Min(columnCount - 1,
                GoogleSheetGridGUI.GetItemAtOffset(columnOffsets, current.mousePosition.x - canvas.x - RowNumberWidth));

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown when current.button == 0 && canvas.Contains(current.mousePosition) &&
                                              current.mousePosition.x < GetCellRect(canvas, 0, columnCount - 1).xMax:
                    if (!editingCell && fillHandle.Contains(current.mousePosition))
                    {
                        fillingSelection = true;
                        fillEnd = new Vector2Int(LastColumn, LastRow);
                        GUIUtility.hotControl = id;
                        current.Use();
                        break;
                    }
                    if (editingCell && row == selectedRow && column == selectedColumn &&
                        current.mousePosition.x >= canvas.x + RowNumberWidth && current.mousePosition.y >= canvas.y + DefaultRowHeight * 2f)
                        return;
                    movingSelection = onBorder && !current.shift && current.clickCount == 1;
                    draggingSelection = !movingSelection;
                    dragOrigin = new Vector2Int(column, row);
                    dropCell = new Vector2Int(FirstColumn, FirstRow);
                    if (!movingSelection)
                    {
                        SelectCell(row, column, current.shift);
                        selectingRows = current.mousePosition.x < canvas.x + RowNumberWidth;
                        selectingColumns = current.mousePosition.y < canvas.y + DefaultRowHeight * 2f;
                        if (selectingRows)
                        {
                            anchorColumn = 0;
                            selectedColumn = columnCount - 1;
                        }
                        if (selectingColumns)
                        {
                            anchorRow = 0;
                            selectedRow = rowCount - 1;
                        }
                        if (current.clickCount == 2)
                            editingCell = focusCell = true;
                    }
                    GUIUtility.hotControl = id;
                    current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    if (fillingSelection)
                    {
                        int rowDistance = row < FirstRow ? FirstRow - row : Mathf.Max(0, row - LastRow);
                        int columnDistance = column < FirstColumn ? FirstColumn - column : Mathf.Max(0, column - LastColumn);
                        fillVertical = rowDistance >= columnDistance;
                        fillEnd = fillVertical
                            ? new Vector2Int(LastColumn, row)
                            : new Vector2Int(column, LastRow);
                    }
                    else if (movingSelection)
                        dropCell = new Vector2Int(Mathf.Max(0, Mathf.Min(columnCount - (LastColumn - FirstColumn + 1), FirstColumn + column - dragOrigin.x)), Mathf.Max(0, FirstRow + row - dragOrigin.y));
                    else if (draggingSelection)
                    {
                        if (!selectingColumns) selectedRow = row;
                        if (!selectingRows) selectedColumn = column;
                    }
                    Repaint();
                    current.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    if (fillingSelection)
                        AutoFillSelection(current.control || current.command);
                    if (movingSelection && dropCell != new Vector2Int(FirstColumn, FirstRow))
                        MoveSelection(dropCell.y, dropCell.x, current.control || current.command);
                    draggingSelection = movingSelection = fillingSelection = false;
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
                case EventType.Ignore when GUIUtility.hotControl == id:
                    draggingSelection = movingSelection = fillingSelection = false;
                    GUIUtility.hotControl = 0;
                    break;
            }
        }

        private static void DrawSelectionBorder(Rect rect)
        {
            Color color = new(0.15f, 0.65f, 0.4f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), color);
        }

        private void AutoFillSelection(bool copy)
        {
            int firstRow = Mathf.Min(FirstRow, fillEnd.y);
            int lastRow = Mathf.Max(LastRow, fillEnd.y);
            int firstColumn = Mathf.Min(FirstColumn, fillEnd.x);
            int lastColumn = Mathf.Max(LastColumn, fillEnd.x);
            if (firstRow == FirstRow && lastRow == LastRow && firstColumn == FirstColumn && lastColumn == LastColumn)
                return;

            string[,] source = ReadSelection();
            var values = new string[lastRow - firstRow + 1, lastColumn - firstColumn + 1];
            GoogleSheetGridGUI.FillAutoFillValues(source, values,
                firstRow - FirstRow, firstColumn - FirstColumn, fillVertical, copy);
            Undo.RecordObject(config, "Autofill selection");
            WriteBlock(firstRow, firstColumn, values);
        }

        private string[,] ReadSelection()
        {
            var values = new string[LastRow - FirstRow + 1, LastColumn - FirstColumn + 1];
            for (int row = 0; row < values.GetLength(0); row++)
                for (int column = 0; column < values.GetLength(1); column++)
                    values[row, column] = GetCellText(FirstRow + row, FirstColumn + column);
            return values;
        }

        private void CopySelection(bool cut)
        {
            var text = new StringBuilder();
            for (int row = FirstRow; row <= LastRow; row++)
            {
                if (row > FirstRow) text.Append('\n');
                for (int column = FirstColumn; column <= LastColumn; column++)
                {
                    if (column > FirstColumn) text.Append('\t');
                    string value = GetCellText(row, column);
                    if (value.IndexOfAny(QuotedCharacters) >= 0)
                        text.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
                    else
                        text.Append(value);
                }
            }
            // A final record separator preserves a trailing empty row in TSV.
            EditorGUIUtility.systemCopyBuffer = text.Append('\n').ToString();
            if (cut) ClearSelection();
        }

        private void ClearSelection()
        {
            Undo.RecordObject(config, "Clear sheet selection");
            for (int row = FirstRow; row <= LastRow && row < config.Items.Count; row++)
                for (int column = FirstColumn; column <= LastColumn; column++)
                    WriteCell(row, column, string.Empty);
            TrimEmptyTrailingRows();
            EditorUtility.SetDirty(config);
            Repaint();
        }

        private void PasteSelection()
        {
            List<List<string>> rows;
            try
            {
                rows = GoogleSheetBulkImporterWindow.ParseCsv(EditorGUIUtility.systemCopyBuffer, '\t');
            }
            catch (FormatException exception)
            {
                ShowNotification(new GUIContent(exception.Message));
                return;
            }
            if (rows.Count == 0) rows.Add(new List<string> { string.Empty });
            int width = 0;
            foreach (var row in rows) width = Mathf.Max(width, row.Count);
            if (FirstColumn + width > EditableColumnCount)
            {
                ShowNotification(new GUIContent("Paste must fit within config columns A–D."));
                return;
            }
            var values = new string[rows.Count, width];
            for (int row = 0; row < rows.Count; row++)
                for (int column = 0; column < width; column++)
                    values[row, column] = column < rows[row].Count ? rows[row][column] : string.Empty;
            int height = values.GetLength(0);
            int targetHeight = LastRow - FirstRow + 1;
            int targetWidth = LastColumn - FirstColumn + 1;
            // Repeat a copied block only when it tiles the selected range exactly.
            if (targetHeight % height == 0 && targetWidth % width == 0)
            {
                var tiled = new string[targetHeight, targetWidth];
                for (int row = 0; row < targetHeight; row++)
                    for (int column = 0; column < targetWidth; column++)
                        tiled[row, column] = values[row % height, column % width];
                values = tiled;
            }
            Undo.RecordObject(config, "Paste sheet selection");
            WriteBlock(FirstRow, FirstColumn, values);
        }

        private void MoveSelection(int row, int column, bool copy)
        {
            string[,] values = ReadSelection();
            Undo.RecordObject(config, copy ? "Copy sheet selection" : "Move sheet selection");
            if (!copy)
                for (int r = FirstRow; r <= LastRow && r < config.Items.Count; r++)
                    for (int c = FirstColumn; c <= LastColumn; c++)
                        WriteCell(r, c, string.Empty);
            WriteBlock(row, column, values);
        }

        private void WriteBlock(int row, int column, string[,] values)
        {
            for (int r = 0; r < values.GetLength(0); r++)
                for (int c = 0; c < values.GetLength(1); c++)
                    WriteCell(row + r, column + c, values[r, c]);
            anchorRow = row;
            anchorColumn = column;
            selectedRow = row + values.GetLength(0) - 1;
            selectedColumn = column + values.GetLength(1) - 1;
            editingCell = false;
            TrimEmptyTrailingRows();
            EditorUtility.SetDirty(config);
            Repaint();
        }

        // Vẽ một ô header bằng style chung của grid.
        private static void DrawHeaderCell(Rect rect, string text)
        {
            GoogleSheetGridGUI.DrawHeaderCell(rect, text);
        }

        // Tính vị trí và kích thước của một ô trong grid.
        private Rect GetCellRect(Rect canvas, int row, int column)
        {
            float y = row < 0
                ? canvas.y + (row + 2) * DefaultRowHeight
                : canvas.y + DefaultRowHeight * 2f +
                  rowOffsets[row];
            float height = row < 0
                ? DefaultRowHeight
                : rowHeights[row];

            return new Rect(
                canvas.x + RowNumberWidth +
                columnOffsets[column],
                y,
                columnWidths[column],
                height);
        }

        // Đọc giá trị hiển thị của một ô config.
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

        // Ghi giá trị ô vào item config và đánh dấu asset đã thay đổi.
        private void SetCellText(int row, int column, string value)
        {
            if (row < 0 || column < 0 || column >= EditableColumnCount)
                return;
            Undo.RecordObject(config, "Edit bulk import cell");
            WriteCell(row, column, value);
            TrimEmptyTrailingRows();
            EditorUtility.SetDirty(config);
        }

        private void WriteCell(int row, int column, string value)
        {
            if (row < 0 || column < 0 || column >= EditableColumnCount)
                return;


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



        }

        // Xóa các dòng trống ở cuối config trước khi lưu.
        private void TrimEmptyTrailingRows()
        {
            while (config.Items.Count > 1 &&
                   IsEmpty(config.Items[config.Items.Count - 1]))
            {
                config.Items.RemoveAt(config.Items.Count - 1);
            }
        }

        // Kiểm tra một dòng import có hoàn toàn trống hay không.
        private static bool IsEmpty(GoogleSheetBulkImportItem item)
        {
            return item == null ||
                   (string.IsNullOrWhiteSpace(item.SourceUrl) &&
                    string.IsNullOrWhiteSpace(item.SheetName) &&
                    item.Type == SheetImportType.Auto &&
                    (string.IsNullOrWhiteSpace(item.SaveTo) ||
                     item.SaveTo == "Assets/_Project/Resources/Config/"));
        }

        // Chuyển nội dung ô Type thành kiểu import tương ứng.
        private static SheetImportType ParseImportType(string value)
        {
            if (string.Equals(value, "google", StringComparison.OrdinalIgnoreCase))
                return SheetImportType.Google;

            return string.Equals(value, "csv", StringComparison.OrdinalIgnoreCase)
                ? SheetImportType.Csv
                : SheetImportType.Auto;
        }

        // Tạo tên ô dạng spreadsheet từ row và column.
        private static string GetCellName(int row, int column)
        {
            int safeColumn = Mathf.Clamp(column, 0, ColumnCount - 1);
            return $"{(char)('A' + safeColumn)}{Mathf.Max(0, row) + 1}";
        }

        // Dọn dòng trống và lưu config khi đóng cửa sổ.
        private void OnDestroy()
        {
            if (config != null)
                AssetDatabase.SaveAssets();
        }
    }

    internal static class GoogleSheetGridGUI
    {
        internal static void BuildOffsets(List<float> sizes, List<float> offsets, int count)
        {
            while (offsets.Count <= count)
                offsets.Add(0f);
            if (offsets.Count > count + 1)
                offsets.RemoveRange(count + 1, offsets.Count - count - 1);
            offsets[0] = 0f;
            for (int i = 0; i < count; i++)
                offsets[i + 1] = offsets[i] + sizes[i];
        }

        internal static int GetItemAtOffset(List<float> offsets, float position)
        {
            int low = 0;
            int high = offsets.Count - 2;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (position < offsets[middle + 1])
                    high = middle;
                else
                    low = middle + 1;
            }
            return low;
        }

        internal static void FillAutoFillValues(string[,] source, string[,] values, int rowOffset, int columnOffset, bool vertical, bool copy)
        {
            int sourceHeight = source.GetLength(0);
            int sourceWidth = source.GetLength(1);
            int laneCount = vertical ? sourceWidth : sourceHeight;
            int sourceCount = vertical ? sourceHeight : sourceWidth;
            int targetCount = vertical ? values.GetLength(0) : values.GetLength(1);
            for (int lane = 0; lane < laneCount; lane++)
            {
                decimal start = 0;
                decimal step = 0;
                string prefix = string.Empty;
                int padding = 0;
                bool series = !copy && sourceCount >= 2 && TryGetFillNumber(
                    source[vertical ? 0 : lane, vertical ? lane : 0], out start, out prefix, out padding);
                if (series)
                {
                    try
                    {
                        for (int i = 1; i < sourceCount; i++)
                        {
                            if (!TryGetFillNumber(source[vertical ? i : lane, vertical ? lane : i],
                                    out decimal number, out string nextPrefix, out _) || nextPrefix != prefix)
                            {
                                series = false;
                                break;
                            }
                            if (i == 1)
                                step = number - start;
                            else if (number != start + step * i)
                            {
                                series = false;
                                break;
                            }
                        }
                    }
                    catch (OverflowException)
                    {
                        series = false;
                    }
                }
                string format = padding > 0 ? new string('0', padding) : null;
                for (int i = 0; i < targetCount; i++)
                {
                    int position = i + (vertical ? rowOffset : columnOffset);
                    int sourcePosition = (position % sourceCount + sourceCount) % sourceCount;
                    string value = source[vertical ? sourcePosition : lane, vertical ? lane : sourcePosition];
                    if (series && (position < 0 || position >= sourceCount))
                    {
                        try
                        {
                            decimal result = start + step * position;
                            value = prefix + (padding > 0 && result == decimal.Truncate(result)
                                ? result.ToString(format, CultureInfo.InvariantCulture)
                                : result.ToString(CultureInfo.InvariantCulture));
                        }
                        catch (OverflowException)
                        {
                            // Keep the repeated source value when the numeric series exceeds decimal range.
                        }
                    }
                    values[vertical ? i : lane, vertical ? lane : i] = value;
                }
            }
        }

        private static bool TryGetFillNumber(string value, out decimal number, out string prefix, out int padding)
        {
            prefix = string.Empty;
            padding = 0;
            if (decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out number))
            {
                if (value.Length > 1 && value[0] == '0' && value.IndexOf('.') < 0)
                    padding = value.Length;
                return true;
            }

            int index = value.Length;
            while (index > 0 && value[index - 1] >= '0' && value[index - 1] <= '9')
                index--;
            if (index == value.Length)
                return false;
            string digits = value.Substring(index);
            prefix = value.Substring(0, index);
            if (digits.Length > 1 && digits[0] == '0')
                padding = digits.Length;
            return decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out number);
        }

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

        // Cung cấp style dùng chung cho các ô dữ liệu.
        public static GUIStyle CellStyle => cellStyle ??= CreateCellStyle();

        // Cung cấp style dùng chung cho ô nhập liệu trên formula bar.
        public static GUIStyle FormulaStyle => formulaStyle ??= CreateFormulaStyle();

        // Bổ sung độ rộng mặc định để danh sách đủ số cột cần vẽ.
        public static void EnsureColumnWidths(
            List<float> widths,
            int count,
            float defaultWidth)
        {
            while (widths.Count < count)
                widths.Add(defaultWidth);
        }

        // Bổ sung chiều cao mặc định để danh sách đủ số dòng cần vẽ.
        public static void EnsureRowHeights(
            List<float> heights,
            int count,
            float defaultHeight)
        {
            while (heights.Count < count)
                heights.Add(defaultHeight);
        }

        // Tính tổng độ rộng của số cột được yêu cầu.
        public static float GetTotalWidth(List<float> widths, int count)
        {
            float width = 0f;
            for (int i = 0; i < count; i++)
                width += widths[i];

            return width;
        }

        // Tính khoảng cách từ đầu grid tới một cột.
        public static float GetColumnOffset(List<float> widths, int column)
        {
            float offset = 0f;
            for (int i = 0; i < column; i++)
                offset += widths[i];

            return offset;
        }

        // Tính tổng chiều cao của số dòng được yêu cầu.
        public static float GetTotalHeight(List<float> heights, int count)
        {
            float height = 0f;
            for (int i = 0; i < count; i++)
                height += heights[i];

            return height;
        }

        // Tính khoảng cách từ đầu grid tới một dòng.
        public static float GetRowOffset(List<float> heights, int row)
        {
            float offset = 0f;
            for (int i = 0; i < row; i++)
                offset += heights[i];

            return offset;
        }

        // Xử lý kéo chuột để thay đổi độ rộng một cột.
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

        // Áp dụng delta resize và giới hạn độ rộng cột tối thiểu.
        internal static float ResizeColumn(float width, float delta)
        {
            return Mathf.Max(
                MinimumColumnWidth,
                width + delta * ResizeSensitivity);
        }

        // Xử lý kéo chuột để thay đổi chiều cao một dòng.
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

        // Áp dụng delta resize và giới hạn chiều cao dòng tối thiểu.
        internal static float ResizeRow(float height, float delta)
        {
            return Mathf.Max(
                MinimumRowHeight,
                height + delta * ResizeSensitivity);
        }

        // Vẽ thanh tab tiêu đề phía trên grid.
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

        // Vẽ nút toolbar và trả về true khi người dùng nhấn.
        public static bool ToolbarButton(string text, string tooltip)
        {
            return GUILayout.Button(
                new GUIContent(text, tooltip),
                EditorStyles.toolbarButton,
                GUILayout.Width(27f),
                GUILayout.Height(25f));
        }

        // Vẽ nền, trạng thái chọn, tìm kiếm và đường kẻ cho một ô.
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

        // Vẽ header của hàng hoặc cột với trạng thái được chọn.
        public static void DrawHeaderCell(
            Rect rect,
            string text,
            bool selected = false)
        {
            EditorGUI.DrawRect(rect, selected ? HeaderSelectedColor : HeaderColor);
            DrawGridLines(rect);
            GUI.Label(rect, text, headerStyle ??= CreateHeaderStyle());
        }

        // Vẽ footer mô phỏng tab sheet ở cuối cửa sổ.
        public static void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: 1.0.0  \u263A", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // Vẽ đường viền dưới và phải của một ô grid.
        private static void DrawGridLines(Rect rect)
        {
            EditorGUI.DrawRect(
                new Rect(rect.xMax - 1f, rect.y, 1f, rect.height),
                GridColor);
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.yMax - 1f, rect.width, 1f),
                GridColor);
        }

        // Tạo style nhập liệu cho các ô dữ liệu.
        private static GUIStyle CreateCellStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                padding = new RectOffset(6, 4, 0, 0),
            };
        }

        // Tạo style nhập liệu cho formula bar.
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

        // Tạo style hiển thị tên tab sheet.
        private static GUIStyle CreateTabStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 4, 0, 0),
            };
        }

        // Tạo style chữ cho header hàng và cột.
        private static GUIStyle CreateHeaderStyle()
        {
            return new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                clipping = TextClipping.Clip,
            };
        }
    }
}
