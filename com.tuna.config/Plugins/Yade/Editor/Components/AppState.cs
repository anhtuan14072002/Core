//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using UnityEngine;
using System;
using Yade.Runtime;
using UnityEditor;
using System.Linq;
using System.IO;

namespace Yade.Editor
{
    [Serializable]
    /// <summary>
    /// State class of spreadsheet editor
    /// </summary>
    public partial class AppState : ScriptableObject
    {
        /// <summary>
        /// Sheet data
        /// </summary>
        public YadeSheetData data;

        /// <summary>
        /// Row offset of current vertical scrollbar position
        /// </summary>
        public int scrollRowIndex;

        /// <summary>
        /// Column offset of current horizontal scrollbar position
        /// </summary>
        public int scrollColumnIndex;

        /// <summary>
        /// Current position of vertical scrollbar
        /// </summary>
        public float scrollTop;

        /// <summary>
        /// Current position of horizontal scrollbar
        /// </summary>
        public float scrollLeft;

        /// <summary>
        /// The cell range for current selecting
        /// </summary>
        public CellRange selectedRange;

        /// <summary>
        /// Extra cell ranges for current selecting, using for record cells that 
        /// selected by cmd/ctrl + LMB
        /// </summary>
        public List<CellRange> extraSelectedRanges;

        /// <summary>
        /// Cell range will be auto filled 
        /// </summary>
        public CellRange autoFillRange;

        /// <summary>
        /// Main cell for current seletion
        /// </summary>
        public CellRange range;

        /// <summary>
        /// The cell under mouse position when draing
        /// </summary>
        public CellRange dragAndDropRange;

        /// <summary>
        /// Current row count of spreadsheet editor 
        /// </summary>
        public int rowCount;

        /// <summary>
        /// Current column count of spreadsheet editor
        /// </summary>
        public int columnCount;

        /// <summary>
        /// Height of bottom edge of column headers to window top
        /// </summary>
        public float fixedHeaderHeight;

        /// <summary>
        /// Width of index column
        /// </summary>
        public float fixedIndexWidth;

        /// <summary>
        /// Copy range in copy/cut features
        /// </summary>
        public CellRange copyFromRange;

        /// <summary>
        /// Whether current aciton is cut
        /// </summary>
        public bool isCut;

        /// <summary>
        /// Whether column editor visible
        /// </summary>
        public bool showColumnEditor;

        /// <summary>
        /// Whether code generator visible
        /// </summary>
        public bool ShowCodeGeneratorEditor;

        /// <summary>
        /// Search text
        /// </summary>
        public string searchText;

        /// <summary>
        /// Is in search state
        /// </summary>
        public bool IsInSearchState;

        private YadeSheet bindingSheet;

        /// <summary>
        /// The yadesheet instance related with this state
        /// </summary>
        public YadeSheet BindingSheet
        {
            get
            {
                return bindingSheet;
            }
        }

        public void SetBindingSheet(YadeSheet sheet)
        {
            bindingSheet = sheet;
        }

        private void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// Init state class
        /// </summary>
        /// <param name="data">Yade sheet data</param>
        public void Init(YadeSheetData data)
        {
            SetData(data);
            this.fixedHeaderHeight = this.GetMinimalFixedHeaderHeight(); ;
            this.fixedIndexWidth = Constants.FIXED_INDEX_WIDTH;

            int dataRowCount = data.GetRowCount();
            int dataColumnCount = data.GetColumnCount();
            this.SetRowCount(dataRowCount);
            this.SetColumnCount(dataColumnCount);
            this.scrollRowIndex = 0;
            this.scrollColumnIndex = 0;
            this.selectedRange = new CellRange(0, 0, 0, 0);
            this.range = new CellRange(0, 0, 0, 0);
            this.extraSelectedRanges = new List<CellRange>();
            this.copyFromRange = CellRange.None;
            this.isCut = false;
            this.ShowCodeGeneratorEditor = false;
            this.showColumnEditor = false;
            this.searchText = string.Empty;
            this.IsInSearchState = false;
        }

