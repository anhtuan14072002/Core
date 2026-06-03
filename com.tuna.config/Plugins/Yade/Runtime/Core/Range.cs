//  Copyright (c) 2020-present amlovey
//  
using System;

namespace Yade.Runtime
{
    /// <summary>
    /// Range of cells
    /// </summary>
    [Serializable]
    public class CellRange
    {
        /// <summary>
        /// Not exist cell range
        /// </summary>
        public static CellRange None = new CellRange(int.MinValue, int.MinValue, int.MinValue, int.MinValue);

        /// <summary>
        /// Start row index of the range
        /// </summary>
        public int startRow;

        /// <summary>
        /// End row index of the range
        /// </summary>
        public int endRow;

        /// <summary>
        /// Start column index of the range
        /// </summary>
        public int startColumn;

        /// <summary>
        /// End column index of the range
        /// </summary>
        public int endColumn;

        public CellRange(int startRow, int endRow, int startColumn, int endColumn)
        {
            ThrowIfCellIsInvalid();
            
            this.startRow = startRow;
            this.endRow = endRow;
            this.startColumn = startColumn;
            this.endColumn = endColumn;
        }

        /// <summary>
        /// Whether the range has more than one cells
        /// </summary>
        /// <returns>True for rang has more than one cells, otherwise this method return False</returns>
        public bool HasMultipleCells()
        {
            return (this.endRow - this.startRow) > 0 || (this.endColumn - this.startColumn) > 0;
        }

        /// <summary>
        /// Exectue callback on each cell
        /// </summary>
        /// <param name="forEachCallback">Callback function will be executed on each cell</param>
        public void ForEach(Action<int, int> forEachCallback)
        {
            if (forEachCallback == null)
            {
                return;
            }

            for (int i = startRow; i <= endRow; i++)
            {
                for (int j = startColumn; j <= endColumn; j++)
                {
                    forEachCallback(i, j);
                }
            }
        }

        /// <summary>
        /// Combine two range of cells
        /// </summary>
        /// <param name="other">The other range of cells</param>
        /// <returns>New range</returns>
        public CellRange Union(CellRange other)
        {
            return new CellRange(
                this.startRow > other.startRow ? other.startRow : this.startRow,
                this.endRow > other.endRow ? this.endRow : other.endRow,
                this.startColumn > other.startColumn ? other.startColumn : this.startColumn,
                this.endColumn > other.endColumn ? this.endColumn : other.endColumn
            );
        }

        /// <summary>
        /// Whether two rangs are equal
        /// </summary>
        /// <param name="other">The other range of cells</param>
        /// <returns>True for two ranges are equal</returns>
        public bool Equals(CellRange other)
        {
            return this.startRow == other.startRow
                && this.endRow == other.endRow
                && this.startColumn == other.startColumn
                && this.endColumn == other.endColumn;
        }

        /// <summary>
        /// Wheter the specific row and column are in the range of cells
        /// </summary>
        /// <param name="row">Index of row</param>
        /// <param name="column">Index of column</param>
        /// <returns></returns>
        public bool Contains(int row, int column)
        {
            return this.startRow <= row && row <= this.endRow && this.startColumn <= column && column <= this.endColumn;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3})", startRow, endRow, startColumn, endColumn);
        }

        private void ThrowIfCellIsInvalid()
        {
            if (startRow > endRow)
            {
                throw new Exception("Invalid cell range, startRow > endRow");
            }

            if (startColumn > endColumn)
            {
                throw new Exception("Invalid cell range, startColumn > endColumn");
            }

            if (startRow < 0 || endRow < 0 || startColumn < 0 || endColumn < 0)
            {
                throw new Exception("Cell range cannot contains negative index value");
            }
        }

        internal bool IsWholeColumn()
        {
            return startRow < 0;
        }

        internal bool IsWholeRow()
        {
            return startColumn < 0;
        }
    }
}
