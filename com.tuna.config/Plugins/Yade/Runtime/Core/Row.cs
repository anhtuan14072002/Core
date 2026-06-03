//  Copyright (c) 2020-present amlovey
//  
using System;

namespace Yade.Runtime
{
    /// <summary>
    /// Data class of cells in a row
    /// </summary>
    [Serializable]
    public class CellsMap : IntIndexedSerializableDictionary<Cell> { }

    /// <summary>
    /// Data class of a row
    /// </summary>
    [Serializable]
    public class Row
    {
        /// <summary>
        /// Default height of a row
        /// </summary>
        public const float DEFAULT_HEIGHT = 25;

        /// <summary>
        /// Height of a row
        /// </summary>
        public float height;

        /// <summary>
        /// Data of cells
        /// </summary>
        public CellsMap cells;

        public Row(float height = DEFAULT_HEIGHT)
        {
            this.height = height;
            cells = new CellsMap();
        }

        /// <summary>
        /// Get data of a row
        /// </summary>
        /// <param name="index">Index of cell</param>
        /// <returns>Cell data</returns>
        public Cell GetCell(int index)
        {
            if (cells.ContainsKey(index))
            {
                return cells[index];
            }

            return null;
        }

        /// <summary>
        /// Set values of a cell
        /// </summary>
        /// <param name="index">Index of cell</param>
        /// <param name="rawValue">Raw value of cell. For example the formula of the row</param>
        /// <param name="value">Value of cell</param>
        internal void SetCellValues(int index, string rawValue, string value)
        {
            var cell = this.GetCell(index);
            if (cell == null)
            {
                cell = new Cell();
                cell.SetRawValue(rawValue);
                cell.SetValue(value);
                cells.Add(index, cell);
            }
            else
            {
                cell.SetRawValue(rawValue);
                cell.SetValue(value);
            }
        }

        /// <summary>
        /// Set cell data
        /// </summary>
        /// <param name="index">Index of cell</param>
        /// <param name="cell">Data of cell</param>
        internal void SetCell(int index, Cell cell)
        {
            if (cells.ContainsKey(index))
            {
                cells[index] = cell;
            }
            else
            {
                cells.Add(index, cell);
            }
        }

        /// <summary>
        /// Whether this row is empty (i.e has no cells or all cells don't have raw value)
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            if (cells == null || cells.Values.Length == 0)
            {
                return true;
            }

            bool isEmpty = true;
            foreach (var cell in cells.Values)
            {
                if (!string.IsNullOrEmpty(cell.GetRawValue()))
                {
                    isEmpty = false;
                    break;
                }
            }

            return isEmpty;
        }
    }

    /// <summary>
    /// Data of rows in sheet
    /// </summary>
    [Serializable]
    public class RowsMap : IntIndexedSerializableDictionary<Row> { }

    /// <summary>
    /// Data class of rows in sheet
    /// </summary>
    [Serializable]
    public class Rows
    {
        /// <summary>
        /// Rows data
        /// </summary>
        public RowsMap items;

        public Rows()
        {
            items = new RowsMap();
        }

        public Row this[int row]
        {
            get
            {
                return this.GetRow(row);
            }
            internal set
            {
                if (items.ContainsKey(row))
                {
                    items[row] = value;
                }
                else
                {
                    items.Add(row, value);
                }
            }
        }

        /// <summary>
        /// Get height of a row
        /// </summary>
        /// <param name="index">Index of row</param>
        /// <returns>Height of row</returns>
        public float GetRowHeight(int index)
        {
            if (items.ContainsKey(index))
            {
                return items[index].height;
            }

            return Row.DEFAULT_HEIGHT;
        }

        /// <summary>
        /// Set height of a row
        /// </summary>
        /// <param name="index">Index of row</param>
        /// <param name="height">Height of row</param>
        internal void SetRowHieght(int index, float height)
        {
            if (items.ContainsKey(index))
            {
                items[index].height = height;
                return;
            }

            Row row = new Row();
            row.height = height;
            items.Add(index, row);
        }

        /// <summary>
        /// Get row in sheet
        /// </summary>
        /// <param name="index">Index of row</param>
        /// <returns>Row data</returns>
        public Row GetRow(int index)
        {
            if (items.ContainsKey(index))
            {
                return items[index];
            }

            return null;
        }

        /// <summary>
        /// Add a row into sheet at specific index
        /// </summary>
        /// <param name="index">Index of row</param>
        /// <param name="row">Row data</param>
        internal void AddRow(int index, Row row)
        {
            if (items.ContainsKey(index))
            {
                items[index] = row;
                return;
            }

            items.Add(index, row);
        }
    }
}