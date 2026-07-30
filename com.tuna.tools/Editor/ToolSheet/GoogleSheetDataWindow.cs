using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Wizard.Editor;

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

        public static void Open(GoogleSheetData target)
        {
            var window = GetWindow<GoogleSheetDataWindow>(target.name);
            window.titleContent = new GUIContent(target.name);
            window.sheet = target;
            window.minSize = new Vector2(900f, 480f);
            window.Show();
        }

        [OnOpenAsset]
        private static bool OpenAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not GoogleSheetData target)
                return false;

            Open(target);
            return true;
        }

        private void OnGUI()
        {
            if (sheet == null)
            {
                EditorGUILayout.HelpBox("Select a Google sheet data asset.", MessageType.Info);
                return;
            }

            GoogleSheetGridGUI.DrawTabBar(sheet.name);
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

            GUILayout.FlexibleSpace();
            search = GUILayout.TextField(
                search,
                EditorStyles.toolbarSearchField,
                GUILayout.Width(200f));
            EditorGUILayout.EndHorizontal();
        }

        private void AddRow()
        {
            Undo.RecordObject(sheet, "Add sheet row");
            sheet.Rows.Add(new GoogleSheetDataRow());
            selectedRow = sheet.Rows.Count - 1;
            EditorUtility.SetDirty(sheet);
        }

        private void RemoveSelectedRow()
        {
            if (selectedRow < 0 || selectedRow >= sheet.Rows.Count)
                return;

            Undo.RecordObject(sheet, "Remove sheet row");
            sheet.Rows.RemoveAt(selectedRow);
            if (selectedRow < rowHeights.Count)
                rowHeights.RemoveAt(selectedRow);
            selectedRow = Mathf.Clamp(selectedRow, 0, sheet.Rows.Count - 1);
            EditorUtility.SetDirty(sheet);
        }

        private void DrawFormulaBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(
                $"{GetColumnName(selectedColumn)}{Mathf.Max(0, selectedRow) + 1}",
                EditorStyles.helpBox,
                GUILayout.Width(RowNumberWidth));

            string value = GetCellText(selectedRow, selectedColumn);
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.TextField(
                value,
                GoogleSheetGridGUI.FormulaStyle,
                GUILayout.Height(DefaultRowHeight));
            if (EditorGUI.EndChangeCheck())
                SetCellText(selectedRow, selectedColumn, value);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawGrid()
        {
            int rowCount = Mathf.Max(MinimumRowCount, sheet.Rows.Count + 1);
            int columnCount = Mathf.Max(MinimumColumnCount, GetDataColumnCount());
            GoogleSheetGridGUI.EnsureColumnWidths(
                columnWidths,
                columnCount,
                DefaultColumnWidth);
            GoogleSheetGridGUI.EnsureRowHeights(
                rowHeights,
                rowCount,
                DefaultRowHeight);
            float width = RowNumberWidth +
                          GoogleSheetGridGUI.GetTotalWidth(
                              columnWidths,
                              columnCount);
            float height = DefaultRowHeight +
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
                string.Empty);

            for (int column = 0; column < columnCount; column++)
            {
                Rect headerRect = GetCellRect(canvas, -1, column);
                GoogleSheetGridGUI.DrawHeaderCell(
                    headerRect,
                    GetColumnName(column),
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
                float rowY = canvas.y + DefaultRowHeight +
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

                for (int column = 0; column < columnCount; column++)
                    DrawDataCell(GetCellRect(canvas, row, column), row, column);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDataCell(Rect rect, int row, int column)
        {
            string value = GetCellText(row, column);
            bool selected = row == selectedRow && column == selectedColumn;
            bool matchesSearch =
                !string.IsNullOrWhiteSpace(search) &&
                value.Contains(search, StringComparison.OrdinalIgnoreCase);

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

            EditorGUI.BeginChangeCheck();
            value = EditorGUI.TextField(
                fieldRect,
                value,
                GoogleSheetGridGUI.CellStyle);
            if (EditorGUI.EndChangeCheck())
                SetCellText(row, column, value);
        }

        private static void DrawHeaderCell(Rect rect, string text)
        {
            GoogleSheetGridGUI.DrawHeaderCell(rect, text);
        }

        private Rect GetCellRect(Rect canvas, int row, int column)
        {
            float y = row < 0
                ? canvas.y
                : canvas.y + DefaultRowHeight +
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

        private void SetCellText(int row, int column, string value)
        {
            if (row < 0 || column < 0)
                return;

            Undo.RecordObject(sheet, "Edit sheet cell");

            while (sheet.Rows.Count <= row)
                sheet.Rows.Add(new GoogleSheetDataRow());

            GoogleSheetDataRow targetRow = sheet.Rows[row] ?? new GoogleSheetDataRow();
            sheet.Rows[row] = targetRow;

            while (targetRow.Cells.Count <= column)
                targetRow.Cells.Add(string.Empty);

            targetRow.Cells[column] = value;
            EditorUtility.SetDirty(sheet);
        }

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

        private void OnDestroy()
        {
            if (sheet != null)
                AssetDatabase.SaveAssets();
        }
    }

    [CustomEditor(typeof(GoogleSheetData))]
    public sealed class GoogleSheetDataInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var sheet = (GoogleSheetData)target;
            EditorGUILayout.LabelField("Rows", sheet.Rows.Count.ToString());

            if (GUILayout.Button("Open Sheet"))
                GoogleSheetDataWindow.Open(sheet);
        }
    }
}