        public void SetRowCount(int rowCount)
        {
            this.rowCount = rowCount <= 100 ? 100 : rowCount;
        }

        public void SetColumnCount(int colCount)
        {
            this.columnCount = colCount <= 26 ? 26 : colCount;
        }

        /// <summary>
        /// Just update data 
        /// </summary>
        /// <param name="data">Yade sheet data</param>
        public void SetData(YadeSheetData data)
        {
            this.data = data;
        }

        /// <summary>
        /// Get height of a row
        /// </summary>
        /// <param name="rowIndex">Index of row</param>
        /// <returns>Height of row</returns>
        public float GetRowHeight(int rowIndex)
        {
            return this.data.data.GetRowHeight(rowIndex);
        }


        /// <summary>
        /// Get width of column by column index (based 0) 
        /// </summary>
        /// <param name="columnIndex">Index of column</param>
        /// <returns>width of column</returns>
        public float GetColumnWidth(int columnIndex)
        {
            return this.data.columnHeaders.GetColumnWidth(columnIndex);
        }

        /// <summary>
        /// Get the rect of range of cells in editor
        /// </summary>
        /// <param name="range">Range of cells</param>
        /// <returns>Rect of range of cells</returns>
        public CellRect GetCellRectByRange(CellRange range)
        {
            var startRow = Mathf.Max(range.startRow, 0);
            var startColumn = Mathf.Max(range.startColumn, 0);

            float top = 0;
            if (this.scrollRowIndex < startRow)
            {
                for (int i = this.scrollRowIndex; i < startRow; i++)
                {
                    top += this.GetRowHeight(i);
                }
            }
            else
            {
                for (int i = startRow; i < this.scrollRowIndex; i++)
                {
                    top -= this.GetRowHeight(i);
                }
            }

            float left = 0;
            if (this.scrollColumnIndex < startColumn)
            {
                for (int i = this.scrollColumnIndex; i < startColumn; i++)
                {
                    left += this.GetColumnWidth(i);
                }
            }
            else
            {
                for (int i = startColumn; i < this.scrollColumnIndex; i++)
                {
                    left -= this.GetColumnWidth(i);
                }
            }

            float width = this.GetColumnTotalWidth(startColumn, range.endColumn);
            float height = this.GetRowTotalHeight(startRow, range.endRow);

            return new CellRect(left, top, width, height);
        }

        /// <summary>
        /// Get total height of rows
        /// </summary>
        /// <param name="startRow">Index of start row</param>
        /// <param name="endRow">Index of end row</param>
        /// <returns>Height of rows. Return zero if endRow less than startRow</returns>
        public float GetRowTotalHeight(int startRow, int endRow)
        {
            if (startRow < 0 || endRow < 0)
            {
                return 0;
            }

            float height = 0;
            for (int i = startRow; i <= endRow; i++)
            {
                height += GetRowHeight(i);
            }

            return height;
        }

        public void SetRowHeight(int row, float height)
        {
            this.data.data.SetRowHieght(row, height);
        }

        public void SetColumnWidth(int column, float width)
        {
            this.data.columnHeaders.SetColumnWidth(column, width);
        }

        /// <summary>
        /// Get total width of columns
        /// </summary>
        /// <param name="startColumn">Index of start column</param>
        /// <param name="endColumn">Index of end column</param>
        /// <returns>Total width of columns. Return zero if endColumn less than startColumn</returns>
        public float GetColumnTotalWidth(int startColumn, int endColumn)
        {
            if (startColumn < 0 || endColumn < 0)
            {
                return 0;
            }

            float width = 0;
            for (int i = startColumn; i <= endColumn; i++)
            {
                width += GetColumnWidth(i);
            }
            return width;
        }

        public int GetRowIndexByY(float posY)
        {
            if (posY < 0)
            {
                return -2;
            }

            float y = Row.DEFAULT_HEIGHT;
            if (y > posY)
            {
                return -1;
            }

            for (int i = 0; i <= this.rowCount - this.scrollRowIndex; i++)
            {
                y += this.GetRowHeight(i + this.scrollRowIndex);
                if (y > posY)
                {
                    return i;
                }
            }

            // If outside of bottom bound of current view, set index to -2
            return -2;
        }

