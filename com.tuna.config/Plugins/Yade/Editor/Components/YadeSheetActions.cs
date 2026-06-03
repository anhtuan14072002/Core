//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Yade.Runtime;
using System.Linq;
using System.Text;
using Yade.Runtime.CSV;

namespace Yade.Editor
{
    public partial class YadeSheet
    {
        private void RegisterShortcuts()
        {
            // Save yadesheet
            commandRegister.Register((int)KeyMod.Ctrl | (int)KeyCode.S, (int)KeyMod.Cmd | (int)KeyCode.S, () =>
            {
                AssetDatabase.SaveAssets();
            });

            // Enter/Return Key to move down cell
            commandRegister.Register((int)KeyCode.Return, () => this.SelectionMove(SelectorMoveDirection.Down));
            commandRegister.Register((int)KeyCode.KeypadEnter, () => this.SelectionMove(SelectorMoveDirection.Down));

            // Arrow key to move cell
            commandRegister.Register((int)KeyCode.LeftArrow, () => this.SelectionMove(SelectorMoveDirection.Left));

            commandRegister.Register((int)KeyCode.RightArrow, () => this.SelectionMove(SelectorMoveDirection.Right));
            commandRegister.Register((int)KeyCode.UpArrow, () => this.SelectionMove(SelectorMoveDirection.Up));
            commandRegister.Register((int)KeyCode.DownArrow, () => this.SelectionMove(SelectorMoveDirection.Down));

            commandRegister.Register((int)KeyCode.Tab, () => this.SelectionMove(SelectorMoveDirection.Right));
            commandRegister.Register((int)KeyCode.Tab | KeyMod.Shift, () => this.SelectionMove(SelectorMoveDirection.Left));

            // Arrow key + shift to move selection
            commandRegister.Register((int)KeyCode.LeftArrow | KeyMod.Shift, () => this.SelectionUnionMove(SelectorMoveDirection.Left));
            commandRegister.Register((int)KeyCode.RightArrow | KeyMod.Shift, () => this.SelectionUnionMove(SelectorMoveDirection.Right));
            commandRegister.Register((int)KeyCode.UpArrow | KeyMod.Shift, () => this.SelectionUnionMove(SelectorMoveDirection.Up));
            commandRegister.Register((int)KeyCode.DownArrow | KeyMod.Shift, () => this.SelectionUnionMove(SelectorMoveDirection.Down));

            // Delete selected cells
            commandRegister.Register((int)KeyCode.Backspace, this.DeleteSelectedCells);
            commandRegister.Register((int)KeyCode.Delete, this.DeleteSelectedCells);

            // Copy & Cut & Paste
            commandRegister.Register(KeyMod.Ctrl | (int)KeyCode.C, KeyMod.Cmd | (int)KeyCode.C, this.Copy);
            commandRegister.Register(KeyMod.Ctrl | (int)KeyCode.V, KeyMod.Cmd | (int)KeyCode.V, this.Paste);
            commandRegister.Register(KeyMod.Ctrl | (int)KeyCode.X, KeyMod.Cmd | (int)KeyCode.X, this.Cut);

            //ESC
            commandRegister.Register((int)KeyCode.Escape, this.CancelCurrent);

            // Undo/Redo
            commandRegister.Register(KeyMod.Ctrl | (int)KeyCode.Z, KeyMod.Cmd | (int)KeyCode.Z, DoUnDoAction);
            commandRegister.Register(KeyMod.Ctrl | KeyMod.Shift | (int)KeyCode.Z, KeyMod.Cmd | KeyMod.Shift | (int)KeyCode.Z, DoRedoAction);

            // Save Data
            commandRegister.Register(KeyMod.Ctrl | (int)KeyCode.S, KeyMod.Cmd | (int)KeyCode.S, AssetDatabase.SaveAssets);
        }

        private void DoRedoAction()
        {
            if (IsInWindowState())
            {
                return;
            }

            if (this.editor.IsFocusing)
            {
                return;
            }

            Undo.PerformRedo();
        }

        private void DoUnDoAction()
        {
            if (IsInWindowState())
            {
                return;
            }

            if (this.editor.IsFocusing)
            {
                return;
            }

            Undo.PerformUndo();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            var assetPaths = DragAndDrop.objectReferences.Select(asset => AssetDatabase.GetAssetPath(asset)).ToArray();
            var range = this.state.dragAndDropRange;
            if (range == null)
            {
                return;
            }

            bool isPerformAssetsArray = evt.commandKey || evt.ctrlKey;
            if (isPerformAssetsArray)
            {
                // Handle Assets formula
                var paths = assetPaths.Select(p => string.Format("\"{0}\"", p));
                var rawValue = string.Format(@"=ASSETS({0})", string.Join(",", paths));
                var row = range.startRow;
                var column = range.startColumn;
                this.state.SetCellRawValue(row, column, rawValue);
            }
            else
            {
                var column = range.startColumn;
                for (int i = 0; i < assetPaths.Length; i++)
                {
                    var row = range.startRow + i;
                    var rawValue = string.Empty;
                    // If shift key pressed, set cell to asset path,
                    // otherwise, set the reference to the cell
                    if (evt.shiftKey)
                    {
                        rawValue = assetPaths[i];
                    }
                    else
                    {
                        rawValue = string.Format(@"=ASSET(""{0}"")", assetPaths[i]);
                    }

                    this.state.SetCellRawValue(row, column, rawValue);
                }
            }

            this.UpdateContents();
        }

        private void OnDragExit(DragExitedEvent evt)
        {
            dragingSelector.Hide();
            DragAndDrop.visualMode = DragAndDropVisualMode.None;
        }

