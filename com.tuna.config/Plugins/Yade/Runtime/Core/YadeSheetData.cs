//  Copyright (c) 2020-present amlovey
//  
using UnityEngine;
using System;
using Yade.Runtime.Formula;
using Yade.Runtime.BinarySerialization;
using System.Collections.Generic;
using System.Linq;

namespace Yade.Runtime
{
    /// <summary>
    /// Data class of Yade spreadsheet
    /// </summary>
    [Serializable]
    [CreateAssetMenu(order = 121, menuName = "Yade Sheet", fileName = "yadesheet.asset")]
    [HelpURL("https://www.amlovey.com/YadeDocs/#/YadeSheetData?id=yadesheetdata")]
    public class YadeSheetData : ScriptableObject
    {
        private FormulaEngine formulaEngine;

        /// <summary>
        /// Formula engine inside this sheet
        /// </summary>
        public FormulaEngine FormulaEngine
        {
            get
            {
                if (formulaEngine == null)
                {
                    formulaEngine = new FormulaEngine(this);
                }

                return formulaEngine;
            }
        }

        [Obsolete("Property UpdatedSinceLastCached is deprecated, please use HasToRebuildCache instead")]
        internal bool UpdatedSinceLastCached
        {
            get
            {
                return HasToRebuildCache;
            }
            set
            {
                HasToRebuildCache = value;
            }
        }

        /// <summary>
        /// Whether need to update cache?
        /// </summary>
        internal bool HasToRebuildCache { get; set; }

        /// <summary>
        /// Turn binary serialization on or off
        /// </summary>
        public bool BinarySerializerEnabled { get; set; }

        private BinarySerializer binarySerializer;
        internal BinarySerializer BinarySerializer
        {
            get
            {
                if (binarySerializer == null)
                {
                    binarySerializer = new YadeSheetBinarySerializer(this);
                }

                return binarySerializer;
            }
        }

        private Dictionary<string, List<CellIndex>> RefTree;

        internal void BuildFullRefTreeIfNeeds()
        {
            if (formulaEngine == null)
            {
                formulaEngine = new FormulaEngine(this);
            }

            if (RefTree == null)
            {
                RefTree = new Dictionary<string, List<CellIndex>>();
                var rowCount = GetRowCount();
                var columnCount = GetColumnCount();

                for (int row = 0; row < rowCount; row++)
                {
                    for (int col = 0; col < columnCount; col++)
                    {
                        var cell = this.GetCell(row, col);
                        if (cell == null)
                        {
                            continue;
                        }

                        if (!cell.IsFormulaCell())
                        {
                            continue;
                        }

                        var rawText = cell.GetRawValue().Trim();
                        if (rawText.Length <= 1)
                        {
                            continue;
                        }

                        UpdateToRefTreeIfNeeds(row, col, rawText);
                    }
                }
            }
        }

        internal void UpdateToRefTreeIfNeeds(int row, int col, string rawText)
        {
            var tokens = formulaEngine.GetTokens(rawText.Substring(1));
            foreach (var token in tokens)
            {
                if (token.type == TokenType.CellRef)
                {
                    var ci = IndexHelper.AlphaBasedToCellIndex(token.value);
                    var ciIdx = GetCellIdx(ci);
                    AddOrUpdateToRefTree(ciIdx, row, col);
                }
                else if (token.type == TokenType.CellRangeRef)
                {
                    var temp = token.value.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    var startCell = IndexHelper.AlphaBasedToCellIndex(temp[0]);
                    var endCell = IndexHelper.AlphaBasedToCellIndex(temp[1]);
                    var cellRange = new CellRange(startCell.row, endCell.row, startCell.column, endCell.column);
                    cellRange.ForEach((r, c) =>
                    {
                        var cidx = GetCellIdx(r, c);
                        AddOrUpdateToRefTree(cidx, row, col);
                    });
                }
            }
        }

