//  Copyright (c) 2020-present amlovey
//  
using System;

namespace Yade.Runtime
{
    /// <summary>
    /// Header of column
    /// </summary>
    [Serializable]
    public class ColumnHeader
    {  
        /// <summary>
        /// Default width of column
        /// </summary>
        public const float DEFAULT_WIDTH = 100;

        /// <summary>
        /// Width of column
        /// </summary>
        public float Width;

        /// <summary>
        /// Alias of column
        /// </summary>
        public string Alias;

        /// <summary>
        /// Date type of this column (for code generation)
        /// </summary>
        public int Type;

        /// <summary>
        /// Field name of this column (for code generation) 
        /// </summary>
        public string Field;

        public ColumnHeader(float width = DEFAULT_WIDTH)
        {
            this.Width = width;
        }
    }

    [Serializable]
    public class ColumnHeaderMap : IntIndexedSerializableDictionary<ColumnHeader> { }
    
    /// <summary>
    /// Header of columns
    /// </summary>
    [Serializable]
    public class ColumnHeaders
    {
        /// <summary>
        /// Column header items
        /// </summary>
        public ColumnHeaderMap items;

        public ColumnHeaders()
        {
            items = new ColumnHeaderMap();
        }

        /// <summary>
        /// Get width of a column
        /// </summary>
        /// <param name="columnIndex">Index of column</param>
        /// <returns>Width of column</returns>
        public float GetColumnWidth(int columnIndex)
        {
            if (items.ContainsKey(columnIndex))
            {
                return items[columnIndex].Width;
            }

            return ColumnHeader.DEFAULT_WIDTH;
        }

        /// <summary>
        /// Set width of column
        /// </summary>
        /// <param name="columnIndex">Index of column</param>
        /// <param name="width">width of column</param>
        internal void SetColumnWidth(int columnIndex, float width)
        {
            if (items.ContainsKey(columnIndex))
            {
                items[columnIndex].Width = width;
                return;
            }

            ColumnHeader column = new ColumnHeader();
            column.Width = width;
            items.Add(columnIndex, column);
        }
    }
}