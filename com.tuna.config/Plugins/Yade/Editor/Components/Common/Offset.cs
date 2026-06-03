//  Copyright (c) 2020-present amlovey
//  
namespace Yade.Editor
{
    public class Offset
    {
        public float left;
        public float top;

        public Offset() : this (0, 0)
        {

        }

        public Offset(float left, float top)
        {
            this.left = left;
            this.top = top;
        }
    }

    public class Size
    {
        public float width;
        public float height;

        public Size() : this (0, 0)
        {
            
        }

        public Size(float width, float height)
        {
            this.width = width;
            this.height = height;
        }
    }
}