        private void OnDraging(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;

            var y = evt.mousePosition.y - this.state.fixedHeaderHeight;
            var x = evt.mousePosition.x - this.state.fixedIndexWidth;
            var rowIndex = this.state.GetRowIndexByY(y);
            var columnIndex = this.state.GetColumnIndexByX(x);

            if (rowIndex >= 0 && columnIndex >= 0)
            {
                rowIndex = rowIndex + this.state.scrollRowIndex;
                columnIndex = columnIndex + this.state.scrollColumnIndex;
                var range = new CellRange(rowIndex, rowIndex, columnIndex, columnIndex);
                var rect = this.state.GetCellRectByRange(range);
                this.state.dragAndDropRange = range;
                dragingSelector.SetRect(rect);
                dragingSelector.Show();
                return;
            }

            dragingSelector.Hide();
            this.state.dragAndDropRange = null;
        }

        private string OnTopEditInputSelectionUpdate(bool forceUpdate = false)
        {
            if (this.state.range == null)
            {
                return string.Empty;
            }

            int row = this.state.range.startRow;
            int column = this.state.range.startColumn;

            string alphaBased = IndexHelper.ToAlphaBasedCellIndex(row, column);
            if (topCellEditInput.GetCellIndex() != alphaBased || forceUpdate)
            {
                var cell = this.state.GetCell(row, column);
                topCellEditInput.SetValue(cell == null ? string.Empty : cell.GetRawValue());
            }

            topCellEditInput.SetEnabled(!this.state.selectedRange.HasMultipleCells());

            return alphaBased;
        }

        private void OnTopEditInputFocusOut()
        {
            this.UpdateContents();
        }

        private void OnTopEditInputValueUpdate(string value)
        {
            // If on selection or editor is focusing, we don't update cell update
            if (this.state.selectedRange == null
                || this.state.selectedRange.HasMultipleCells()
                || !this.topCellEditInput.IsFocusing)
            {
                return;
            }

            int row = this.state.selectedRange.startRow;
            int column = this.state.selectedRange.startColumn;
            this.state.SetCellRawValue(row, column, value);
            this.editor.SetValueWithFireValueChanged(value);

            //Only update current focusing cell
            this.UpdateSelectCellContent(this.state.range);
        }

        private void DeletedRowsOrColumnsFromFixedHeaders()
        {
            var range = this.state.selectedRange;
            if (range.startRow < 0)
            {
                this.DeleteSelectedColumns();
            }
            else if (range.startColumn < 0)
            {
                this.DeleteSelectedRows();
            }
        }

        private void AddRowsOrColumnsFromFixedHeaders()
        {
            var range = this.state.selectedRange;
            if (range.startRow < 0)
            {
                var count = range.endColumn - range.startColumn + 1;
                this.AddColumn(count);
                this.state.columnCount++;
            }
            else if (range.startColumn < 0)
            {
                var count = range.endRow - range.startRow + 1;
                this.AddRow(count);
                this.state.rowCount++;
            }
        }

        private void DeleteSelectedCells()
        {
            this.state.RecordUndo("Delete Selected Cells");
            this.state.selectedRange.ForEach((row, column) => this.state.DeleteCell(row, column));
            this.state.extraSelectedRanges.ForEach(range =>
            {
                this.state.DeleteCell(range.startRow, range.startColumn);
            });

            this.UpdateContents();
            this.topCellEditInput.SetValueWithoutFireEvent("");
        }

        private void DeleteSelectedRows()
        {
            this.state.RecordUndo("Delete Selected Rows");
            HashSet<int> indexes = new HashSet<int>();
            this.state.selectedRange.ForEach((row, _) => indexes.Add(row));
            this.state.extraSelectedRanges.ForEach(range => indexes.Add(range.startRow));
            this.state.DeleteRows(indexes.ToArray());
            this.state.SetRowCount(this.state.rowCount - indexes.Count);
            this.UpdateTableUI();
        }

        private void DeleteSelectedColumns()
        {
            this.state.RecordUndo("Delete Selected Columns");
            HashSet<int> indexes = new HashSet<int>();
            this.state.selectedRange.ForEach((_, column) => indexes.Add(column));
            this.state.extraSelectedRanges.ForEach(range => indexes.Add(range.startColumn));
            this.state.DeleteColumns(indexes.ToArray());
            this.state.SetColumnCount(this.state.columnCount - indexes.Count);
            this.UpdateTableUI();
        }

        private void AddRow(int delta = 1)
        {
            if (this.state.extraSelectedRanges.Count > 0)
            {
                EditorUtility.DisplayDialog("", "Not support actions", "ok");
                return;
            }

            this.state.RecordUndo("Add Row");
            this.state.AddRow(this.state.range.startRow, delta);
            this.UpdateTableUI();
        }

        private void AddColumn(int delta = 1)
        {
            if (this.state.extraSelectedRanges.Count > 0)
            {
                EditorUtility.DisplayDialog("", "Not support actions", "ok");
                return;
            }

            this.state.RecordUndo("AddColumn");
            this.state.AddColumn(this.state.range.startColumn, delta);
            this.UpdateTableUI();
        }

        private void OnResized(GeometryChangedEvent evt)
        {
            this.UpdateTableUI();
        }

        private void OnEditorValueUpdate(string value)
        {
            // Update the top cell edit input value when in editing
            if (this.editor.IsFocusing)
            {
                this.topCellEditInput.SetValue(value);
            }
        }

        private void OnEditorFocused()
        {
            // Don't render label of the selected cell 
            this.UpdateContents(this.state.selectedRange);
        }

        private void OnEditorSubmit(string value)
        {
            int row = this.state.selectedRange.startRow;
            int column = this.state.selectedRange.startColumn;

            if (string.IsNullOrEmpty(value))
            {
                this.state.RecordUndo("editor submit");
                this.state.DeleteCell(row, column);
                return;
            }

            this.state.RecordUndo("editor submit");
            this.state.SetCellRawValue(row, column, value);
            this.UpdateContents();

            this.CancelCopyAndCut();
        }

