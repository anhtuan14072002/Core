//  Copyright (c) 2020-present amlovey
//  
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace Yade.Editor
{
    public class TopCellEditInput : Container
    {
        private Label cellSelection;
        private TextField cellEditor;

        public Func<string> OnCellSelectionUpdate;
        public Action<string> OnValueUpdate;
        public Action<KeyDownEvent> OnKeyDownEvent;
        public Action OnFocusOut;

        private bool updateValueWithoutFiringEvent;

        private bool isFocusing;
        public bool IsFocusing
        {
            get
            {
                return isFocusing;
            }
        }

        public TopCellEditInput()
        {
            updateValueWithoutFiringEvent = false;

            this.AddToClassList("top-cell-edit-input");
            this.style.bottom = float.NaN;

            float left = Constants.FIXED_INDEX_WIDTH;
            cellSelection = new Label();
            cellSelection.style.unityTextAlign = TextAnchor.MiddleCenter;
            cellSelection.style.maxHeight = 32;
            cellSelection.style.position = Position.Absolute;
            cellSelection.SetEdgeDistance(0, 0, float.NaN, 0);
            cellSelection.style.width = left;
            cellSelection.SetMargin(0);
            cellSelection.SetPadding(0);
            cellSelection.SetBorderColor(ColorHelper.Parse(Theme.Current.TopCellBorder));
            cellSelection.SetBorderWidth(0, 1, 0, 1);
            cellSelection.schedule.Execute(() =>
            {
                if (OnCellSelectionUpdate != null)
                {
                    cellSelection.text = OnCellSelectionUpdate();
                }
            }).Every(100);
            cellSelection.RegisterCallback<MouseDownEvent>(OnCopy);
            this.Add(cellSelection);

            cellEditor = new TextField();
            cellEditor.multiline = true;
            cellEditor.style.position = Position.Absolute;
            cellEditor.SetEdgeDistance(left, 0, 0, 0);
            cellEditor.SetMargin(0);
            cellEditor.SetPadding(8, 8, 6, 6);
            cellEditor.SetBorderColor(ColorHelper.Parse(Theme.Current.TopCellBorder));
            cellEditor.SetBorderWidth(1, 1, 0, 1);
            cellEditor.RegisterValueChangedCallback(OnCellEditorValueUpdate);
            cellEditor.RegisterCallback<FocusOutEvent>(FocusOut);
            cellEditor.RegisterCallback<FocusInEvent>(FocusIn);
            cellEditor.delegatesFocus = true;
            cellEditor.doubleClickSelectsWord = true;
            cellEditor.focusable = true;
            cellEditor.RegisterCallback<KeyDownEvent>(this.OnKeyDown);
            this.Add(cellEditor);

            VisualElement resizer = new VisualElement();
            resizer.AddToClassList("top-cell-editor-input-resizer");
            resizer.style.position = Position.Absolute;
            resizer.style.height = 8;
            resizer.SetEdgeDistance(0, float.NaN, 0, -4);
            this.Add(resizer);
        }

        private void OnCopy(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                if (!string.IsNullOrEmpty(cellSelection.text))
                {
                    Utilities.SetClipboardData(cellSelection.text);
                    Debug.Log("YADE: Cell Index Copied!");
                }
            }
        }

        private void FocusIn(FocusInEvent evt)
        {
            Input.imeCompositionMode = IMECompositionMode.On;
            isFocusing = true;
        }

        private void FocusOut(FocusOutEvent evt)
        {
            Input.imeCompositionMode = IMECompositionMode.Auto;
            isFocusing = false;
            if (OnFocusOut != null)
            {
                OnFocusOut();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            evt.StopImmediatePropagation();

            if (evt.keyCode == KeyCode.None)
            {
                return;
            }

            // Allow Alt+Enter to insert break line
            if (evt.altKey && (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return))
            {
#if UNITY_EDITOR_WIN
                var index = cellEditor.cursorIndex;
                var newValue = cellEditor.value.Insert(index, "\n");
                cellEditor.SetValueWithoutNotify(newValue);
                cellEditor.SelectRange(index + 1, index + 1);
#endif
                return;
            }

            if (OnKeyDownEvent != null)
            {
                OnKeyDownEvent(evt);
            }
        }

        public void SetSelectionValue(string alphaBasedCellIndex)
        {
            this.cellSelection.text = alphaBasedCellIndex;
        }

        public string GetCellIndex()
        {
            return this.cellSelection.text;
        }

        public void SetValue(string value)
        {
            this.cellEditor.value = value;
        }

        public void SetValueWithoutFireEvent(string value)
        {
            this.cellEditor.SetValueWithoutNotify(value);
        }

        private void OnCellEditorValueUpdate(ChangeEvent<string> evt)
        {
            if (OnValueUpdate != null && !updateValueWithoutFiringEvent)
            {
                OnValueUpdate(evt.newValue);
            }
        }

        private void SetHeight(float height)
        {
            this.style.height = height;
        }
    }
}
