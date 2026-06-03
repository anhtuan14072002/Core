//  Copyright (c) 2020-present amlovey
//  
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using Yade.Runtime;

namespace Yade.Editor
{
    public partial class YadeSheet
    {
        private Container toolbar;

        private void RenderToolbar()
        {
            toolbar = new Container();
            toolbar.SetEdgeDistance(0, 0, 0, float.NaN);
            toolbar.style.height = 32;
            toolbar.SetPadding(6, 0, 0, 0);
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.alignItems = Align.Center;

            var undoButton = new IconButton(Icons.Undo);
            undoButton.OnClick = UnityEditor.Undo.PerformUndo;
            undoButton.tooltip = "Undo";

            var redoButton = new IconButton(Icons.Redo);
            redoButton.OnClick = UnityEditor.Undo.PerformRedo;
            redoButton.tooltip = "Redo";

            var copyButton = new IconButton(Icons.ContentCopy);
            copyButton.tooltip = "Copy";
            copyButton.OnClick = this.Copy;

            var cutButton = new IconButton(Icons.ContentCut);
            cutButton.tooltip = "Cut";
            cutButton.OnClick = this.Cut;

            var pasteButton = new IconButton(Icons.ContentPaste);
            pasteButton.tooltip = "Paste";
            pasteButton.OnClick = this.Paste;
            pasteButton.SetEnabled(false);
            pasteButton.schedule.Execute(() =>
            {
                pasteButton.SetEnabled(IsPasteEnabled());
            }).Every(100);

            var locateInProjectButton = new IconButton(Icons.Room);
            locateInProjectButton.tooltip = "Locate Sheet in Project Window";
            locateInProjectButton.OnClick = LocateSheetInProject;

            toolbar.Add(GetImportExportButton());
            // toolbar.Add(GetExportButton());
            toolbar.Add(GetDivider());
            toolbar.Add(undoButton);
            toolbar.Add(redoButton);
            toolbar.Add(copyButton);
            toolbar.Add(cutButton);
            toolbar.Add(pasteButton);
            toolbar.Add(GetFunctionsButton());
            toolbar.Add(GetDivider());
            toolbar.Add(locateInProjectButton);
            toolbar.Add(GetColumnEditorButton());
            toolbar.Add(GetCodeGeneratorEditor());
            toolbar.Add(GetDivider());
            toolbar.Add(GetHelpButton());

            this.Add(toolbar);
            this.Add(GetSearchInput());
        }

        private IconButton GetHelpButton()
        {
            IconButton helpButton = new IconButton(Icons.HelpOutline);
            helpButton.tooltip = "Open online document";
            helpButton.OnClick = () => UnityEngine.Application.OpenURL(Constants.DOC_URL);

            return helpButton;
        }

        private IconButton GetCodeGeneratorEditor()
        {
            IconButton button = new IconButton(Icons.Code);
            button.tooltip = "Open Code Generator";
            button.OnClick = OpenCodeGeneratorEditor;
            return button;
        }

        private void OpenCodeGeneratorEditor()
        {
            codeGeneratorEditor.Show();
        }

        #region Search Input Control and Events

        private SearchInput GetSearchInput()
        {
            searchInput = new SearchInput(this.state.searchText, this.state.IsInSearchState);
            searchInput.style.position = Position.Absolute;
            searchInput.SetEdgeDistance(float.NaN, 7, 6, float.NaN);
            searchInput.style.width = 210;
            searchInput.style.height = 18;

            searchInput.OnSubmit = OnSearchSubmit;
            searchInput.OnValueChanged = OnSearchValueChanged;
            searchInput.OnGoToNextClick = OnSearchGoToNextClick;
            searchInput.OnGoToPreviousClick = OnSearchGoToPreviousClick;
            searchInput.OnClearClick = OnSearchClearClick;

            return searchInput;
        }

        private void OnSearchClearClick()
        {
            this.state.searchText = string.Empty;
            this.state.IsInSearchState = false;
        }

