using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Sheet.Editor
{
    public sealed class GoogleSheetDataWindow : EditorWindow
    {
        private const int MinimumColumnCount = 15;
        private const int MinimumRowCount = 25;
        private const float RowNumberWidth = 62f;
        private const float DefaultColumnWidth = 140f;
        private const float DefaultRowHeight = 25f;

        [SerializeField] private GoogleSheetData sheet;
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

        // Mở cửa sổ chỉnh sửa cho GoogleSheetData asset được chọn.
        public static void Open(GoogleSheetData target)
        {
            var window = GetWindow<GoogleSheetDataWindow>(target.name);
            window.titleContent = new GUIContent(target.name);
            window.sheet = target;
            window.SelectCell(0, 0, false);
            window.minSize = new Vector2(900f, 480f);
            window.Show();
        }

        // Mở sheet editor khi người dùng double-click GoogleSheetData asset.
        [OnOpenAsset]
        private static bool OpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not GoogleSheetData target)
                return false;

            Open(target);
            return true;
        }

        // Vẽ thanh công cụ, formula bar, grid và footer của sheet editor.
        private void OnGUI()
        {
            if (sheet == null)
            {
                EditorGUILayout.HelpBox("Select a Google sheet data asset.", MessageType.Info);
                return;
            }

            HandleKeyboard();
            GoogleSheetGridGUI.DrawTabBar(sheet.name);
            DrawToolbar();
            DrawFormulaBar();
            DrawGrid();
            GoogleSheetGridGUI.DrawFooter();
        }

        // Vẽ thanh tìm kiếm và các nút thêm, xóa dòng.
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

        // Thêm một dòng dữ liệu mới vào cuối sheet.
        private void AddRow()
        {
            Undo.RecordObject(sheet, "Add sheet row");
            sheet.Rows.Add(new GoogleSheetDataRow());
            SelectCell(sheet.Rows.Count - 1, selectedColumn, false);
            EditorUtility.SetDirty(sheet);
        }

        // Xóa dòng dữ liệu đang được chọn.
        private void RemoveSelectedRow()
        {
            if (selectedRow < 0 || selectedRow >= sheet.Rows.Count)
                return;

            Undo.RecordObject(sheet, "Remove sheet row");
            sheet.Rows.RemoveAt(selectedRow);
            if (selectedRow < rowHeights.Count)
                rowHeights.RemoveAt(selectedRow);
            SelectCell(Mathf.Clamp(selectedRow, 0, Mathf.Max(0, sheet.Rows.Count - 1)), selectedColumn, false);
            EditorUtility.SetDirty(sheet);
        }

        // Vẽ thanh chỉnh sửa nội dung của ô đang chọn.
        private void DrawFormulaBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                $"{GetColumnName(selectedColumn)}{Mathf.Max(0, selectedRow) + 1}",
                EditorStyles.helpBox,
                GUILayout.Width(RowNumberWidth));

            string value = GetCellText(selectedRow, selectedColumn);
            GUI.SetNextControlName("SheetFormula");
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.TextField(
                value,
                GoogleSheetGridGUI.FormulaStyle,
                GUILayout.Height(DefaultRowHeight));
            if (EditorGUI.EndChangeCheck())
                SetCellText(selectedRow, selectedColumn, value);

            EditorGUILayout.EndHorizontal();
            if (GUI.GetNameOfFocusedControl() == "SheetFormula")
            {
                gridFocused = false;
                editingCell = false;
            }
        }

        // Vẽ toàn bộ sheet grid, header, cell và vùng resize.
        private void DrawGrid()
        {
            int rowCount = Mathf.Max(MinimumRowCount, Mathf.Max(sheet.Rows.Count + 1, LastRow + 1));
            int columnCount = Mathf.Max(MinimumColumnCount, Mathf.Max(GetDataColumnCount(), LastColumn + 1));
            GoogleSheetGridGUI.EnsureColumnWidths(
                columnWidths,
                columnCount,
                DefaultColumnWidth);
            GoogleSheetGridGUI.EnsureRowHeights(
                rowHeights,
                rowCount,
                DefaultRowHeight);
            GoogleSheetGridGUI.BuildOffsets(columnWidths, columnOffsets, columnCount);
            GoogleSheetGridGUI.BuildOffsets(rowHeights, rowOffsets, rowCount);
            float width = RowNumberWidth + columnOffsets[columnCount];
            float height = DefaultRowHeight + rowOffsets[rowCount];

            scroll = EditorGUILayout.BeginScrollView(scroll);
            Rect canvas = GUILayoutUtility.GetRect(width, height);
            int selectionControl = GUIUtility.GetControlID("SheetSelection".GetHashCode(), FocusType.Passive);
            int firstVisibleRow = Mathf.Max(0, GoogleSheetGridGUI.GetItemAtOffset(rowOffsets, scroll.y - DefaultRowHeight) - 1);
            int lastVisibleRow = Mathf.Min(rowCount - 1, GoogleSheetGridGUI.GetItemAtOffset(rowOffsets, scroll.y + position.height - DefaultRowHeight) + 1);
            int firstVisibleColumn = Mathf.Max(0, GoogleSheetGridGUI.GetItemAtOffset(columnOffsets, scroll.x - RowNumberWidth) - 1);
            int lastVisibleColumn = Mathf.Min(columnCount - 1, GoogleSheetGridGUI.GetItemAtOffset(columnOffsets, scroll.x + position.width - RowNumberWidth) + 1);


            DrawHeaderCell(
                new Rect(
                    canvas.x,
                    canvas.y,
                    RowNumberWidth,
                    DefaultRowHeight),
                string.Empty);

            for (int column = firstVisibleColumn; column <= lastVisibleColumn; column++)
            {
                Rect headerRect = GetCellRect(canvas, -1, column);
                GoogleSheetGridGUI.DrawHeaderCell(
                    headerRect,
                    GetColumnName(column),
                    column >= FirstColumn && column <= LastColumn);
                GoogleSheetGridGUI.HandleColumnResize(
                    new Rect(
                        headerRect.x,
                        canvas.y,
                        headerRect.width,
                        DefaultRowHeight),
                    column,
                    columnWidths,
                    Repaint);
            }

            for (int row = firstVisibleRow; row <= lastVisibleRow; row++)
            {
                float rowY = canvas.y + DefaultRowHeight +
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

            HandleGridMouse(canvas, rowCount, columnCount, selectionControl);
            EditorGUILayout.EndScrollView();
        }

        // Vẽ và xử lý nhập liệu cho một ô dữ liệu.
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
            selectedColumn = Mathf.Max(0, column);
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
                            SelectCell(Mathf.Max(0, sheet.Rows.Count - 1), Mathf.Max(0, GetDataColumnCount() - 1), true);
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
                float y = canvas.y + DefaultRowHeight + rowOffsets[dropCell.y];
                DrawSelectionBorder(new Rect(x, y, selection.width, selection.height));
            }
            Rect inside = new(selection.x + 4f, selection.y + 4f, selection.width - 8f, selection.height - 8f);
            bool onBorder = selection.Contains(current.mousePosition) && !inside.Contains(current.mousePosition);
            if (onBorder && !editingCell && !fillHandle.Contains(current.mousePosition))
                EditorGUIUtility.AddCursorRect(selection, MouseCursor.MoveArrow);

            int row = GoogleSheetGridGUI.GetItemAtOffset(rowOffsets, current.mousePosition.y - canvas.y - DefaultRowHeight);
            int column = Mathf.Min(columnCount - 1,
                GoogleSheetGridGUI.GetItemAtOffset(columnOffsets, current.mousePosition.x - canvas.x - RowNumberWidth));

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown when current.button == 0 && canvas.Contains(current.mousePosition):
                    if (!editingCell && fillHandle.Contains(current.mousePosition))
                    {
                        fillingSelection = true;
                        fillEnd = new Vector2Int(LastColumn, LastRow);
                        GUIUtility.hotControl = id;
                        current.Use();
                        break;
                    }
                    if (editingCell && row == selectedRow && column == selectedColumn &&
                        current.mousePosition.x >= canvas.x + RowNumberWidth && current.mousePosition.y >= canvas.y + DefaultRowHeight)
                        return;
                    movingSelection = onBorder && !current.shift && current.clickCount == 1;
                    draggingSelection = !movingSelection;
                    dragOrigin = new Vector2Int(column, row);
                    dropCell = new Vector2Int(FirstColumn, FirstRow);
                    if (!movingSelection)
                    {
                        SelectCell(row, column, current.shift);
                        selectingRows = current.mousePosition.x < canvas.x + RowNumberWidth;
                        selectingColumns = current.mousePosition.y < canvas.y + DefaultRowHeight;
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
                        dropCell = new Vector2Int(Mathf.Max(0, FirstColumn + column - dragOrigin.x), Mathf.Max(0, FirstRow + row - dragOrigin.y));
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
            Undo.RecordObject(sheet, "Autofill selection");
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
            Undo.RecordObject(sheet, "Clear sheet selection");
            for (int row = FirstRow; row <= LastRow && row < sheet.Rows.Count; row++)
                for (int column = FirstColumn; column <= LastColumn; column++)
                    if (sheet.Rows[row] != null && column < sheet.Rows[row].Cells.Count)
                        sheet.Rows[row].Cells[column] = string.Empty;
            EditorUtility.SetDirty(sheet);
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
            Undo.RecordObject(sheet, "Paste sheet selection");
            WriteBlock(FirstRow, FirstColumn, values);
        }

        private void MoveSelection(int row, int column, bool copy)
        {
            string[,] values = ReadSelection();
            Undo.RecordObject(sheet, copy ? "Copy sheet selection" : "Move sheet selection");
            if (!copy)
                for (int r = FirstRow; r <= LastRow && r < sheet.Rows.Count; r++)
                    for (int c = FirstColumn; c <= LastColumn; c++)
                        if (sheet.Rows[r] != null && c < sheet.Rows[r].Cells.Count)
                            sheet.Rows[r].Cells[c] = string.Empty;
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
            EditorUtility.SetDirty(sheet);
            Repaint();
        }

        // Vẽ một ô header bằng style chung của grid.
        private static void DrawHeaderCell(Rect rect, string text)
        {
            GoogleSheetGridGUI.DrawHeaderCell(rect, text);
        }

        // Tính vị trí và kích thước của một ô trong sheet grid.
        private Rect GetCellRect(Rect canvas, int row, int column)
        {
            float y = row < 0
                ? canvas.y
                : canvas.y + DefaultRowHeight +
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

        // Tìm số cột dữ liệu lớn nhất hiện có trong sheet.
        private int GetDataColumnCount()
        {
            int count = 0;
            for (int i = 0; i < sheet.Rows.Count; i++)
            {
                if (sheet.Rows[i] != null)
                    count = Mathf.Max(count, sheet.Rows[i].Cells.Count);
            }

            return count;
        }

        // Đọc nội dung ô, trả về chuỗi rỗng nếu ô chưa tồn tại.
        private string GetCellText(int row, int column)
        {
            if (row < 0 ||
                row >= sheet.Rows.Count ||
                sheet.Rows[row] == null ||
                column < 0 ||
                column >= sheet.Rows[row].Cells.Count)
            {
                return string.Empty;
            }

            return sheet.Rows[row].Cells[column] ?? string.Empty;
        }

        // Tạo ô khi cần, ghi dữ liệu và đánh dấu sheet đã thay đổi.
        private void SetCellText(int row, int column, string value)
        {
            if (row < 0 || column < 0)
                return;

            Undo.RecordObject(sheet, "Edit sheet cell");
            WriteCell(row, column, value);
            EditorUtility.SetDirty(sheet);
        }

        private void WriteCell(int row, int column, string value)
        {
            while (sheet.Rows.Count <= row)
                sheet.Rows.Add(new GoogleSheetDataRow());

            GoogleSheetDataRow targetRow = sheet.Rows[row] ?? new GoogleSheetDataRow();
            sheet.Rows[row] = targetRow;

            while (targetRow.Cells.Count <= column)
                targetRow.Cells.Add(string.Empty);

            targetRow.Cells[column] = value;
        }

        // Chuyển chỉ số cột thành tên spreadsheet như A, B hoặc AA.
        private static string GetColumnName(int index)
        {
            string name = string.Empty;
            index++;

            while (index > 0)
            {
                index--;
                name = (char)('A' + index % 26) + name;
                index /= 26;
            }

            return name;
        }

        // Lưu thay đổi của sheet khi đóng cửa sổ.
        private void OnDestroy()
        {
            if (sheet != null)
                AssetDatabase.SaveAssets();
        }
    }

    [CustomEditor(typeof(GoogleSheetData))]
    public sealed class GoogleSheetDataInspector : UnityEditor.Editor
    {
        // Hiển thị thông tin sheet và nút mở sheet editor trong Inspector.
        public override void OnInspectorGUI()
        {
            var sheet = (GoogleSheetData)target;
            EditorGUILayout.LabelField("Rows", sheet.Rows.Count.ToString());

            if (GUILayout.Button("Open Sheet"))
                GoogleSheetDataWindow.Open(sheet);
        }
    }
}
