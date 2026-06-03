//  Copyright (c) 2020-present amlovey
//  
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Yade.Runtime;
using Wrap = UnityEngine.UIElements.Wrap;

namespace Yade.Editor
{
    public enum EditorActiveMode
    {
        None,
        Keyboard,
        Mouse,
        Functions,
    }

    public class CellInputEditor : Element
    {
        private TextField input;
        public TextField InputField
        {
            get
            {
                return input;
            }
        }

        private Cell cell;
        private Label label;

        private float defautWidth;
        private string defautValue;

        public bool AutoFocus { get; set; }
        private bool allowAutoFocus;

        private EditorActiveMode mode;
        public EditorActiveMode Mode
        {
            get
            {
                return mode;
            }
        }

        private bool isFocusing;
        public bool IsFocusing
        {
            get
            {
                return isFocusing;
            }
        }

        public Action<string> OnSubmit;
        public Action<KeyDownEvent> OnKeyDownEvent;
        public Action OnFocused;
        public Action<string> OnValueUpdate;

        private bool isFromPaste = false;
        private bool isBreakLine = false;

        public CellInputEditor()
        {
            this.style.flexWrap = Wrap.NoWrap;
            this.style.overflow = Overflow.Visible;

            label = new Label();
            label.style.position = Position.Absolute;
            label.visible = false;
            this.Add(label);

            mode = EditorActiveMode.None;
            input = new TextField();
            input.multiline = true;
            input.RegisterValueChangedCallback(OnValueChanged);
            input.RegisterCallback<FocusInEvent>(evt => Input.imeCompositionMode = IMECompositionMode.On);
            input.RegisterCallback<FocusOutEvent>(evt => Input.imeCompositionMode = IMECompositionMode.Auto);
            input.style.marginLeft = 0;
            input.SetBorderWidth(0);
            input.pickingMode = PickingMode.Position;
            input.delegatesFocus = true;
            input.RegisterCallback<KeyDownEvent>(OnKeyDown);

            this.Add(input);
            this.SetPadding(2, 2);
            isFocusing = false;

            SetAllowAutoFocus(true);
            ForceFocus();
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            if (isFromPaste)
            {
                isFromPaste = false;
                return;
            }

            if (isBreakLine)
            {
                isBreakLine = false;
                return;
            }

            if (cell == null)
            {
                cell = new Cell(evt.newValue);
                defautValue = null;
                isFocusing = true;
            }

            SetInputWidthByValue(evt.newValue);
            isFocusing = true;

            if (OnValueUpdate != null)
            {
                OnValueUpdate(evt.newValue);
            }
        }

        private void SetInputWidthByValue(string value)
        {
            label.text = value;
            var size = label.MeasureTextSize(label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            SetInputWidth(size.x + 10);
        }

        private void SetInputWidth(float charWidth)
        {
            var width = charWidth > this.defautWidth ? charWidth : this.defautWidth;
            this.input.style.width = width;
            this.style.width = width;
        }

        public void SetActiveMode(EditorActiveMode mode)
        {
            this.mode = mode;
        }

        public void Submit()
        {
            // Submit if only cell value has changed
            if (this.OnSubmit != null
                && isFocusing
                && this.defautValue != this.input.value)
            {
                isFocusing = false;
                this.OnSubmit(this.input.value);
            }

            this.Hide();
            this.SetAllowAutoFocus(true);
            this.mode = EditorActiveMode.None;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            isFromPaste = evt.keyCode == KeyCode.V && (evt.commandKey || evt.ctrlKey);
            
            if (Input.compositionString.Length > 0)
            {
                return;
            }

            if (evt.keyCode == KeyCode.None)
            {
                return;
            }

            // Allow Alt+Enter to insert break line
            if (evt.altKey && (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return))
            {
                isBreakLine = true;

#if UNITY_EDITOR_WIN
                var index = input.cursorIndex;
                var newValue = input.value.Insert(index, "\n");
                input.SetValueWithoutNotify(newValue);
                input.SelectRange(index + 1, index + 1);
#endif
                return;
            }


            if (OnKeyDownEvent != null)
            {
                OnKeyDownEvent(evt);
            }
        }

        public void SetValueWithFireValueChanged(string value)
        {
            this.input.SetValueWithoutNotify(value);
        }

        public void SetRect(CellRect rect)
        {
            this.SetOffset(new Offset(rect.x, rect.y));
            this.SetSize(new Size(rect.width, rect.height));
            input.style.height = rect.height - 6;
            input.style.width = 0;
            this.defautWidth = rect.width - 2;
        }

        public void SetCell(Cell cell)
        {
            SetAllowAutoFocus(false);
            this.input.multiline = true;
            
            this.cell = cell;
            var value = cell == null ? string.Empty : cell.GetRawValue();
            SetInputWidthByValue(value);
            this.input.SetValueWithoutNotify(value);
            var length = value.Length;

            switch (mode)
            {
                case EditorActiveMode.Mouse:
                    this.defautValue = value;
                    break;
                case EditorActiveMode.Functions:
                    length --;
                    break;
            }

            this.schedule.Execute(() =>
            {
                isFocusing = true;

                if (this.OnFocused != null)
                {
                    this.OnFocused();
                }

                this.input.Focus();
                this.input.SelectRange(length, length);

            }).ExecuteLater(60);
        }

        public void SetAllowAutoFocus(bool allow)
        {
            allowAutoFocus = allow;
        }

        private void ForceFocus()
        {
            this.schedule.Execute(() =>
            {
                if (allowAutoFocus && AutoFocus && this.input.focusController.focusedElement != this.input)
                {
                    this.input.style.width = 0;
                    var ele = this.input.ElementAt(0);
                    if (ele != null)
                    {
                        ele.Focus();
                    }
                }
            }).Every(80);
        }

        public override void Hide()
        {
            this.input.style.width = 0;
            isFocusing = false;
            mode = EditorActiveMode.None;
            this.input.SetValueWithoutNotify(string.Empty);
            this.label.text = string.Empty;
            this.input.multiline = false;
        }
    }
}