        private void AddOrUpdateToRefTree(string idx, int refRowIndex, int refColIndx)
        {
            var ci = new CellIndex() { row = refRowIndex, column = refColIndx };
            if (RefTree.ContainsKey(idx))
            {
                var list = RefTree[idx];
                var existedItem = list.Find(item => item.row == refColIndx && item.column == refColIndx);
                if (existedItem == null)
                {
                    list.Add(ci);
                }
            }
            else
            {
                RefTree.Add(idx, new List<CellIndex>() { ci });
            }
        }

        private string GetCellIdx(CellIndex index)
        {
            return GetCellIdx(index.row, index.column);
        }

        private string GetCellIdx(int row, int col)
        {
            return string.Format("{0}_{1}", row, col);
        }

        public void SetFormulaEngine(FormulaEngine engine)
        {
            this.formulaEngine = engine;
        }

        /// <summary>
        /// Data of rows
        /// </summary>
        public Rows data;

        /// <summary>
        /// Data of column headers
        /// </summary>
        public ColumnHeaders columnHeaders;

        /// <summary>
        /// Headers to display 
        /// </summary>
        public int VisualHeaders;

        /// <summary>
        /// Get columns count of sheet
        /// </summary>
        /// <returns>Column count of sheet</returns>
        public int GetColumnCount()
        {
            int count = -1;
            if (data != null)
            {
                foreach (int key in data.items.innerDictionary.Keys)
                {
                    var row = data.items[key];
                    if (row.IsEmpty())
                    {
                        continue;
                    }

                    foreach (var cellKey in row.cells.Keys)
                    {
                        if (count < cellKey)
                        {
                            count = cellKey;
                        }
                    }
                }
            }
            return count + 1;
        }

        /// <summary>
        /// Get rows count of the sheet
        /// </summary>
        /// <returns>Row count of the sheet</returns>
        public int GetRowCount()
        {
            int count = -1;
            if (data != null)
            {
                foreach (int key in data.items.innerDictionary.Keys)
                {
                    if (count < key)
                    {
                        count = key;
                    }
                }
            }

            // Skip the last rows if there are empty
            while (count >= 0)
            {
                var row = this.data.GetRow(count);

                // If row is null, it should be empty row 
                if (row == null)
                {
                    count--;
                    continue;
                }

                if (row.IsEmpty())
                {
                    count--;
                    break;
                }

                break;
            }

            return count + 1;
        }

        /// <summary>
        /// Delete the cell
        /// </summary>
        /// <param name="rowIndex">Row index of cell</param>
        /// <param name="columnIndex">Column index of cell</param>
        internal void DeleteCell(int rowIndex, int columnIndex)
        {
            if (this.data == null)
            {
                return;
            }

            var row = this.data.GetRow(rowIndex);
            if (row != null && row.cells.ContainsKey(columnIndex))
            {
                row.cells.Remove(columnIndex);

                // delete empty row when row is default height
                if (row.cells.Count == 0 && row.height - Row.DEFAULT_HEIGHT < 0.01)
                {
                    this.data.items.Remove(rowIndex);
                }
            }
        }

        /// <summary>
        /// Get cell at specific position
        /// </summary>
        /// <param name="rowIndex">Index of row, starts from zero</param>
        /// <param name="columnIndex">Index of column, starts form zero</param>
        /// <returns>The cell or null if not exists</returns>
        public Cell GetCell(int rowIndex, int columnIndex)
        {
            if (this.data == null)
            {
                this.data = new Rows();
                return null;
            }

            var row = this.data.GetRow(rowIndex);
            if (row == null)
            {
                return null;
            }

            return row.GetCell(columnIndex);
        }

        /// <summary>
        /// Get cell at specific position
        /// </summary>
        /// <param name="alphaBasedCellIndex">Alpha based cell index</param>
        /// <returns>The cell or null if not exists</returns>
        public Cell GetCell(string alphaBasedCellIndex)
        {
            var cellIndex = IndexHelper.AlphaBasedToCellIndex(alphaBasedCellIndex);
            if (cellIndex == null)
            {
                return null;
            }

            return GetCell(cellIndex.row, cellIndex.column);
        }