        private void OnSearchGoToPreviousClick()
        {
            var range = this.state.range;
            var rowCount = this.state.data.GetRowCount();
            var columnCount = this.state.data.GetColumnCount();
            var selectedIndex = range.startRow * rowCount + range.endColumn;
            CellIndex lastCellInBack = null;
            CellIndex lastCellInForward = null;

            for (int row = rowCount - 1; row >= 0; row--)
            {
                for (int column = columnCount - 1; column >= 0; column--)
                {
                    var index = row * rowCount + column;
                    if (index == selectedIndex)
                    {
                        continue;
                    }

                    // Try find last matched cell in following range
                    if (index > selectedIndex && lastCellInForward == null)
                    {
                        if (IsCellMatched(row, column, this.state.searchText))
                        {
                            lastCellInForward = new CellIndex() { row = row, column = column };
                            row = row + 1;
                            break;
                        }
                    }

                    // Try find last matched cell in previous range
                    if (index < selectedIndex && lastCellInBack == null)
                    {
                        if (IsCellMatched(row, column, this.state.searchText))
                        {
                            lastCellInBack = new CellIndex() { row = row, column = column };
                            break;
                        }
                    }
                }

                if (lastCellInBack != null)
                {
                    break;
                }
            }

            if (lastCellInBack != null)
            {
                JumpToCell(lastCellInBack.row, lastCellInBack.column);
                return;
            }

            if (lastCellInForward != null)
            {
                JumpToCell(lastCellInForward.row, lastCellInForward.column);
                return;
            }

            // No more result found, display no results dialog
            if (!IsCellMatched(range.startRow, range.startColumn, this.state.searchText))
            {
                DisplayNoResultsFoundDialog();
            }
        }

        private void OnSearchGoToNextClick()
        {
            var range = this.state.range;
            var rowCount = this.state.data.GetRowCount();
            var columnCount = this.state.data.GetColumnCount();
            var selectedIndex = range.startRow * rowCount + range.endColumn;
            CellIndex firstCellInBack = null;
            CellIndex firstCellInForward = null;

            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    var index = row * rowCount + column;
                    if (index == selectedIndex)
                    {
                        continue;
                    }

                    // Try find first matched cell in previous range
                    if (index < selectedIndex && firstCellInBack == null)
                    {
                        if (IsCellMatched(row, column, this.state.searchText))
                        {
                            firstCellInBack = new CellIndex() { row = row, column = column };
                            row = range.startRow - 1;
                            break;
                        }
                    }

                    // Try find first matched cell in following range
                    if (index > selectedIndex && firstCellInForward == null)
                    {
                        if (IsCellMatched(row, column, this.state.searchText))
                        {
                            firstCellInForward = new CellIndex() { row = row, column = column };
                            break;
                        }
                    }
                }

                if (firstCellInForward != null)
                {
                    break;
                }
            }

            if (firstCellInForward != null)
            {
                JumpToCell(firstCellInForward.row, firstCellInForward.column);
                return;
            }

            if (firstCellInBack != null)
            {
                JumpToCell(firstCellInBack.row, firstCellInBack.column);
                return;
            }

