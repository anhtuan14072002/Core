//  Copyright (c) 2020-present amlovey
//  
using System;
using Yade.Runtime;

namespace Yade.Editor
{
    [Serializable]
    public class ViewRange : CellRange
    {
        public float width;
        public float height;

        public ViewRange(int startRow, int endRow, int startColumn, int endColumn, float width, float height)
            : base(startRow, endRow, startColumn, endColumn)
        {
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3}, {4}, {5})", startRow, endRow, startColumn, endColumn, width, height);
        }

        public bool Intersects(CellRange range)
        {
            if (this.startRow - 1 > range.endRow)
            {
                return false;
            }

            if (this.startColumn - 1 > range.endColumn)
            {
                return false;
            }

            if (this.endColumn + 1 < range.startColumn)
            {
                return false;
            }

            if (this.endRow + 1 < range.startRow)
            {
                return false;
            }

            return true;
        }
    }
}