        /// <summary>
        /// Set the raw value of cell at specific position
        /// </summary>
        /// <param name="rowIndex">Index of row, starts from zero</param>
        /// <param name="columnIndex">Index of column, starts form zero</param>
        /// <param name="rawText">Raw text for the cell</param>
        [Obsolete("SetCellRawValue is deprecated, please use SetRawValueInternal instead.")]
        internal void SetCellRawValue(int rowIndex, int columnIndex, string rawText)
        {
            SetRawValueInternal(rowIndex, columnIndex, rawText);
        }

        /// <summary>
        /// Set the raw value of cell at specific position
        /// </summary>
        /// <param name="rowIndex">Index of row, starts from zero</param>
        /// <param name="columnIndex">Index of column, starts form zero</param>
        /// <param name="rawText">Raw text for the cell</param>
        internal void SetRawValueInternal(int rowIndex, int columnIndex, string rawText)
        {
            if (rowIndex < 0)
            {
                return;
            }

            if (columnIndex < 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(rawText))
            {
                var cell = this.GetCell(rowIndex, columnIndex);
                if (cell == null)
                {
                    return;
                }

                this.DeleteCell(rowIndex, columnIndex);
                return;
            }

            if (this.data == null)
            {
                this.data = new Rows();
            }

            var row = this.data.GetRow(rowIndex);
            if (row == null)
            {
                row = new Row();
                this.data.AddRow(rowIndex, row);
            }

            string value = rawText;
            string trimedRaw = rawText.Trim();
            if (trimedRaw.StartsWith("="))
            {
                var returnValue = this.Evaluate(trimedRaw.Substring(1));
                bool isSingleUnityObject = returnValue is UnityEngine.Object;
                bool isMultipleUnityObject = returnValue is UnityEngine.Object[];
                if (isSingleUnityObject || isMultipleUnityObject)
                {
                    value = returnValue.GetType().ToString();
                    var cell = row.GetCell(columnIndex);
                    if (cell == null)
                    {
                        cell = new Cell();
                        row.cells.Add(columnIndex, cell);
                    }

                    cell.SetRawValue(rawText);
                    cell.SetValue(value);
                    if (isSingleUnityObject)
                    {
                        cell.SetUnityObject(returnValue as UnityEngine.Object);
                    }
                    else
                    {
                        cell.SetUnityObjects(returnValue as UnityEngine.Object[]);
                    }
                }
                else
                {
                    value = returnValue.ToString();
                }
            }
            else
            {
                rawText = string.Empty;
                var cell = row.GetCell(columnIndex);
                if (cell != null)
                {
                    cell.SetUnityObject(null);
                    cell.SetUnityObjects(null);
                }
            }

            row.SetCellValues(columnIndex, rawText, value);
        }

        internal void UpdateReferencedCells(int rowIndex, int columnIndex)
        {
            var idx = GetCellIdx(rowIndex, columnIndex);
            if (!RefTree.ContainsKey(idx))
            {
                return;
            }

            var cells = RefTree[idx];
            for (int i = 0; i < cells.Count; i++)
            {
                var cellIdx = cells[i];

                var cell = this.GetCell(cellIdx.row, cellIdx.column);
                if (cell == null)
                {
                    continue;
                }

                SetRawValueInternal(cellIdx.row, cellIdx.column, cell.GetRawValue());
                UpdateReferencedCells(cellIdx.row, cellIdx.column);
            }
        }

        /// <summary>
        /// Update cell
        /// </summary>
        /// <param name="rowIndex">Index of row, starts from zero</param>
        /// <param name="columnIndex">Index of column, starts form zero</param>
        internal void UpdateCell(int rowIndex, int columnIndex)
        {
            var cell = this.GetCell(rowIndex, columnIndex);
            if (cell != null)
            {
                this.SetRawValueInternal(rowIndex, columnIndex, cell.GetRawValue());
            }
        }

        /// <summary>
        /// Delete rows
        /// </summary>
        /// <param name="rowIndexes">Row indexes to be deleted</param>
        internal void DeleteRows(int[] rowIndexes)
        {
            if (this.data == null)
            {
                return;
            }

            this.data.items.DeleteWithMove(rowIndexes);
        }

