//  Copyright (c) 2022-present amlovey
//  
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class SelectionDialog : Container
    {
        public Action OnClose;

        private Clickable clickable;

        private VisualElement dialog;
        private Label titleLabel;
        private ListView list;
        private List<string> options;
        private int selectedIdx;
        private Action<int> callback;
        private List<Toggle> toggles;

        public SelectionDialog()
        {
            this.clickable = new Clickable(this.OnClicked);
            this.AddManipulator(this.clickable);

            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.LayerMask);
            this.style.flexDirection = FlexDirection.Column;

            this.selectedIdx = 0;
            this.options = new List<string>();
            toggles = new List<Toggle>();

            RenderDialogBackground();
            RenderTitle();
            RenderList();
            RenderButtons();
        }

        public void Hide()
        {
            this.list.itemsSource = null;
            this.list.RebuildView();
            this.visible = false;
        }

        public void Show(
            string title,
            List<string> options,
            Action<int> callback)
        {
            this.selectedIdx = 0;
            this.titleLabel.text = title;
            this.options = options;
            this.list.itemsSource = options;
            this.callback = callback;
            this.visible = true;
            this.list.RebuildView();
        }


        private void RenderList()
        {
            list = new ListView();
            list.SetItemHeight(32);
            list.selectionType = SelectionType.None;
            list.style.position = Position.Absolute;
            list.style.alignSelf = Align.Center;
            list.style.top = 42;
            list.style.width = 360;
            list.style.height = 178;
            list.style.borderBottomColor = ColorHelper.Parse(Theme.Current.BorderColor);
            list.style.borderBottomWidth = 1;
            list.style.paddingBottom = 6;

            list.makeItem = () => 
            {
                var t = new Toggle();
                toggles.Add(t);
                return t;
            };
            
            list.bindItem = (visualElement, index) =>
            {
                var toggle = visualElement as Toggle;
                toggle.SetValueWithoutNotify(index == this.selectedIdx);

                if (toggle != null)
                {
                    toggle.text = "  " + options[index];
                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        OnToggleValueChanged(index, toggle, evt.newValue);
                    });
                }
            };

            dialog.Add(list);
        }

        private void OnToggleValueChanged(int index, Toggle toggle, bool selected)
        {
            foreach (var item in toggles)
            {
                if (item != toggle)
                {
                    item.SetValueWithoutNotify(false);
                }   
                else
                {
                    selectedIdx = selected ? index : -1;
                }
            }
        }

        private void RenderButtons()
        {
            var buttonGroups = new Container();
            buttonGroups.SetEdgeDistance(15, 0, 15, 0);
            buttonGroups.style.top = 226;
            buttonGroups.style.flexDirection = FlexDirection.RowReverse;

            Button Ok = new Button();
            Ok.text = "Import";
            Ok.style.marginLeft = 12;
            Ok.style.marginBottom = 10;
            Ok.style.unityFontStyleAndWeight = FontStyle.Bold;
            Ok.clickable.clicked += () =>
            {
                if (selectedIdx == -1)
                {
                    UnityEditor.EditorUtility.DisplayDialog("Error", "Need select a sheet to import", "Ok");
                    return;
                }

                if (callback != null)
                {
                    callback(this.selectedIdx);
                }

                Hide();
            };

            buttonGroups.Add(Ok);

            dialog.Add(buttonGroups);
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
            line.style.width = 360;
            line.style.height = 1;
            line.style.alignSelf = Align.Center;

            dialog.Add(line);
        }

        private void RenderDialogBackground()
        {
            dialog = new VisualElement();
            dialog.SetBorderRadius(5);
            dialog.style.backgroundColor = ColorHelper.Parse(Theme.Current.DialogBackground);
            dialog.style.top = 180;
            dialog.style.alignSelf = Align.Center;
            dialog.style.width = 390;
            dialog.style.height = 260;
            dialog.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());
            dialog.pickingMode = PickingMode.Position;

            this.Add(dialog);
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

