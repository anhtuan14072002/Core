//  Copyright (c) 2022-present amlovey
//  
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class TextInputDialog : Container
    {
        public Action OnClose;

        private Clickable clickable;
        private VisualElement dialog;
        private Label titleLabel;
        private Label descriptionLabel;
        private TextField urlInput;
        private Action<string> callback;

        public TextInputDialog()
        {
            this.clickable = new Clickable(this.OnClicked);
            this.AddManipulator(this.clickable);

            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.LayerMask);
            this.style.flexDirection = FlexDirection.Column;

            RenderDialogBackground();
            RenderTitle();
            RenderContent();
            RenderButtons();
        }

        private void RenderContent()
        {
            descriptionLabel = new Label();
            descriptionLabel.SetEdgeDistance(15, 0, 15, 0);
            descriptionLabel.style.position = Position.Absolute;
            descriptionLabel.style.top = 52;
            this.dialog.Add(descriptionLabel);

            urlInput = new TextField();
            urlInput.RegisterCallback<KeyDownEvent>(evt => evt.StopImmediatePropagation());
            urlInput.SetEdgeDistance(15, 0, 15, 0);
            urlInput.style.position = Position.Absolute;
            urlInput.style.top = 76;
            urlInput.style.height = 24;
            this.dialog.Add(urlInput);
        }

        public void Hide()
        {
            this.visible = false;
        }

        private void RenderButtons()
        {
            var buttonGroups = new Container();
            buttonGroups.SetEdgeDistance(15, 0, 15, 0);
            buttonGroups.style.top = 114;
            buttonGroups.style.flexDirection = FlexDirection.RowReverse;

            Button Ok = new Button();
            Ok.text = "Import";
            Ok.style.marginLeft = 12;
            Ok.style.marginBottom = 10;
            Ok.style.unityFontStyleAndWeight = FontStyle.Bold;
            Ok.clickable.clicked += () =>
            {
                if (callback != null)
                {
                    callback(urlInput.value);
                }

                Hide();
            };

            buttonGroups.Add(Ok);

            dialog.Add(buttonGroups);
        }

        private void RenderDialogBackground()
        {
            dialog = new VisualElement();
            dialog.SetBorderRadius(5);
            dialog.style.backgroundColor = ColorHelper.Parse(Theme.Current.DialogBackground);
            dialog.style.top = 180;
            dialog.style.alignSelf = Align.Center;
            dialog.style.width = 360;
            dialog.style.height = 148;
            dialog.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());
            dialog.pickingMode = PickingMode.Position;

            this.Add(dialog);
        }

        public void Show(string title, string description, Action<string> callback)
        {
            this.titleLabel.text = title;
            this.descriptionLabel.text = description;
            this.urlInput.SetValueWithoutNotify(string.Empty);
            this.callback = callback;
            this.visible = true;
        }

        private void RenderTitle()
        {
            titleLabel = new Label();
            titleLabel.style.position = Position.Absolute;
            titleLabel.style.fontSize = 18;
            titleLabel.style.left = 15;
            titleLabel.style.height = 36;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            dialog.Add(titleLabel);

            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.backgroundColor = ColorHelper.Parse(Theme.Current.EditorLine);
            line.style.top = 36;
            line.style.width = 330;
            line.style.height = 1;
            line.style.alignSelf = Align.Center;

            dialog.Add(line);
        }

        private void OnClicked()
        {
            if (OnClose != null)
            {
                OnClose();
            }
        }
    }
}
