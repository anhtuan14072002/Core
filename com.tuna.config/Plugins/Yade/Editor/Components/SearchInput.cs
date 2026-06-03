//  Copyright (c) 2020-present amlovey
//  
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class SearchInput : Element
    {
        private TextField textField;
        private TextElement clearLabel;
        private TextElement searchIcon;
        private TextElement placeholderText;
        private TextElement nextLabel;
        private TextElement previousLabel;

        public Action<string> OnSubmit;
        public Action<string> OnValueChanged;
        public Action OnGoToNextClick;
        public Action OnGoToPreviousClick;
        public Action OnClearClick;

        private bool hasValueChange;
        private string lastSearchValue;

        public SearchInput(string searchText, bool inSearchState)
        {
            this.lastSearchValue = string.Empty;
            this.hasValueChange = false;
            this.style.fontSize = 13;
            this.style.flexDirection = FlexDirection.Row;
            this.style.alignItems = Align.Center;
            this.AddToClassList("search-input");

            RenderTextField(searchText);
            RenderSeachIcon();
            RenderClearButton();
            RenderNextResultButton();
            RenderPreviousResultButton();

            this.UpdateUIState();
            this.SetResultNavigationUIEnable(inSearchState);
        }

        private void RenderTextField(string defaultText)
        {
            this.textField = new TextField();
            this.textField.RegisterCallback<FocusInEvent>(evt => Input.imeCompositionMode = IMECompositionMode.On);
            this.textField.RegisterCallback<FocusOutEvent>(evt => Input.imeCompositionMode = IMECompositionMode.Auto);
            this.textField.RegisterCallback<KeyDownEvent>(evt => evt.StopImmediatePropagation());
            this.textField.style.height = 24;
            this.textField.style.position = Position.Absolute;
            this.textField.style.left = 0;
            this.textField.style.right = 0;
            this.textField.style.marginBottom = 0;
            this.textField.value = defaultText;
            lastSearchValue = defaultText;
            this.textField.RegisterValueChangedCallback(OnTextFieldValueChanged);
            this.textField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnKeyboradDown);
            this.Add(textField);
        }

        private void RenderSeachIcon()
        {
            searchIcon = new TextElement();
            searchIcon.BindMaterialIconFontIfNeeds();
            searchIcon.AddToClassList("icon");
            searchIcon.text = Icons.Search.value;
            searchIcon.style.width = 24;
            searchIcon.style.height = 24;
            searchIcon.style.fontSize = 12;
            searchIcon.style.marginLeft = 5;
            searchIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
            this.Add(searchIcon);
        }

        private void RenderClearButton()
        {
            clearLabel = new TextElement();
            clearLabel.BindMaterialIconFontIfNeeds();
            clearLabel.AddToClassList("icon");
            clearLabel.text = Icons.Close.value;
            clearLabel.style.position = Position.Absolute;
            clearLabel.style.right = 0;
            clearLabel.style.width = 24;
            clearLabel.style.height = 24;
            clearLabel.style.fontSize = 12;
            clearLabel.style.marginRight = 5;
            clearLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            clearLabel.style.color = ColorHelper.Parse(Theme.Current.ForegroundColor);
            clearLabel.RegisterCallback<MouseDownEvent>(OnClearButtonClick);
            this.Add(clearLabel);
        }

        private void RenderNextResultButton()
        {
            nextLabel = new TextElement();
            nextLabel.BindMaterialIconFontIfNeeds();
            nextLabel.AddToClassList("icon");
            nextLabel.text = Icons.ArrowForward.value;
            nextLabel.style.position = Position.Absolute;
            nextLabel.style.right = 24;
            nextLabel.style.width = 24;
            nextLabel.style.height = 24;
            nextLabel.style.fontSize = 12;
            nextLabel.style.marginRight = 2;
            nextLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            nextLabel.style.color = ColorHelper.Parse(Theme.Current.ForegroundColor);
            nextLabel.RegisterCallback<MouseDownEvent>(OnNextClick);

            this.Add(nextLabel);
        }

        private void RenderPreviousResultButton()
        {
            previousLabel = new TextElement();
            previousLabel.BindMaterialIconFontIfNeeds();
            previousLabel.AddToClassList("icon");
            previousLabel.text = Icons.ArrowBack.value;
            previousLabel.style.position = Position.Absolute;
            previousLabel.style.right = 44;
            previousLabel.style.width = 24;
            previousLabel.style.height = 24;
            previousLabel.style.fontSize = 12;
            previousLabel.style.marginRight = 2;
            previousLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            previousLabel.style.color = ColorHelper.Parse(Theme.Current.ForegroundColor);
            previousLabel.RegisterCallback<MouseDownEvent>(OnPreviousClick);

            this.Add(previousLabel);
        }

        public void Reset()
        {
            SetText(string.Empty);
        }

        public void SetText(string text)
        {
            this.textField.value = text;
            this.UpdateUIState();
        }

        private void OnKeyboradDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                var newValue= this.textField.value;
                this.textField.Focus();

                var oldAndNewValueEqual = newValue.Equals(lastSearchValue, StringComparison.OrdinalIgnoreCase);
                if (oldAndNewValueEqual)
                {
                    if (evt.shiftKey)
                    {
                        if (this.OnGoToPreviousClick != null)
                        {
                            this.OnGoToPreviousClick();
                        }
                    }
                    else
                    {
                        if (this.OnGoToNextClick != null)
                        {
                            this.OnGoToNextClick();
                        }
                    }

                    return;
                }

                if (OnSubmit != null&& hasValueChange)
                {
                    lastSearchValue = newValue;
                    OnSubmit(this.textField.value);
                    hasValueChange = false;
                }
            }
        }

        private void OnClearButtonClick(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                this.textField.value = string.Empty;
                this.UpdateUIState();
                this.SetResultNavigationUIEnable(false);    

                if (OnClearClick != null)
                {
                    this.OnClearClick();
                }

                this.schedule.Execute(this.textField.Focus).ExecuteLater(10);
            }
        }

        private void OnTextFieldValueChanged(ChangeEvent<string> evt)
        {
            if (OnValueChanged != null)
            {
                OnValueChanged(this.textField.value);
            }

            this.UpdateUIState();
            this.hasValueChange = true;
        }

        private void OnPreviousClick(MouseDownEvent evt)
        {
            if (this.OnGoToPreviousClick != null)
            {
                this.OnGoToPreviousClick();
            }
        }

        private void OnNextClick(MouseDownEvent evt)
        {
            if (this.OnGoToNextClick != null)
            {
                this.OnGoToNextClick();
            }
        }

        public void SetResultNavigationUIEnable(bool enable)
        {
            this.nextLabel.visible = enable;
            this.previousLabel.visible = enable;
            if (enable)
            {
                this.RemoveFromClassList("search-input");
                this.AddToClassList("search-input-with-result");
            }
            else
            {
                this.RemoveFromClassList("search-input-with-result");
                this.AddToClassList("search-input");
            }
        }

        private void UpdateUIState()
        {
            bool hasValue = !string.IsNullOrEmpty(this.textField.value);
            this.clearLabel.visible = hasValue;
            this.searchIcon.style.color = ColorHelper.Parse(hasValue ? "#909090" : Theme.Current.ForegroundColor);
            if (!hasValue)
            {
                this.SetResultNavigationUIEnable(false);
            }
        }
    }
}
