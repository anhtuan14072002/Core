//  Copyright (c) 2020-present amlovey
//  
namespace Yade.Editor
{
    public class Resizer : Element
    {
        private bool vertical;

        public Resizer(bool vertical)
        {
            this.vertical = vertical;
            this.AddToClassList(vertical ? "resizer-vertical" : "resizer-horizontal");
        }

        public void SetRect(CellRect rect)
        {
            this.SetOffset(new Offset(rect.x, rect.y));
            this.SetSize(new Size(rect.width, rect.height));
        }
    }
}
