//  Copyright (c) 2020-present amlovey
//  
using UnityEngine.UIElements;
using Yade.Runtime;

namespace Yade.Editor
{
    public partial class YadeSheet
    {
        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (IsClickedOnTopHeaders(evt))
            {
                BuildHeaderContextMenu(evt);
                return;
            }

            if (IsClickOnIndexHeaders(evt))
            {
                this.BuildIndexsHeaderContextMenu(evt);
                return;
            }

            if (IsClickedOnSelectedCells(evt))
            {
                BuildCellsContextMenu(evt);
                return;
            }

            evt.PreventDefault();
            evt.StopImmediatePropagation();
        }

        private void BuildIndexsHeaderContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (this.state.selectedRange.startColumn < 0 && this.state.extraSelectedRanges.Count == 0)
            {
                evt.menu.AppendAction("Insert", a => OnContextMenuClick("insert"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Delete", a => OnContextMenuClick("delete"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Clear Contents", a => OnContextMenuClick("clear"), DropdownMenuAction.AlwaysEnabled);
            }
        }

        private void BuildHeaderContextMenu(ContextualMenuPopulateEvent evt)
        {
            // If clicked on the header of selected columns
            if (this.state.selectedRange.startRow < 0 
                && this.state.extraSelectedRanges.Count == 0
                && this.IsClikedOnSelectedColumnHeaders(evt))
            {
                evt.menu.AppendAction("Insert", a => OnContextMenuClick("insert"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Delete", a => OnContextMenuClick("delete"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Clear Contents", a => OnContextMenuClick("clear"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }

            AddShowHeaderAction(evt, "Show Data Type Header", VisualHeaderType.DataType);
            AddShowHeaderAction(evt, "Show Field Header", VisualHeaderType.Field);
            AddShowHeaderAction(evt, "Show Alias Header", VisualHeaderType.Alias);
        }

        private void AddShowHeaderAction(ContextualMenuPopulateEvent evt, string name, VisualHeaderType type)
        {
            var visible = this.state.IsVisualHeaderVisible(type);
            evt.menu.AppendAction(
                name,
                a => OnVisualHeaderChange(type, !visible),
                visible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
            );
        }

        private void OnVisualHeaderChange(VisualHeaderType type, bool visible)
        {
            this.state.SetVisualHeaderVisible(type, visible);
            this.state.fixedHeaderHeight += visible ? Row.DEFAULT_HEIGHT : -Row.DEFAULT_HEIGHT;
            this.UpdateUIBaseOnState();
        }

        private void BuildCellsContextMenu(ContextualMenuPopulateEvent evt)
        {
            bool hasExtraSelection = this.state.extraSelectedRanges.Count > 0;
            if (!hasExtraSelection)
            {
                evt.menu.AppendAction("Cut", a => OnContextMenuClick("cut"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Copy", a => OnContextMenuClick("copy"), DropdownMenuAction.AlwaysEnabled);
                if (IsPasteEnabled())
                {
                    evt.menu.AppendAction("Paste", a => OnContextMenuClick("paste"), DropdownMenuAction.AlwaysEnabled);
                }
            }

            var selectedRange = state.selectedRange;
            if (selectedRange != null && selectedRange.startColumn == selectedRange.endColumn)
            {
                var column = IndexHelper.IntToAlphaIndex(selectedRange.startColumn);

                evt.menu.AppendSeparator();
                evt.menu.AppendAction(string.Format("Sort Sheet/By Column {0} (A to Z)", column), a => OnContextMenuClick("sortaz"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction(string.Format("Sort Sheet/By Column {0} (Z to A)", column), a => OnContextMenuClick("sortza"), DropdownMenuAction.AlwaysEnabled);
            }

            if (ShowPingInUnity())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Locate in Project", a => OnContextMenuClick("ping"), DropdownMenuAction.AlwaysEnabled);
            }

            if (!hasExtraSelection)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Add Row", a => OnContextMenuClick("addrow"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Add Column", a => OnContextMenuClick("addcolumn"), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendAction("Delete Row", a => OnContextMenuClick("deleterow"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Delete Column", a => OnContextMenuClick("deletecolumn"), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Clear Contents", a => OnContextMenuClick("delete"), DropdownMenuAction.AlwaysEnabled);

            if (YadeGlobal.extendedContextMenuItem != null)
            {
                if (YadeGlobal.extendedContextMenuItem.Count > 0)
                {
                    evt.menu.AppendSeparator();
                }

                foreach (var item in YadeGlobal.extendedContextMenuItem)
                {
                    // Fliter out unavailable menus
                    if (!item.IsAvailable(this.state))
                    {
                        continue;
                    }

                    var enabledState = item.GetEnabledState(state) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                    evt.menu.AppendAction(item.GetMenuName(), a => OnContextMenuClick(item.GetMenuKey()), enabledState);
                }
            }
        }

        private bool IsPasteEnabled()
        {
            var clipboardData = Utilities.GetClipboardData();
            if (!string.IsNullOrWhiteSpace(clipboardData))
            {
                return true;
            }
            else
            {
                return !this.state.copyFromRange.Equals(CellRange.None);
            }
        }

        private bool ShowPingInUnity()
        {
            bool allAreUnityObjects = true;
            this.state.selectedRange.ForEach((row, column) =>
            {
                var cell = this.state.GetCell(row, column);
                if (cell != null)
                {
                    allAreUnityObjects = allAreUnityObjects && cell.HasUnityObject();
                }
                else
                {
                    allAreUnityObjects = false;
                }
            });
            return allAreUnityObjects;
        }

        private void OnContextMenuClick(string key)
        {
            switch (key)
            {
                case "addrow":
                    this.AddRow();
                    break;
                case "addcolumn":
                    this.AddColumn();
                    break;
                case "clear":
                    this.DeleteSelectedCells();
                    break;
                case "insert":
                    this.AddRowsOrColumnsFromFixedHeaders();
                    break;
                case "delete":
                    this.DeletedRowsOrColumnsFromFixedHeaders();
                    break;
                case "deleterow":
                    this.DeleteSelectedRows();
                    break;
                case "deletecolumn":
                    this.DeleteSelectedColumns();
                    break;
                case "copy":
                    this.Copy();
                    break;
                case "paste":
                    this.Paste();
                    break;
                case "cut":
                    this.Cut();
                    break;
                case "ping":
                    PingSelectedInProjectWindow();
                    break;
                case "sortaz":
                case "sortza":
                    state.SheetOrderByColumn(key == "sortaz");
                    UpdateContents();
                    break;
            }

            if (YadeGlobal.extendedContextMenuItem != null)
            {
                foreach (var item in YadeGlobal.extendedContextMenuItem)
                {
                    if (item.GetMenuKey().ToLower() == key.ToLower())
                    {
                        item.Execute(state);
                        break;
                    }
                }
            }
        }

        private bool IsClickedOnTopHeaders(ContextualMenuPopulateEvent evt)
        {
            return (evt.mousePosition.y < this.state.fixedHeaderHeight + Row.DEFAULT_HEIGHT)
                && (evt.mousePosition.y > this.state.fixedHeaderHeight - state.GetExtraHeaderCount() * Row.DEFAULT_HEIGHT)
                && (evt.mousePosition.x > this.state.fixedIndexWidth);
        }

        private bool IsClikedOnSelectedColumnHeaders(ContextualMenuPopulateEvent evt)
        {
            var selectedRange = this.state.selectedRange;
            var offsetY = evt.mousePosition.y - this.state.fixedHeaderHeight + state.GetExtraHeaderCount() * Row.DEFAULT_HEIGHT;
            var offsetX = evt.mousePosition.x - this.state.fixedIndexWidth;
            var rect = this.state.GetCellRectByRange(selectedRange);
            return rect.IsXYInRect(offsetX, offsetY);
        }

        private bool IsClickOnIndexHeaders(ContextualMenuPopulateEvent evt)
        {
            var clickOnIndexesHeader = evt.mousePosition.x < this.state.fixedIndexWidth
                && (evt.mousePosition.y > this.state.fixedHeaderHeight + Row.DEFAULT_HEIGHT);
            
            var selectedRange = this.state.selectedRange;
            var offsetY = evt.mousePosition.y - this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT;
            var offsetX = evt.mousePosition.x;
            var rect = this.state.GetCellRectByRange(selectedRange);
            return rect.IsXYInRect(offsetX, offsetY) && clickOnIndexesHeader;
        }

        private bool IsClickedOnSelectedCells(ContextualMenuPopulateEvent evt)
        {
            var offsetY = evt.mousePosition.y - this.state.fixedHeaderHeight;
            var offsetX = evt.mousePosition.x - this.state.fixedIndexWidth;
            CellRange selectedRange = this.state.selectedRange;

            var sRect = this.state.GetCellRectByRange(selectedRange);
            return sRect.IsXYInRect(offsetX, offsetY - Row.DEFAULT_HEIGHT);
        }
    }
}