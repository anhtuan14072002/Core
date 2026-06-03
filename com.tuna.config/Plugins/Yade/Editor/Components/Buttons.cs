//  Copyright (c) 2020-present amlovey
//  
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class IconButton : Button
    {
        public Action OnClick;

        public IconButton(Icon icon)
        {
            this.SetPadding(5, 5);
            this.SetMargin(1, 0);
            this.style.backgroundImage = null;
            this.AddToClassList("hoverable");
            this.AddToClassList("icon-button");
            this.BindMaterialIconFontIfNeeds();

            this.text = icon.value;
            this.clickable.clicked += OnClicked;
        }

        private void OnClicked()
        {
            if (OnClick != null)
            {
                OnClick();
            }
        }
    }

    public class ToogleButton : Button
    {
        public Action<bool> OnValueChanged;

        private bool isOn;
        public bool IsOn
        {
            get
            {
                return isOn;
            }
            set
            {
                isOn = value;
                this.EnableInClassList("toogle_isOn", isOn);
            }
        }

        public ToogleButton(Icon icon)
        {
            isOn = false;
            this.SetPadding(5, 5);
            this.SetMargin(1, 0);
            this.style.backgroundImage = null;
            this.AddToClassList("hoverable");
            this.AddToClassList("icon-button");
            this.BindMaterialIconFontIfNeeds();
            this.text = icon.value;
            this.clickable.clicked += OnClicked;
        }

        private void OnClicked()
        {
            isOn = !isOn;
            if (OnValueChanged != null)
            {
                OnValueChanged(isOn);
            }
            this.EnableInClassList("toogle_isOn", isOn);
        }
    }

    public class DropdownIconButton : TextElement, IToolbarMenuElement
    {
        Clickable clickable;

        public DropdownMenu menu { get; }
        private TextElement arrowElement;

        public DropdownIconButton(Icon icon)
        {
            this.text = icon.value;
            menu = new DropdownMenu();
            clickable = new Clickable(this.ShowMenu);
            this.AddManipulator(clickable);

            this.EnableInClassList("unity-toolbar-menu--popup", true);
            this.AddToClassList("hoverable");
            this.AddToClassList("icon-button");
            this.BindMaterialIconFontIfNeeds();

            this.style.unityTextAlign = TextAnchor.MiddleLeft;
            this.style.marginTop = 0;
            this.style.paddingLeft = 5;
            this.style.width = 40;
            this.style.flexShrink = 0;

            arrowElement = new TextElement();
            arrowElement.text = Icons.ArrowDropDown.value;
            arrowElement.style.position = Position.Absolute;
            arrowElement.style.top = 0;
            arrowElement.style.bottom = 0;
            arrowElement.style.right = 3;
            arrowElement.style.fontSize = 16;
            arrowElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            arrowElement.pickingMode = PickingMode.Ignore;
            Add(arrowElement);
        }
    }

    public class DropdownTextButton : TextElement, IToolbarMenuElement
    {
        Clickable clickable;

        public DropdownMenu menu { get; }
        private TextElement arrowElement;
        private TextElement textElement;

        public DropdownTextButton(string text)
        {
            menu = new DropdownMenu();
            clickable = new Clickable(this.ShowMenu);
            this.AddManipulator(clickable);

            this.AddToClassList("hoverable");
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
            arrowElement.style.position = Position.Absolute;
            arrowElement.style.top = 5;
            arrowElement.style.bottom = 0;
            arrowElement.style.right = 2;
            arrowElement.style.fontSize = 16;
            arrowElement.pickingMode = PickingMode.Ignore;
            Add(arrowElement);
        }
    }
}