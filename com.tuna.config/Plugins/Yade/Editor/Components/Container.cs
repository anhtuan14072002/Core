//  Copyright (c) 2020-present amlovey
//  
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class Container : VisualElement
    {
        public Container()
        {
            this.style.position = Position.Absolute;
            this.SetEdgeDistance(0, 0, 0, 0);
            this.SetPadding(0);
        }
    }
}