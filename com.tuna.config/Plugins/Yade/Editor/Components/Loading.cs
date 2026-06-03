//  Copyright (c) 2022-present amlovey
//  
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class Loading : Container
    {
        private VisualElement dialog;
        private Label loadingText;
        
        public Loading()
        {
            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.LayerMask);
            this.style.flexDirection = FlexDirection.Column;

            RenderDialogBackground();
            RenderLoadingText();
        }

        public void Show(string msg)
        {
            loadingText.text = msg;
            this.visible = true;
        }

        private void RenderLoadingText()
        {
            loadingText = new Label();
            loadingText.style.alignSelf = Align.Center;
            loadingText.style.fontSize = 14;
            loadingText.style.top = 37;

            this.dialog.Add(loadingText);
        }

        private void RenderDialogBackground()
        {
            dialog = new VisualElement();
            dialog.SetBorderRadius(5);
            dialog.style.backgroundColor = ColorHelper.Parse(Theme.Current.DialogBackground);
            dialog.style.top = 210;
            dialog.style.alignSelf = Align.Center;
            dialog.style.width = 240;
            dialog.style.height = 88;
            dialog.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());
            dialog.pickingMode = PickingMode.Position;

            this.Add(dialog);
        }

        public void Hide()
        {
            this.visible = false;
        }
    }
}