            // No more result found, display no results dialog
            if (!IsCellMatched(range.startRow, range.startColumn, this.state.searchText))
            {
                DisplayNoResultsFoundDialog();
            }
        }

        private void OnSearchValueChanged(string newValue)
        {
            this.state.searchText = newValue;
        }

        private void OnSearchNavigating(CellIndex index)
        {
            this.JumpToCell(index.row, index.column);
        }

        private void OnSearchSubmit(string searchValue)
        {
            if (string.IsNullOrEmpty(searchValue))
            {
                return;
            }

            var rowCount = this.state.data.GetRowCount();
            var columnCount = this.state.data.GetColumnCount();

            CellIndex firstMatchedCell = null;
            List<CellIndex> cells = new List<CellIndex>();
            for (int row = 0; row < rowCount; row++)
            {
                for (int column = 0; column < columnCount; column++)
                {
                    if (IsCellMatched(row, column, searchValue))
                    {
                        firstMatchedCell = new CellIndex() { row = row, column = column };
                        break;
                    }
                }

                // Break the loop if find matched cell
                if (firstMatchedCell != null)
                {
                    break;
                }
            }

            if (firstMatchedCell != null)
            {
                JumpToCell(firstMatchedCell.row, firstMatchedCell.column);
                searchInput.SetResultNavigationUIEnable(true);
                this.state.IsInSearchState = true;
            }
            else
            {
                DisplayNoResultsFoundDialog();
            }
        }

        private bool IsCellMatched(int row, int column, string searchText)
        {
            var cell = this.state.GetCell(row, column);
            if (cell != null && cell.GetRawValue().ToLower().IndexOf(searchText.ToLower()) != -1)
            {
                return true;
            }

            return false;
        }

        private void DisplayNoResultsFoundDialog()
        {
            EditorUtility.DisplayDialog("YADE", "No results found!", "Ok");
        }

        #endregion

        private IconButton GetColumnEditorButton()
        {
            IconButton button = new IconButton(Icons.SupervisorAccount);
            button.tooltip = "Edit Column Headers";
            button.OnClick = OpenColumnEditor;
            return button;
        }

        private void OpenColumnEditor()
        {
            columnEditor.Show();
        }

        private void LocateSheetInProject()
        {
            if (state.data != null)
            {
                EditorGUIUtility.PingObject(state.data);
            }
        }

        private DropdownIconButton GetImportExportButton()
        {
            var dropDownButton = new DropdownIconButton(Icons.ImportExport);
            dropDownButton.tooltip = "Import and Export";
            foreach (var importer in YadeGlobal.importers)
            {
                if (!importer.IsAvailable(this.state)) 
                {
                    continue;
                }

                dropDownButton.menu.AppendAction(
                    importer.GetMenuName(),
                    a => DoImporterClick(importer.GetMenuName()),
                    DropdownMenuAction.AlwaysEnabled
                );
            }
            
            dropDownButton.menu.AppendSeparator();

            foreach (var exporter in YadeGlobal.exporters)
            {
                if (!exporter.IsAvailable(this.state)) 
                {
                    continue;
                }

                dropDownButton.menu.AppendAction(
                    exporter.GetMenuName(),
                    a => DoExporterClick(exporter.GetMenuName()),
                    DropdownMenuAction.AlwaysEnabled
                );
            }

            return dropDownButton;
        }

        private void DoExporterClick(string key)
        {
            foreach (var item in YadeGlobal.exporters)
            {
                if (item.GetMenuName() == key)
                {
                    bool needRefreshUI = item.Execute(this.state);
                    if (needRefreshUI)
                    {
                        this.UpdateUIBaseOnState();
                    }
                    break;
                }
            }
        }

        private void DoImporterClick(string key)
        {
            foreach (var item in YadeGlobal.importers)
            {
                if (item.GetMenuName() == key)
                {
                    bool needRefreshUI = item.Execute(this.state);
                    if (needRefreshUI)
                    {
                        this.UpdateUIBaseOnState();
                    }
                    break;
                }
            }
        }

        private DropdownIconButton GetFunctionsButton()
        {
            var functionsButton = new DropdownIconButton(Icons.Functions);
            functionsButton.tooltip = "Fuctions";
            functionsButton.RegisterCallback<PointerEnterEvent>(evt=> this.editor.SetAllowAutoFocus(false));
            functionsButton.RegisterCallback<PointerLeaveEvent>(evt => {
                if (SelectedDropMenu)
                {
                    SelectedDropMenu = false;
                }
                else
                {
                    this.editor.SetAllowAutoFocus(true);
                }
            });

            foreach (var funName in this.state.data.FormulaEngine.GetFunctionNames())
            {
                functionsButton.menu.AppendAction(funName.ToUpper(), a => DoMenuAction(funName.ToLower()), DropdownMenuAction.AlwaysEnabled);
            }

            return functionsButton;
        }

        private static bool SelectedDropMenu;
        private void DoMenuAction(string key)
        {
            SelectedDropMenu = true;

            if (this.state.selectedRange.HasMultipleCells())
            {
                EditorUtility.DisplayDialog("YADE", "Don't support when multiple cells are selected!", "Ok");
                return;
            }
            
            var valueFormat = "={0}()";
            var value = key.ToUpper();
            var cell = new Cell();
            cell.SetRawValue(string.Format(valueFormat, value));
            var rect = this.state.GetCellRectByRange(this.state.range);
            this.editor.SetRect(rect);
            this.editor.SetActiveMode(EditorActiveMode.Functions);
            this.editor.SetCell(cell);
        }
    }
}