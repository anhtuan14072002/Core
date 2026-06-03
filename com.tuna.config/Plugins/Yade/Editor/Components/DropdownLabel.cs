//  Copyright (c) 2021-present amlovey
//  
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class DropdownLabel : TextElement, IToolbarMenuElement
    {
        public DropdownMenu menu { get; private set; }

        private TextElement arrowElement;
        private TextElement textElement;

        public DropdownLabel()
        {
            menu = new DropdownMenu();
            menu.AppendAction("Cut", a => Debug.Log(a.name), DropdownMenuAction.AlwaysEnabled);

            this.style.position = Position.Absolute;
            this.style.fontSize = 12;

            this.AddToClassList("text-button");
            
            this.style.flexShrink = 0;
            this.style.flexDirection = FlexDirection.Row;

            textElement = new TextElement();
            textElement.style.fontSize = 13;
            textElement.style.marginRight = 13;
            textElement.text = text;
            textElement.pickingMode = PickingMode.Ignore;
            textElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            Add(textElement);

            arrowElement = new TextElement();
            arrowElement.AddToClassList("icon");
            arrowElement.BindMaterialIconFontIfNeeds();
            arrowElement.text = Icons.ArrowDropDown.value;
            arrowElement.AddToClassList("dropdown-arrow");
            arrowElement.style.position = Position.Absolute;
            arrowElement.style.top = 5;
            arrowElement.style.bottom = 0;
            arrowElement.style.right = 2;
            arrowElement.style.fontSize = 16;
            Add(arrowElement);
        }

        public bool IsArrowClicked(Vector2 worldPos)
        {
            return arrowElement.worldBound.Contains(worldPos);
        }

        public void ClearMenus()
        {
            menu = new DropdownMenu();
        }

        public void ShowDropdown()
        {
            this.ShowMenu();
        }
    }
}