        public int GetColumnIndexByX(float posX)
        {
            if (posX < 0)
            {
                return -1;
            }

            float x = 0;
            for (int i = 0; i < this.columnCount - this.scrollColumnIndex; i++)
            {
                x += this.GetColumnWidth(i + this.scrollColumnIndex);
                if (x > posX)
                {
                    return i;
                }
            }

            return -2;
        }

        public Cell GetCell(int rowIndex, int columnIndex)
        {
            return this.data.GetCell(rowIndex, columnIndex);
        }

        public void ClearData()
        {
            this.data.data.items.Clear();
        }

        public void DeleteCell(int rowIndex, int columnIndex)
        {
            this.data.DeleteCell(rowIndex, columnIndex);
            EditorUtility.SetDirty(this.data);
        }

        public void AddRow(int selectedRow, int delta = 1)
        {
            this.data.AddRow(selectedRow, delta);
            EditorUtility.SetDirty(this.data);
        }

        public void AddColumn(int selectedColumn, int delta = 1)
        {
            this.data.AddColumn(selectedColumn, delta);
            EditorUtility.SetDirty(this.data);
        }

        public void DeleteRows(int[] rowIndexs)
        {
            this.data.DeleteRows(rowIndexs);
            EditorUtility.SetDirty(this.data);
        }

        public void DeleteColumns(int[] columnIndexes)
        {
            this.data.DeleteColumn(columnIndexes);
            EditorUtility.SetDirty(this.data);
        }

        public void SetCellRawValue(int rowIndex, int columnIndex, string text)
        {
            this.data.SetRawValueInternal(rowIndex, columnIndex, text);
            EditorUtility.SetDirty(this.data);
        }

        public void SheetOrderByColumn(bool isAsc)
        {
            if (selectedRange == null)
            {
                return;
            }

            RecordUndo("sortby");
            var sortedColumn = selectedRange.startColumn;
            var rows = data.data.items.Values;

            var selectAction = new Func<Row, string>(row =>
            {
                var cell = row.GetCell(sortedColumn);
                if (cell == null)
                {
                    return string.Empty;
                }

                if (cell.HasUnityObject())
                {
                    var assetPath = AssetDatabase.GetAssetPath(cell.GetUnityObject());
                    var fileName = Path.GetFileNameWithoutExtension(assetPath);
                    return fileName;
                }
                else
                {
                    return cell.GetValue();
                }
            });

            if (isAsc)
            {
                rows = rows.OrderBy(selectAction, new CellValueComparer()).ToArray();
            }
            else
            {
                rows = rows.OrderByDescending(selectAction, new CellValueComparer()).ToArray();
            }

            data.Clear();

            for (int i = 0; i < rows.Length; i++)
            {
                data.data.AddRow(i, rows[i]);
            }
        }

        public void RecordUndo(string name)
        {
            Undo.RegisterCompleteObjectUndo(this, name);
            if (data != null)
            {
                Undo.RegisterCompleteObjectUndo(data, name);
            }
        }