        private void OnTopEditorKeyDown(KeyDownEvent evt)
        {
            evt.StopImmediatePropagation();

            if (evt.keyCode == KeyCode.Return)
            {
                this.editor.Submit();
                this.SelectionMove(SelectorMoveDirection.Down);
            }

            if (evt.keyCode == KeyCode.Tab && evt.shiftKey)
            {
                this.editor.Submit();
                this.SelectionMove(SelectorMoveDirection.Left);
            }

            // Only works for editor that focus from keyborad
            if (this.editor.Mode == EditorActiveMode.Keyboard)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.DownArrow:
                        this.editor.Submit();
                        this.SelectionMove(SelectorMoveDirection.Down);
                        break;
                    case KeyCode.RightArrow:
                    case KeyCode.Tab:
                        this.editor.Submit();
                        this.SelectionMove(SelectorMoveDirection.Right);
                        break;
                    case KeyCode.UpArrow:
                        this.editor.Submit();
                        this.SelectionMove(SelectorMoveDirection.Up);
                        break;
                    case KeyCode.LeftArrow:
                        this.editor.Submit();
                        this.SelectionMove(SelectorMoveDirection.Left);
                        break;
                }
            }
        }

        /// <summary>
        /// Key down event callback for cell editor
        /// </summary>
        /// <param name="evt">Keydown event</param>
        private void OnEditorKeyDown(KeyDownEvent evt)
        {
            evt.StopImmediatePropagation();

            if (this.state.selectedRange.HasMultipleCells())
            {
                return;
            }

            // Only works for editor that focus from keyborad
            if (this.editor.Mode == EditorActiveMode.Mouse
                || this.editor.Mode == EditorActiveMode.Functions)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        this.editor.Submit();
                        this.UpdateSelectCellContent(this.state.selectedRange);
                        break;
                }
                return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                case KeyCode.RightArrow:
                case KeyCode.Tab:
                case KeyCode.UpArrow:
                case KeyCode.LeftArrow:
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    this.editor.Submit();
                    break;
            }
        }

        /// <summary>
        /// Key down event for sheet, it does not handle editor key down event
        /// </summary>
        /// <param name="evt">Keydown event</param>
        private void OnKeyDown(KeyDownEvent evt)
        {
            // Don't handle key event if in column alias edit mode
            if (this.state.showColumnEditor)
            {
                return;
            }

            // Stop propagation will mute keyboard press audio. And we need to 
            // skip the undo/redo shortcut. Otherwise, Undo/Redo will not work.
            if (!(evt.keyCode == KeyCode.Z && (evt.commandKey || evt.ctrlKey)))
            {
                evt.StopPropagation();
            }

            // Only works for editor that focus from keyborad
            if (this.editor.Mode == EditorActiveMode.Mouse
                || this.editor.Mode == EditorActiveMode.Functions)
            {
                return;
            }

            // If match the registered command, excute the command.
            commandRegister.Execute(evt);
        }

        private void OnWheel(WheelEvent evt)
        {
            if (IsInWindowState())
            {
                return;
            }

            this.HandleScroll(evt.delta);
        }

        private void HandleScroll(Vector3 delta)
        {
            var dx = delta.x;
            var dy = delta.y;
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                if (dx < 0)
                {
                    this.scrollView.horizontalScroller.ScrollPageUp(-dx);
                }
                else
                {
                    this.scrollView.horizontalScroller.ScrollPageDown(dx);
                }
            }
            else
            {
                if (dy < 0)
                {
                    this.scrollView.verticalScroller.ScrollPageUp(-dy);
                }
                else
                {
                    this.scrollView.verticalScroller.ScrollPageDown(dy);
                }
            }

            this.state.scrollLeft = this.scrollView.horizontalScroller.value;
            this.state.scrollTop = this.scrollView.verticalScroller.value;
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (isTopCellEditInputResizing)
            {
                var height = this.state.fixedHeaderHeight + evt.mouseDelta.y;
                var minalHeight = this.state.GetMinimalFixedHeaderHeight();
                height = Mathf.Clamp(height, minalHeight, minalHeight + 100);
                this.state.fixedHeaderHeight = height;
                var inputHeight = height - Row.DEFAULT_HEIGHT - this.topCellEditInput.resolvedStyle.top - this.state.GetExtraHeaderHeight();
                topCellEditInput.style.height = inputHeight;
                this.UpdateLayersTop();
                this.UpdateTableUI();
                return;
            }

            if (HandleCellResizer(evt))
            {
                return;
            }

            if (IsInWindowState())
            {
                return;
            }

            if (IsTargetStatusBar(evt.target as VisualElement))
            {
                return;
            }

            if ((evt.pressedButtons & 0x01) == 1)
            {
                if (dragingAutoFillElement)
                {
                    this.DoAutoFillMouseMove(evt);
                    return;
                }

                var y = evt.mousePosition.y - this.state.fixedHeaderHeight;
                var x = evt.mousePosition.x - this.state.fixedIndexWidth;
                var rowIndex = this.state.GetRowIndexByY(y);
                var columnIndex = this.state.GetColumnIndexByX(x);

                if (rowIndex >= 0 && columnIndex >= 0)
                {
                    rowIndex = rowIndex + this.state.scrollRowIndex;
                    columnIndex = columnIndex + this.state.scrollColumnIndex;
                    var range = new CellRange(rowIndex, rowIndex, columnIndex, columnIndex);
                    var newRange = range.Union(this.state.range);
                    var rect = this.state.GetCellRectByRange(newRange);
                    this.selector.SetRect(rect);
                    this.state.selectedRange = newRange;
                    this.UpdateHeaders(this.GetViewRange());
                }
            }
        }

        private bool HandleCellResizer(MouseMoveEvent evt)
        {
            var y = evt.mousePosition.y - this.state.fixedHeaderHeight;
            var x = evt.mousePosition.x - this.state.fixedIndexWidth;
            var rowIndex = this.state.GetRowIndexByY(y);
            var columnIndex = this.state.GetColumnIndexByX(x);

            if (isResizing)
            {
                if (resizingRow <= -1 && resizingColumn >= 0)
                {
                    var width = this.state.GetColumnWidth(resizingColumn) + evt.mouseDelta.x;
                    if (width <= 50)
                    {
                        width = 50;
                    }
                    this.state.SetColumnWidth(resizingColumn, width);
                    this.UpdateTableUI();
                }

                if (resizingColumn == -1 && resizingRow >= 0)
                {
                    var height = this.state.GetRowHeight(resizingRow) + evt.mouseDelta.y;
                    if (height <= 25)
                    {
                        height = 25;
                    }
                    this.state.SetRowHeight(resizingRow, height);
                    this.UpdateTableUI();
                }

                rowIndex = this.resizingRow;
                columnIndex = this.resizingColumn;
            }

            var isHoverOnResizer = this.IsTargetResizer(evt.target as VisualElement);
            if (isHoverOnResizer)
            {
                return true;
            }

            if (rowIndex <= -1 && columnIndex >= 0)
            {
                float offsetX = this.state.GetColumnTotalWidth(this.state.scrollColumnIndex, columnIndex + this.state.scrollColumnIndex) + this.state.fixedIndexWidth;
                var columnWidth = this.state.GetColumnWidth(columnIndex);
                if (columnIndex > 0 && x + this.state.fixedIndexWidth < offsetX - columnWidth / 2)
                {
                    offsetX -= columnWidth;
                    columnIndex--;
                }

                var rect = new CellRect(offsetX - 6, 0, 12, Row.DEFAULT_HEIGHT + this.state.GetExtraHeaderHeight());
                this.verticalResizer.SetRect(rect);
                this.verticalResizer.name = columnIndex.ToString();
                this.verticalResizer.Show();
                this.horizontalResizer.Hide();
                return true;
            }

            if (columnIndex == -1 && rowIndex >= 0)
            {
                float offsetY = this.state.GetExtraHeaderHeight() + this.state.GetRowTotalHeight(this.state.scrollRowIndex, rowIndex + this.state.scrollRowIndex) + Row.DEFAULT_HEIGHT;
                var rowHeight = this.state.GetRowHeight(rowIndex);

                if (rowIndex > 0 && y + this.state.GetExtraHeaderHeight() < offsetY - rowHeight / 2)
                {
                    offsetY -= rowHeight;
                    rowIndex--;
                }

                var rect = new CellRect(0, offsetY - 6, this.state.fixedIndexWidth, 12);
                this.horizontalResizer.SetRect(rect);
                this.horizontalResizer.Show();
                this.horizontalResizer.name = rowIndex.ToString();
                this.verticalResizer.Hide();
                return true;
            }

            return false;
        }

        private bool IsTargetResizer(VisualElement currentElement)
        {
            if (currentElement == null)
            {
                return false;
            }

            return currentElement.ClassListContains("resizer-horizontal")
                || currentElement.ClassListContains("resizer-vertical");
        }

        private bool IsTargetTopCellEditInputResizer(VisualElement currentElement)
        {
            if (currentElement == null)
            {
                return false;
            }

            return currentElement.ClassListContains("top-cell-editor-input-resizer");
        }

        private bool IsTargetStatusBar(VisualElement currentElement)
        {
            if (currentElement == null)
            {
                return false;
            }

            return currentElement.ClassListContains("statusbar");
        }

        private void DoAutoFillMouseMove(MouseMoveEvent evt)
        {
            var offsetY = evt.mousePosition.y - this.state.fixedHeaderHeight;
            var offsetX = evt.mousePosition.x - this.state.fixedIndexWidth;
            CellRange selectedRange = this.state.selectedRange;

            var sRect = this.state.GetCellRectByRange(selectedRange);
            if (sRect.IsXYInRect(offsetX, offsetY - Row.DEFAULT_HEIGHT))
            {
                this.state.autoFillRange = null;
                this.autoFillSelector.Hide();
                return;
            }

            int rowIndex = this.state.GetRowIndexByY(offsetY);
            int columnIndex = this.state.GetColumnIndexByX(offsetX);

            int startRow = rowIndex + this.state.scrollRowIndex;
            int startColumn = columnIndex + this.state.scrollColumnIndex;

            startRow = Mathf.Clamp(startRow, 0, this.state.rowCount - 1);
            startColumn = Mathf.Clamp(startColumn, 0, this.state.columnCount - 1);

            CellRange range = new CellRange(startRow, startRow, startColumn, startColumn);
            CellRange newRange = this.state.range.Union(range);

            // Center of drag rect
            float spanX = offsetX - sRect.x - sRect.width / 2;
            float spanY = offsetY - sRect.y - sRect.height / 2;

            // correct the delta of X and Y
            float xDelta = spanX < 0 ? offsetX - sRect.x : offsetX - sRect.x - sRect.width;
            float yDelta = spanY < 0 ? offsetY - sRect.y : offsetY - sRect.y - sRect.height;

            if (Mathf.Abs(spanX) < sRect.width / 2)
            {
                newRange.startColumn = selectedRange.startColumn;
                newRange.endColumn = selectedRange.endColumn;
                if (spanY < 0)
                {
                    newRange.endRow = selectedRange.endRow;
                }
                else
                {
                    newRange.startRow = selectedRange.startRow;
                }
            }
            else if (Mathf.Abs(spanY) < sRect.height / 2)
            {
                newRange.startRow = selectedRange.startRow;
                newRange.endRow = selectedRange.endRow;
                if (spanX < 0)
                {
                    newRange.endColumn = selectedRange.endColumn;
                }
                else
                {
                    newRange.startColumn = selectedRange.startColumn;
                }
            }
            else if (Mathf.Abs(xDelta) > Mathf.Abs(yDelta))
            {
                newRange.startRow = selectedRange.startRow;
                newRange.endRow = selectedRange.endRow;
                if (xDelta < 0)
                {
                    newRange.endColumn = selectedRange.endColumn;
                }
                else
                {
                    newRange.startColumn = selectedRange.startColumn;
                }
            }
            else if (Mathf.Abs(xDelta) <= Mathf.Abs(yDelta))
            {
                newRange.startColumn = selectedRange.startColumn;
                newRange.endColumn = selectedRange.endColumn;
                if (yDelta < 0)
                {
                    newRange.endRow = selectedRange.endRow;
                }
                else
                {
                    newRange.startRow = selectedRange.startRow;
                }
            }

            this.state.autoFillRange = newRange;
            var rect = this.state.GetCellRectByRange(newRange);
            this.autoFillSelector.SetRect(rect);
            this.autoFillSelector.Show();
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (IsInWindowState())
            {
                return;
            }

            if (isTopCellEditInputResizing)
            {
                isTopCellEditInputResizing = false;
                return;
            }

            if (isResizing)
            {
                isResizing = false;
                resizingColumn = -1;
                resizingRow = -1;
                this.horizontalResizer.Hide();
                this.verticalResizer.Hide();
                return;
            }

            if (dragingAutoFillElement && this.state.autoFillRange != null && this.state.autoFillRange.HasMultipleCells())
            {
                this.state.RecordUndo("Do auto fill");
                // Do fill actions
                this.state.AutoFillDataFromRangeToRange(this.state.selectedRange, this.state.autoFillRange, false, !evt.shiftKey);
                this.UpdateContents();

                // Reset auto fill state
                CellRange newRange = this.state.selectedRange.Union(this.state.autoFillRange);
                var rect = this.state.GetCellRectByRange(newRange);
                this.selector.SetRect(rect);
                this.state.selectedRange = newRange;
                dragingAutoFillElement = false;
                this.autoFillSelector.Hide();
                this.state.autoFillRange = null;
                this.UpdateHeaders(this.GetViewRange());
            }
        }

        private void OnMousedDown(MouseDownEvent evt)
        {
            if (IsInWindowState())
            {
                return;
            }

            if (isResizing || isTopCellEditInputResizing)
            {
                return;
            }

            // If left mouse button click
            if (evt.button == 0)
            {
                this.state.RecordUndo("OnMouseDown");

                var currentElement = evt.target as VisualElement;
                var isAutoFillElement = currentElement.ClassListContains("yade-selector-auto-fill");

                // Just return if click on autofill element
                if (isAutoFillElement)
                {
                    if (this.editor.IsFocusing)
                    {
                        this.editor.Submit();
                    }

                    dragingAutoFillElement = true;
                    return;
                }

                // If click on dropdowntext cell
                foreach (var item in dropdownLabelPools.Usings)
                {
                    if (item.IsArrowClicked(evt.mousePosition))
                    {
                        item.ShowDropdown();
                        return;
                    }
                }

                // Just reutrn if click on top cell edit input resizer
                if (IsTargetTopCellEditInputResizer(currentElement))
                {
                    isTopCellEditInputResizing = true;
                    return;
                }

                // Just return if click on status bar
                if (IsTargetStatusBar(currentElement))
                {
                    return;
                }

                var y = evt.mousePosition.y - this.state.fixedHeaderHeight;
                var x = evt.mousePosition.x - this.state.fixedIndexWidth;

                var rowIndex = this.state.GetRowIndexByY(y);
                var columnIndex = this.state.GetColumnIndexByX(x);
                var range = new CellRange(rowIndex, rowIndex, columnIndex, columnIndex);

                bool isResizerElement = IsTargetResizer(currentElement);
                if (isResizerElement)
                {
                    if (rowIndex <= -1 && columnIndex >= 0)
                    {
                        isResizing = true;
                        int column;
                        if (int.TryParse(this.verticalResizer.name, out column))
                        {
                            resizingColumn = this.state.scrollColumnIndex + column;
                        }

                        resizingRow = -1;
                        return;
                    }

                    if (columnIndex == -1 && rowIndex >= 0)
                    {
                        isResizing = true;
                        int row;
                        if (int.TryParse(this.horizontalResizer.name, out row))
                        {
                            resizingRow = this.state.scrollRowIndex + row;
                        }
                        resizingColumn = -1;
                        return;
                    }
                }

                if (y < -this.state.GetExtraHeaderHeight() || columnIndex <= -2)
                {
                    return;
                }

                if (rowIndex == -1 && columnIndex == -1)
                {
                    range.startRow = -1;
                    range.endRow = this.state.rowCount - 1;
                    range.startColumn = -1;
                    range.endColumn = this.state.columnCount - 1;
                }
                else if (rowIndex <= -1 && columnIndex >= 0)
                {
                    range.startRow = -1;
                    range.endRow = this.state.rowCount - 1;
                    range.startColumn = columnIndex + this.state.scrollColumnIndex;
                    range.endColumn = range.startColumn;
                }
                else if (rowIndex >= 0 && columnIndex == -1)
                {
                    range.startColumn = -1;
                    range.endColumn = this.state.columnCount - 1;
                    range.startRow = rowIndex + this.state.scrollRowIndex;
                    range.endRow = range.startRow;
                }

                if (rowIndex >= 0 && columnIndex >= 0)
                {
                    rowIndex = rowIndex + this.state.scrollRowIndex;
                    columnIndex = columnIndex + this.state.scrollColumnIndex;
                    range = new CellRange(rowIndex, rowIndex, columnIndex, columnIndex);
                }

                if (evt.shiftKey)
                {
                    var newRange = range.Union(this.state.range);
                    var rect = this.state.GetCellRectByRange(newRange);
                    this.state.selectedRange = newRange;
                    this.selector.SetRect(rect);
                    this.UpdateHeaders(this.GetViewRange());
                }
                else
                {
                    if (!range.Equals(this.state.selectedRange))
                    {
                        if (evt.ctrlKey || evt.commandKey)
                        {
                            if (HasExtraSelectorInRange(range))
                            {
                                this.state.extraSelectedRanges.RemoveAll(r => r.Equals(range));
                                this.UpdateSelectorLayout(GetViewRange());
                            }
                            else
                            {
                                this.AddNewSelector(this.state.range);
                                this.selector.HideHandle();
                            }
                        }
                        else
                        {
                            this.ClearExtraSelectors();
                            this.selector.ShowHandle();
                        }

                        var rect = this.state.GetCellRectByRange(range);

                        if (this.editor.IsFocusing)
                        {
                            this.editor.Submit();
                            this.UpdateSelectCellContent(this.state.range);
                        }
                        else
                        {
                            this.editor.Hide();
                        }

                        this.state.selectedRange = range;
                        this.state.range = range;
                        this.selector.SetRect(rect);
                        this.UpdateHeaders(this.GetViewRange());
                    }

                    // If double click
                    if (evt.clickCount == 2 && rowIndex >= 0 && columnIndex >= 0)
                    {
                        var rect = this.state.GetCellRectByRange(range);
                        this.editor.SetRect(rect);
                        var cell = this.state.GetCell(range.startRow, range.startColumn);
                        this.editor.SetActiveMode(EditorActiveMode.Mouse);
                        this.editor.SetCell(cell);
                        this.CancelCopyAndCut();
                    }
                }
            }
        }

        private bool HasExtraSelectorInRange(CellRange range)
        {
            bool hasRange = false;
            for (int i = 0; i < this.state.extraSelectedRanges.Count; i++)
            {
                if (this.state.extraSelectedRanges[i].Equals(range))
                {
                    hasRange = true;
                    break;
                }
            }

            return hasRange;
        }

        private ViewRange GetViewRange()
        {
            var clientWidth = this.layout.width;
            var clientHeight = this.layout.height;

            int startRow = this.state.scrollRowIndex;
            int endRow = this.state.rowCount - 1;
            int startColumn = this.state.scrollColumnIndex;
            int endColumn = this.state.columnCount - 1;

            float height = this.state.fixedHeaderHeight;
            for (int index = startRow; index < this.state.rowCount; index++)
            {
                height += this.state.GetRowHeight(index);
                endRow = index;
                if (height > clientHeight)
                {
                    break;
                }
            }

            float width = this.state.fixedIndexWidth;
            for (int index = startColumn; index < this.state.columnCount; index++)
            {
                width += this.state.GetColumnWidth(index);
                endColumn = index;
                if (width > clientWidth)
                {
                    break;
                }
            }

            return new ViewRange(startRow, endRow, startColumn, endColumn, width, height);
        }

        private void OnHorizontalScroll(float value)
        {
            var normalizedOffset = (this.scrollView.scrollOffset.x) / this.scrollView.horizontalScroller.slider.highValue;
            if (normalizedOffset > 0.98)
            {
                bool selectAll = this.state.selectedRange.endColumn == this.state.columnCount - 1;
                this.state.columnCount += 1;
                this.UpdateScrollViewSize();

                // If in select all mode, we need update selected cells too
                if (selectAll)
                {
                    this.state.selectedRange.endColumn = this.state.columnCount - 1;
                    var rect = this.state.GetCellRectByRange(this.state.selectedRange);
                    this.selector.SetRect(rect);
                    this.UpdateFixedTopHeaders(this.GetViewRange());
                }
            }

            int columnIndex = 0;
            float totalWidth = 0;
            for (; columnIndex < this.state.columnCount; columnIndex++)
            {
                if (totalWidth > value + this.state.fixedIndexWidth)
                {
                    break;
                }
                totalWidth += this.state.GetColumnWidth(columnIndex);
            }
            this.state.scrollColumnIndex = columnIndex - 1;
            this.state.scrollLeft = this.scrollView.horizontalScroller.value;
            this.UpdateTableUI();
        }

        private void OnVerticalScroll(float value)
        {
            var normalizedOffset = this.scrollView.scrollOffset.y / this.scrollView.verticalScroller.slider.highValue;
            if (normalizedOffset > 0.98)
            {
                bool selectAll = this.state.selectedRange.endRow == this.state.rowCount - 1;
                this.state.rowCount += 1;
                this.UpdateScrollViewSize();

                // If in select all mode, we need update selected cells too
                if (selectAll)
                {
                    this.state.selectedRange.endRow = this.state.rowCount - 1;
                    var rect = this.state.GetCellRectByRange(this.state.selectedRange);
                    this.selector.SetRect(rect);
                    this.UpdateFixedTopHeaders(this.GetViewRange());
                }
            }

            int rowIndex = 0;
            float totalHeight = 0;

            for (; rowIndex < this.state.rowCount; rowIndex++)
            {
                if (totalHeight > value + 16)
                {
                    break;
                }

                totalHeight += this.state.GetRowHeight(rowIndex);
            }

            this.state.scrollTop = this.scrollView.verticalScroller.value;

            if (this.state.scrollRowIndex != rowIndex - 1)
            {
                this.state.scrollRowIndex = rowIndex - 1;
                this.UpdateTableUI();
            }
        }

        private void SelectionUnionMove(SelectorMoveDirection direction)
        {
            // Disable in window mode
            if (IsInWindowState())
            {
                return;
            }

            // Disable in mouse active and function active mode
            if (this.editor.Mode == EditorActiveMode.Mouse
                || this.editor.Mode == EditorActiveMode.Functions)
            {
                return;
            }

            var range = this.state.range;
            var selectedRange = this.state.selectedRange;
            int startRow = selectedRange.startRow;
            int endRow = selectedRange.endRow;
            int startColumn = selectedRange.startColumn;
            int endColumn = selectedRange.endColumn;

            switch (direction)
            {
                case SelectorMoveDirection.Left:
                    if (endColumn > range.startColumn)
                    {
                        endColumn--;
                    }
                    else
                    {
                        startColumn = startColumn > 0 ? startColumn - 1 : 0;
                    }
                    break;
                case SelectorMoveDirection.Right:
                    if (startColumn < range.startColumn)
                    {
                        startColumn++;
                    }
                    else
                    {
                        endColumn = endColumn < this.state.columnCount - 1 ? endColumn + 1 : this.state.columnCount - 1;
                    }
                    break;
                case SelectorMoveDirection.Up:
                    if (endRow > range.endRow)
                    {
                        endRow--;
                    }
                    else
                    {
                        startRow = startRow > 0 ? startRow - 1 : 0;
                    }
                    break;
                case SelectorMoveDirection.Down:
                    if (startRow < range.startRow)
                    {
                        startRow++;
                    }
                    else
                    {
                        endRow = endRow < this.state.rowCount - 1 ? endRow + 1 : this.state.rowCount - 1;
                    }
                    break;
            }

            var newRange = new CellRange(startRow, endRow, startColumn, endColumn);
            selectedRange = newRange.Union(range);
            this.state.selectedRange = selectedRange;
            var rect = this.state.GetCellRectByRange(selectedRange);
            this.selector.SetRect(rect);
            this.UpdateHeaders(this.GetViewRange());
        }

        private void SelectionMove(SelectorMoveDirection direction)
        {
            // Disable in window mode
            if (IsInWindowState())
            {
                return;
            }

            // Disable in mouse active and function active mode
            if (this.editor.Mode == EditorActiveMode.Mouse
                || this.editor.Mode == EditorActiveMode.Functions)
            {
                return;
            }

            if (this.state.extraSelectedRanges.Count > 0)
            {
                this.ClearExtraSelectors();
                this.selector.ShowHandle();
            }

            var range = this.state.selectedRange;
            int startRow = range.startRow;
            int endRow = range.endRow;
            int startColumn = range.startColumn;
            int endColumn = range.endColumn;

            switch (direction)
            {
                case SelectorMoveDirection.Left:
                    if (startColumn > 0)
                    {
                        startColumn--;
                        endColumn--;
                    }
                    break;
                case SelectorMoveDirection.Right:
                    if (endColumn < this.state.columnCount - 1)
                    {
                        startColumn++;
                        endColumn++;
                    }
                    break;
                case SelectorMoveDirection.Up:
                    if (startRow > 0)
                    {
                        startRow--;
                        endRow--;
                    }
                    break;
                case SelectorMoveDirection.Down:
                    if (endRow < this.state.rowCount - 1)
                    {
                        startRow++;
                        endRow++;
                    }
                    break;
            }

            var newRange = new CellRange(startRow, endRow, startColumn, endColumn);
            var rect = this.state.GetCellRectByRange(newRange);
            this.state.selectedRange = newRange;
            this.state.range = new CellRange(startRow, startRow, startColumn, startColumn);
            this.selector.SetRect(rect);

            if (!newRange.HasMultipleCells())
            {
                this.editor.SetRect(rect);
                this.editor.Hide();
            }

            this.UpdateHeaders(this.GetViewRange());
            this.MoveScrollbarBaseOnSelectionMove(rect, direction);
            this.Focus();
        }

        private void MoveScrollbarBaseOnSelectionMove(CellRect selectedRect, SelectorMoveDirection direction)
        {
            var tableWidth = this.layout.width - this.state.fixedIndexWidth;
            var tableHeight = this.layout.height - this.state.fixedHeaderHeight;

            switch (direction)
            {
                case SelectorMoveDirection.Left:
                    if (selectedRect.x < 0)
                    {
                        var left = this.state.scrollLeft + selectedRect.x;
                        if (left < 0)
                        {
                            left = 0;
                        }

                        this.scrollView.horizontalScroller.value = left;
                        this.state.scrollLeft = left;
                    }
                    break;
                case SelectorMoveDirection.Right:
                    if (selectedRect.x + selectedRect.width > tableWidth)
                    {
                        var deltaWidth = this.state.GetColumnWidth(this.state.selectedRange.endColumn);
                        var left = this.state.scrollLeft + deltaWidth;
                        this.scrollView.horizontalScroller.value = left;
                        this.state.scrollLeft = left;
                    }
                    break;
                case SelectorMoveDirection.Up:
                    if (selectedRect.y < 0)
                    {
                        var top = this.state.scrollTop + selectedRect.y;
                        if (top < 0)
                        {
                            top = 0;
                        }
                        this.scrollView.verticalScroller.value = top;
                        this.state.scrollTop = top;
                    }
                    break;
                case SelectorMoveDirection.Down:
                    if (selectedRect.y + selectedRect.height > tableHeight)
                    {
                        var deltaHeight = this.state.GetRowHeight(this.state.selectedRange.endRow);
                        var top = this.state.scrollTop + deltaHeight;
                        this.scrollView.verticalScroller.value = top;
                        this.state.scrollTop = top;
                    }
                    break;
            }
        }

        private void PingSelectedInProjectWindow()
        {
            List<Object> objects = new List<Object>();
            this.state.selectedRange.ForEach((row, column) =>
            {
                var cell = this.state.GetCell(row, column);
                if (cell != null)
                {
                    if (!cell.HasUnityObject())
                    {
                        return;
                    }

                    var unityObject = cell.GetUnityObject();
                    if (unityObject != null)
                    {
                        objects.Add(cell.GetUnityObject());
                    }
                    else
                    {
                        foreach (var item in cell.GetUnityObjects())
                        {
                            if (item)
                            {
                                objects.Add(item);
                            }
                        }
                    }
                }
            });

            if (objects.Count() == 0)
            {
                EditorGUIUtility.PingObject(objects[0]);
            }
            else
            {
                Selection.objects = objects.ToArray();
            }
        }

        private void SetCellStateToCopied()
        {
            this.ClearClipboard();
            var range = this.state.selectedRange;
            this.state.copyFromRange = range;
            var rect = this.state.GetCellRectByRange(range);
            this.copyOrCutFromSelector.SetRect(rect);
            this.copyOrCutFromSelector.Show();
        }

        private void CopyRawDataToClipboard(CellRange range)
        {
            // Copy data to clipboard
            StringBuilder sb = new StringBuilder();
            for (int i = range.startRow; i <= range.endRow; i++)
            {
                if (i != range.startRow)
                {
                    sb.Append("\r\n");
                }

                for (int j = range.startColumn; j <= range.endColumn; j++)
                {
                    if (j != range.startColumn)
                    {
                        sb.Append("\t");
                    }

                    var cell = this.state.GetCell(i, j);
                    if (cell != null)
                    {
                        var rawValue = cell.GetRawValue();
                        if (rawValue.Contains("\r") || rawValue.Contains("\n") || rawValue.Contains("\t") || rawValue.Contains("\""))
                        {
                            sb.Append(string.Format("\"{0}\"", rawValue));
                        }
                        else
                        {
                            sb.Append(cell.GetRawValue());
                        }
                    }
                }
            }

            Utilities.SetClipboardData(sb.ToString());
        }

        private void Copy()
        {
            this.SetCellStateToCopied();
            this.state.copyFromRange = this.state.selectedRange;
            this.CopyRawDataToClipboard(this.state.selectedRange);
            this.state.isCut = false;
        }

        private void Cut()
        {
            this.SetCellStateToCopied();
            this.state.isCut = true;
        }

        private void ClearClipboard()
        {
            Utilities.SetClipboardData("\n");
        }

        private void Paste()
        {
            if (editor.IsFocusing)
            {
                return;
            }

            // Try copy from clipboard
            var clipboardData = Utilities.GetClipboardData();
            if (!string.IsNullOrWhiteSpace(clipboardData))
            {
                this.state.RecordUndo("Paste");
                YadeCSVReader reader = new YadeCSVReader(clipboardData, false, '\t');
                YadeCSVCell cell;
                while ((cell = reader.Read()) != null)
                {
                    var rowIdx = this.state.range.startRow + cell.Row;
                    var colIdx = this.state.range.startColumn + cell.Column;
                    if (!string.IsNullOrEmpty(cell.Value))
                    {
                        this.state.SetCellRawValue(rowIdx, colIdx, cell.Value);
                    }
                    else
                    {
                        this.state.DeleteCell(rowIdx, colIdx);
                    }
                }

                this.UpdateContents();
                this.OnTopEditInputSelectionUpdate(true);

                if (state.isCut)
                {
                    this.CancelCopyAndCut();
                }
            }
            else
            {
                // If no data in clipboard, try copy from sheet if there are copy range in state
                if (!this.state.copyFromRange.Equals(CellRange.None) &&
                    !this.state.copyFromRange.Equals(this.state.selectedRange))
                {
                    this.state.RecordUndo("Paste");
                    var fromRange = this.state.copyFromRange;
                    var rowSize = fromRange.endRow - fromRange.startRow;
                    var columnSize = fromRange.endColumn - fromRange.startColumn;

                    var toRange = this.state.range;
                    toRange.endRow = toRange.startRow + rowSize;
                    toRange.endColumn = toRange.startColumn + columnSize;

                    this.state.AutoFillDataFromRangeToRange(fromRange, toRange, this.state.isCut, false);
                    if (this.state.isCut)
                    {
                        this.copyOrCutFromSelector.Hide();
                        this.state.copyFromRange = CellRange.None;
                        this.state.isCut = false;
                    }

                    this.UpdateContents();
                    this.OnTopEditInputSelectionUpdate(true);
                }
            }
        }

        private void CancelCurrent()
        {
            CancelCopyAndCut();
        }

        private void CancelCopyAndCut()
        {
            this.copyOrCutFromSelector.Hide();
            this.state.copyFromRange = CellRange.None;
            this.state.isCut = false;
        }

        private void JumpToCell(int row, int column)
        {
            var viewRange = this.GetViewRange();
            var range = new CellRange(row, row, column, column);
            var rect = this.state.GetCellRectByRange(range);

            if (viewRange.Contains(row, column)
                && (rect.x + rect.width <= viewRange.width)
                && (rect.y + rect.height <= viewRange.height))
            {
                // If current view range contains the cell and whole of cell
                // is in view port, we just jump to the cell. Here just do 
                // nothing
            }
            else
            {
                // Calculate new scrollview value
                var hValue = this.state.scrollLeft + rect.x + rect.width - viewRange.width / 2;
                this.scrollView.horizontalScroller.value = hValue;

                var vValue = this.state.scrollTop + rect.y + rect.height - viewRange.height / 2;
                this.scrollView.verticalScroller.value = vValue;
            }

            this.state.range = range;
            this.state.selectedRange = range;
            this.selector.SetRect(rect);
            this.UpdateHeaders(viewRange);
        }

        private bool IsInWindowState()
        {
            return !columnEditor.Hidden
            || !codeGeneratorEditor.Hidden
            || textInputDialog.visible
            || loading.visible
            || selectionDialog.visible;
        }

        private void AddActionToEnumDropdown(DropdownLabel dropdown, string option, string enumTypeString, int row, int column)
        {
            dropdown.menu.AppendAction(
                option,
                a =>
                {
                    dropdown.text = option;
                    string raw = string.Format("=ENUM(\"{0}\", \"{1}\")", enumTypeString, option);
                    this.state.SetCellRawValue(row, column, raw);

                    this.topCellEditInput.SetValueWithoutFireEvent(raw);

                    var cellRange = new CellRange(row, row, column, column);
                    this.state.selectedRange = cellRange;
                    this.state.range = cellRange;
                    var rect = this.state.GetCellRectByRange(this.state.selectedRange);
                    this.selector.SetRect(rect);
                    this.UpdateContents();
                },
                dropdown.text == option ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
        }
    }
}