        /// <summary>
        /// Add one row to the above of base row
        /// </summary>
        /// <param name="baseRowIndex">Base row</param>
        internal void AddRow(int baseRowIndex, int delta = 1)
        {
            if (this.data == null)
            {
                this.data = new Rows();
            }

            this.data.items.MoveBy(baseRowIndex, delta);
        }

        /// <summary>
        /// Delete columns 
        /// </summary>
        /// <param name="indexes">Column indexes to be deleted</param>
        internal void DeleteColumn(int[] indexes)
        {
            if (this.data != null)
            {
                var keys = this.data.items.Keys;
                foreach (var key in keys)
                {
                    var row = this.data.items[key];
                    row.cells.DeleteWithMove(indexes);
                }
            }

            if (this.columnHeaders != null)
            {
                columnHeaders.items.DeleteWithMove(indexes);
            }
        }

        /// <summary>
        /// Delete the settings of column headers
        /// </summary>
        /// <param name="index">Inde of column</param>
        internal void DeleteColumnHeaderSettings(int index)
        {
            if (columnHeaders != null)
            {
                if (columnHeaders.items.ContainsKey(index))
                {
                    columnHeaders.items.Remove(index);
                }
            }
        }

        /// <summary>
        /// Add one column to the left of base column
        /// </summary>
        /// <param name="baseColumnIndex">Base column</param>
        internal void AddColumn(int baseColumnIndex, int delta = 1)
        {
            if (this.data == null)
            {
                this.data = new Rows();
            }

            var keys = this.data.items.Keys;
            foreach (var key in keys)
            {
                var row = this.data.items[key];
                row.cells.MoveBy(baseColumnIndex, delta);
            }

            if (columnHeaders == null)
            {
                columnHeaders = new ColumnHeaders();
            }

            columnHeaders.items.MoveBy(baseColumnIndex, delta);
        }

        /// <summary>
        /// Set alias of a column header
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <param name="aliasValue">Value of alias</param>
        internal void SetColumnHeaderColumn(int index, string alias, int type, string field)
        {
            if (columnHeaders == null)
            {
                columnHeaders = new ColumnHeaders();
            }

            if (columnHeaders.items.ContainsKey(index))
            {
                columnHeaders.items[index].Alias = alias;
                columnHeaders.items[index].Type = type;
                columnHeaders.items[index].Field = field;
            }
            else
            {
                columnHeaders.items.Add(index, new ColumnHeader() { Alias = alias, Type = type, Field = field });
            }
        }

        /// <summary>
        /// Get alias of a column header
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Value of alias</returns>
        public string GetColumnHeaderAlias(int index)
        {
            if (columnHeaders == null)
            {
                return string.Empty;
            }

            if (columnHeaders.items.ContainsKey(index))
            {
                return columnHeaders.items[index].Alias;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get field of a column header
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Value of field</returns>
        public string GetColumnHeaderField(int index)
        {
            if (columnHeaders == null)
            {
                return string.Empty;
            }

            if (columnHeaders.items.ContainsKey(index))
            {
                return columnHeaders.items[index].Field;
            }

            return string.Empty;
        }

        /// <summary>
        /// Get data type of a column header
        /// </summary>
        /// <param name="index">Index of column</param>
        /// <returns>Value of type</returns>
        public int GetColumnHeaderType(int index)
        {
            if (columnHeaders == null)
            {
                return 0;
            }

            if (columnHeaders.items.ContainsKey(index))
            {
                return columnHeaders.items[index].Type;
            }

            return 0;
        }

        /// <summary>
        /// Get value of the formula of this sheet
        /// </summary>
        /// <param name="forumla">Formula string</param>
        /// <returns>Value of the forumla</returns>
        public object Evaluate(string forumla)
        {
            if (formulaEngine == null)
            {
                formulaEngine = new FormulaEngine(this);
            }

            return formulaEngine.Evaluate(forumla);
        }

        /// <summary>
        /// Clear all data 
        /// </summary>
        internal void Clear()
        {
            if (this.data != null && this.data.items != null)
            {
                this.data.items.Clear();
                HasToRebuildCache = true;
            }
        }
    }
}