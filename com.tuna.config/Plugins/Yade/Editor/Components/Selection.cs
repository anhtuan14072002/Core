//  Copyright (c) 2020-present amlovey
//  
using System;
using UnityEngine;

namespace Yade.Editor
{
    public enum SelectorMoveDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public class AutoFillSelector : Element
    {
        private Color color = ColorHelper.Parse(Theme.Current.AutoFillSelector);

        public AutoFillSelector()
        {
            this.SetBorderWidth(2);
            this.SetBorderColor(color);
        }

        public void SetRect(CellRect rect)
        {
            this.SetOffset(new Offset(rect.x, rect.y));
            this.SetSize(new Size(rect.width + 2f, rect.height + 2f));
        }
    }

    public class DragingSelector : AutoFillSelector
    {
        public DragingSelector()
        {
            this.SetBorderColor(ColorHelper.Parse(Theme.Current.DragSelectorBorder));
        }
    }


    public class Selector : Element
    {
        private Element autoFillElement;

        public event Action OnSelectorChanged;

        public Selector()
        {
            this.SetBorderWidth(2);
            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.SelectorBackgroud);
            this.SetBorderColor(ColorHelper.Parse(Theme.Current.Selector));
            this.RenderHandle();
        }

        private void RenderHandle()
        {
            autoFillElement = new Element();
            autoFillElement.AddToClassList("yade-selector-auto-fill");
            autoFillElement.style.width = 8f;
            autoFillElement.style.height = 8f;
            autoFillElement.style.backgroundColor = ColorHelper.Parse(Theme.Current.Selector);
            autoFillElement.SetBorderColor(Color.white);
            autoFillElement.SetBorderWidth(1);

            this.Add(autoFillElement);
        }

        public void ShowHandle()
        {
            this.autoFillElement.Show();
        }

        public override void Show()
        {
            base.Show();
            ShowHandle();
        }

        public override void Hide()
        {
            base.Hide();
            HideHandle();
        }

        public void HideHandle()
        {
            this.autoFillElement.Hide();
        }

        public void SetToExtraMode()
        {
            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.SelectorExtraBackround);
            this.SetBorderColor(ColorHelper.Parse(Theme.Current.SelectorExtraBorder));
        }

        public void SetToCopyMode()
        {
            this.SetBorderColor(ColorHelper.Parse(Theme.Current.SelectorCopyModeBorder));
            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.SelectorCopyModeBackground);
        }

        public void SetRect(CellRect rect)
        {
            this.SetOffset(new Offset(rect.x, rect.y));
            this.SetSize(new Size(rect.width + 2f, rect.height + 2f));
            this.autoFillElement.SetOffset(new Offset(rect.width - 6, rect.height - 6));

            if (this.OnSelectorChanged != null)
            {
                this.OnSelectorChanged();
            }

            this.Show();
        }
    }
}