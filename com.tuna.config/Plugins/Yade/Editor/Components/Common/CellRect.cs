//  Copyright (c) 2020-present amlovey
//  
namespace Yade.Editor
{
    public class CellRect
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public CellRect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool IsXYInRect(float x, float y)
        {
            return x >= this.x && y >= this.y && x <= this.x + this.width && y <= this.y + this.height;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3})", x, y, width, height);
        }
    }
}