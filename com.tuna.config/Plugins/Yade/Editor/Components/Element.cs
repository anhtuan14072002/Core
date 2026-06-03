//  Copyright (c) 2020-present amlovey
//  
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class Element : VisualElement
    {
        public Element()
        {
            this.style.position = Position.Absolute;
            this.style.left = 0;
            this.style.top = 0;
            this.style.right = float.NaN;
            this.style.bottom = float.NaN;
            this.style.width = 0;
            this.style.height = 0;
            this.style.flexShrink = 0;
            this.style.flexGrow = 0;
        }

        public void SetOffset(Offset offset)
        {
            this.style.left = offset.left;
            this.style.top = offset.top;
        }

        public void SetSize(Size size)
        {
            this.style.width = size.width;
            this.style.height = size.height;
        }

        public virtual void Hide()
        {
            this.visible = false;
        }

        public virtual void Show()
        {
            this.visible = true;
        }
    }
}