        public void AutoFillDataFromRangeToRange(CellRange fromRange, CellRange toRange, bool removeFromRange = false, bool fillSeries = true)
        {
            var map = new Dictionary<string, string>();

            // save the data
            fromRange.ForEach((row, column) =>
            {
                var cell = this.GetCell(row, column);
                if (cell != null)
                {
                    map.Add(string.Format("{0},{1}", row, column), cell.GetRawValue());
                }
            });

            if (removeFromRange)
            {
                fromRange.ForEach((row, column) => this.DeleteCell(row, column));
            }

            // copy to new range
            int fromRowSize = fromRange.endRow - fromRange.startRow + 1;
            int fromColumnSize = fromRange.endColumn - fromRange.startColumn + 1;

            // fill range
            toRange.ForEach((row, column) =>
            {
                int rowIndex = (row - toRange.startRow) % fromRowSize + fromRange.startRow;
                int columnIndex = (column - toRange.startColumn) % fromColumnSize + fromRange.startColumn;
                string key = string.Format("{0},{1}", rowIndex, columnIndex);
                if (map.ContainsKey(key))
                {
                    this.SetCellRawValue(row, column, map[key]);
                }
                else
                {
                    this.DeleteCell(row, column);
                }
            });

            if (!fillSeries)
            {
                return;
            }

            // if only one cell selected, we don't need to fill serials
            if (fromRowSize == 1 && fromColumnSize == 1)
            {
                return;
            }

            // another pass to check number fill
            bool isHorizontalCopy = toRange.endColumn > fromRange.endColumn;
            if (isHorizontalCopy && fromColumnSize > 1)
            {
                for (int row = toRange.startRow; row <= toRange.endRow; row++)
                {
                    for (int column = toRange.startColumn; column <= toRange.endColumn; column++)
                    {
                        // if in orginal range
                        var key = string.Format("{0},{1}", row, column);
                        if (map.ContainsKey(key))
                        {
                            if (IsNumber(map[key]))
                            {
                                continue;
                            }

                            break;
                        }

                        var lastlastCellValue = GetCell(row, column - 2).GetDouble();
                        var lastCellValue = GetCell(row, column - 1).GetDouble();
                        var currentValue = lastCellValue + lastCellValue - lastlastCellValue;
                        SetCellRawValue(row, column, currentValue.ToString());
                    }
                }
            }
            else if (!isHorizontalCopy && fromRowSize > 1)
            {
                for (int column = toRange.startColumn; column <= toRange.endColumn; column++)
                {
                    for (int row = toRange.startRow; row <= toRange.endRow; row++)
                    {
                        // if in orginal range
                        var key = string.Format("{0},{1}", row, column);
                        if (map.ContainsKey(key))
                        {
                            if (IsNumber(map[key]))
                            {
                                continue;
                            }

                            break;
                        }

                        var lastlastCellValue = GetCell(row - 2, column).GetDouble();
                        var lastCellValue = GetCell(row - 1, column).GetDouble();
                        var currentValue = lastCellValue + lastCellValue - lastlastCellValue;
                        SetCellRawValue(row, column, currentValue.ToString());
                    }
                }
            }
        }

        private bool IsNumber(string v)
        {
            double d;
            return double.TryParse(v, out d);
        }

        /// <summary>
        /// Is visual header is visible
        /// </summary>
        /// <param name="header">Visual header type</param>
        /// <returns>Whether the visual header type is visible</returns>
        internal bool IsVisualHeaderVisible(VisualHeaderType header)
        {
            var headerNumber = (int)header;
            return (data.VisualHeaders & headerNumber) == headerNumber;
        }

        /// <summary>
        /// Set the visibilty of visual header
        /// </summary>
        /// <param name="header">Visual header type</param>
        /// <param name="visible">Visiblity of visual header</param>
        internal void SetVisualHeaderVisible(VisualHeaderType header, bool visible)
        {
            var headerNumber = (int)header;

            if (visible)
            {
                data.VisualHeaders = data.VisualHeaders | headerNumber;
            }
            else
            {
                data.VisualHeaders = data.VisualHeaders & ~(headerNumber);
            }

            EditorUtility.SetDirty(this.data);
        }

        public float GetMinimalFixedHeaderHeight()
        {
            return Constants.FIXED_HEADER_HEIGHT + GetExtraHeaderHeight();
        }

        public float GetExtraHeaderCount()
        {
            var count = 0;
            count += IsVisualHeaderVisible(VisualHeaderType.Alias) ? 1 : 0;
            count += IsVisualHeaderVisible(VisualHeaderType.Field) ? 1 : 0;
            count += IsVisualHeaderVisible(VisualHeaderType.DataType) ? 1 : 0;

            return count;
        }

        public float GetExtraHeaderHeight()
        {
            return Row.DEFAULT_HEIGHT * GetExtraHeaderCount();
        }
    }
}