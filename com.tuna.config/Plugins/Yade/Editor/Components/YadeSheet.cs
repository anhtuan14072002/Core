//  Copyright (c) 2020-present amlovey
//  
using UnityEngine;
using UnityEngine.UIElements;
using Yade.Runtime;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;

namespace Yade.Editor
{
    public partial class YadeSheet : Container
    {
        private AppState state;
        private Container grids;
        private Container headers;
        private Container content;
        private Container overlay;
        private ScrollView scrollView;
        private VisualElement scrollViewContentPanel;

        private Container extraSelectorsLayer;
        private Selector selector;
        private Selector copyOrCutFromSelector;
        private AutoFillSelector autoFillSelector;
        private DragingSelector dragingSelector;
        private Label selectionText;
        private SelectionDialog selectionDialog;
        private TextInputDialog textInputDialog;
        private Loading loading;

        private SearchInput searchInput;

        private CellInputEditor editor;
        private Container resizerLayer;
        private Resizer verticalResizer;
        private Resizer horizontalResizer;
        private TopCellEditInput topCellEditInput;
        private Container statusBar;

        private ColumnEditor columnEditor;
        private CodeGeneratorEditor codeGeneratorEditor;

        private CommandRegister commandRegister;
        private bool dragingAutoFillElement;

        private bool isResizing;
        private int resizingColumn;
        private int resizingRow;

        private bool isTopCellEditInputResizing;

        // Visual elements pooling
        private ElementsPool<Label> labelCellsPool;
        private ElementsPool<AssetLabel> assetLabelsPool;
        private ElementsPool<AssetsLabel> assetsPackPool;
        private ElementsPool<DropdownLabel> dropdownLabelPools;
        private ElementsPool<Label> headersPool;
        private ElementsPool<Element> linesPool;

        public YadeSheet(AppState state)
        {
            this.AddToClassList("yade");
            this.style.flexShrink = 0;
            this.focusable = true;
            this.state = state;
            this.state.SetBindingSheet(this);

            dragingAutoFillElement = false;
            isResizing = false;
            resizingColumn = -1;
            resizingRow = -1;
            isTopCellEditInputResizing = false;
            commandRegister = new CommandRegister();

            this.RenderHeaderContainers();
            this.RenderGridContainer();
            this.RenderScrollView();
            this.RenderContentLayer();
            this.RenderResizerLayer();
            this.RenderExtraSelectorsLayer();
            this.RenderOverlayer();
            this.RenderFixedTopCellEditorInput();
            this.RenderStatusBar();
            this.RenderToolbar();
            this.RenderColumnEditor();
            this.RenderCodeGeneratorEditor();
            this.RenderSelectionDialog();
            this.RenderTextInputDialog();
            this.RenderLoading();

            labelCellsPool = new ElementsPool<Label>(this.content);
            assetLabelsPool = new ElementsPool<AssetLabel>(this.content);
            assetsPackPool = new ElementsPool<AssetsLabel>(this.content);
            dropdownLabelPools = new ElementsPool<DropdownLabel>(this.content);
            headersPool = new ElementsPool<Label>(this.headers);
            linesPool = new ElementsPool<Element>(this.grids);

            this.RegisterCallback<GeometryChangedEvent>(OnResized);
            this.RegisterCallback<MouseDownEvent>(OnMousedDown);
            this.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            this.RegisterCallback<MouseUpEvent>(OnMouseUp);
            this.RegisterCallback<WheelEvent>(OnWheel);
            this.RegisterCallback<DragUpdatedEvent>(OnDraging);
            this.RegisterCallback<DragExitedEvent>(OnDragExit);
            this.RegisterCallback<DragPerformEvent>(OnDragPerform);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            this.RegisterShortcuts();

            HandleEditorAutoFocus();
        }

        private void RenderLoading()
        {
            loading = new Loading();
            loading.Hide();

            this.Add(loading);
        }

        public void ShowLoading(string msg)
        {
            loading.Show(msg);
        }

        public void HideLoading()
        {
            loading.Hide();
        }

        public void UpdateColumnSettings()
        {
            this.columnEditor.UpdateColumnSettings();
        }

        private void HandleEditorAutoFocus()
        {
            this.schedule.Execute(() =>
            {
                var focusedEelement = this.focusController.focusedElement;
                if (focusedEelement is TextField && focusedEelement != this.editor.InputField)
                {
                    this.editor.AutoFocus = false;
                }
                else
                {
                    if (focusedEelement is ListView || IsInWindowState())
                    {
                        this.editor.AutoFocus = false;
                        return;
                    }

                    this.editor.AutoFocus = true;
                }
            }).Every(30);
        }

        private void RenderCodeGeneratorEditor()
        {
            codeGeneratorEditor = new CodeGeneratorEditor(state);
            if (!state.ShowCodeGeneratorEditor)
            {
                codeGeneratorEditor.Hide();
            }

            this.Add(codeGeneratorEditor);
        }

        public void UpdateUIBaseOnState()
        {
            this.UpdateTableUI();
            this.UpdateScrollViewSize();
            this.UpdateLayersTop();
            scrollView.horizontalScroller.value = this.state.scrollLeft;
            scrollView.verticalScroller.value = this.state.scrollTop;
            OnTopEditInputSelectionUpdate(true);
        }

        private void RenderTextInputDialog()
        {
            textInputDialog = new TextInputDialog();
            textInputDialog.OnClose = textInputDialog.Hide;
            textInputDialog.Hide();

            this.Add(textInputDialog);
        }

        public void ShowTextInputDialog(
            string title,
            string description,
            Action<string> callback
        )
        {
            this.textInputDialog.Show(title, description, callback);
        }

        private void RenderSelectionDialog()
        {
            selectionDialog = new SelectionDialog();
            selectionDialog.OnClose = selectionDialog.Hide;
            selectionDialog.Hide();

            this.Add(selectionDialog);
        }

        public void ShowSelectionDialog(
            string title,
            List<string> options,
            Action<int> callback)
        {
            this.selectionDialog.Show(title, options, callback);
        }

        private void RenderColumnEditor()
        {
            columnEditor = new ColumnEditor(this.state);
            if (!state.showColumnEditor)
            {
                columnEditor.Hide();
            }

            columnEditor.OnClose = () =>
            {
                columnEditor.Hide();
                this.UpdateHeaders(this.GetViewRange());
            };

            this.Add(columnEditor);
        }

        private VisualElement GetDivider()
        {
            VisualElement divider = new VisualElement();
            divider.style.width = 1;
            divider.style.height = 16;
            divider.SetMargin(2, 0);
            divider.style.backgroundColor = ColorHelper.Parse(Theme.Current.Divider);
            return divider;
        }

        private void RenderStatusBar()
        {
            statusBar = new Container();
            statusBar.style.backgroundColor = ColorHelper.Parse(Theme.Current.StatusBarBackround);
            statusBar.SetEdgeDistance(0, float.NaN, 0, 0);
            statusBar.style.height = 24;

            IconButton smile = new IconButton(Icons.SentimentSatisfied);
            smile.style.position = Position.Absolute;
            smile.tooltip = "Please consider leaving review \nto support YADE. Thanks a lot!";
            smile.style.right = 8;
            smile.OnClick = () =>
            {
                Application.OpenURL(Constants.UAS_URL);
            };
            statusBar.Add(smile);

            var versionLabel = new Label(string.Format("Version: {0}", Constants.APPVERSION));
            versionLabel.style.position = Position.Absolute;
            versionLabel.style.fontSize = 12;
            versionLabel.style.right = 32;
            versionLabel.style.top = 6;
            statusBar.Add(versionLabel);

            selectionText = new Label();
            selectionText.style.position = Position.Absolute;
            selectionText.style.fontSize = 12;
            selectionText.style.left = 16;
            selectionText.style.top = 6;
            statusBar.Add(selectionText);

            this.Add(statusBar);
        }

        private void UpdateSelectionText()
        {
            if (this.state.selectedRange.endRow == this.state.rowCount - 1)
            {
                selectionText.text = string.Empty;
                return;
            }

            if (this.state.selectedRange.HasMultipleCells() || this.state.extraSelectedRanges.Count() > 0)
            {
                var total = GetCellsCount(this.state.selectedRange);
                this.state.extraSelectedRanges.ForEach(range => total += GetCellsCount(range));
                selectionText.text = string.Format("Count: {0}", total);
            }
            else
            {
                selectionText.text = string.Empty;
            }
        }

        private int GetCellsCount(CellRange range)
        {
            var rows = range.endRow - range.startRow + 1;
            var columns = range.endColumn - range.startColumn + 1;
            return rows * columns;
        }

        private void UpdateTableUI()
        {
            var viewRange = this.GetViewRange();
            UpdateGrids(viewRange);
            UpdateHeaders(viewRange);
            UpdateSelectorLayout(viewRange);
            UpdateEditorLayout();
            UpdateContents();
        }

        private void RenderFixedTopCellEditorInput()
        {
            topCellEditInput = new TopCellEditInput();
            topCellEditInput.SetSelectionValue(OnTopEditInputSelectionUpdate());
            float top = Constants.FIXED_HEADER_HEIGHT - Row.DEFAULT_HEIGHT - 32;
            float editInputHeight = this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT - top - state.GetExtraHeaderHeight();
            topCellEditInput.SetEdgeDistance(0, top, 0, float.NaN);
            topCellEditInput.style.height = editInputHeight;
            topCellEditInput.OnCellSelectionUpdate = () => OnTopEditInputSelectionUpdate();
            topCellEditInput.OnFocusOut = this.OnTopEditInputFocusOut;
            topCellEditInput.OnValueUpdate = OnTopEditInputValueUpdate;
            topCellEditInput.OnKeyDownEvent = OnTopEditorKeyDown;
            this.Add(topCellEditInput);
        }

        private void RenderContentLayer()
        {
            content = new Container();
            content.SetEdgeDistance(this.state.fixedIndexWidth, this.state.fixedHeaderHeight, 16, 40);
            content.style.overflow = Overflow.Hidden;
            content.style.unityOverflowClipBox = OverflowClipBox.PaddingBox;
            this.Add(content);
        }

        private void RenderExtraSelectorsLayer()
        {
            extraSelectorsLayer = new Container();
            extraSelectorsLayer.SetEdgeDistance(this.state.fixedIndexWidth, this.state.fixedHeaderHeight, 12, 36);
            extraSelectorsLayer.style.overflow = Overflow.Hidden;
            extraSelectorsLayer.style.unityOverflowClipBox = OverflowClipBox.PaddingBox;

            for (int i = 0; i < this.state.extraSelectedRanges.Count; i++)
            {
                var range = this.state.extraSelectedRanges[i];
                var rect = this.state.GetCellRectByRange(range);
                var newSelector = new Selector();
                newSelector.SetRect(rect);
                newSelector.HideHandle();
                newSelector.SetToExtraMode();

                this.extraSelectorsLayer.Add(newSelector);
            }

            this.Add(extraSelectorsLayer);
        }

        private void RenderOverlayer()
        {
            overlay = new Container();
            overlay.SetEdgeDistance(this.state.fixedIndexWidth, this.state.fixedHeaderHeight, 12, 36);
            overlay.style.overflow = Overflow.Hidden;
            overlay.style.unityOverflowClipBox = OverflowClipBox.PaddingBox;

            autoFillSelector = new AutoFillSelector();
            overlay.Add(autoFillSelector);
            autoFillSelector.Hide();

            selector = new Selector();
            selector.OnSelectorChanged += this.UpdateSelectionText;
            selector.OnSelectorChanged += this.UpdateEditorLayout;
            overlay.Add(selector);

            dragingSelector = new DragingSelector();
            dragingSelector.Hide();
            overlay.Add(dragingSelector);

            copyOrCutFromSelector = new Selector();
            copyOrCutFromSelector.HideHandle();
            copyOrCutFromSelector.Hide();
            copyOrCutFromSelector.SetToCopyMode();
            overlay.Add(copyOrCutFromSelector);

            editor = new CellInputEditor();
            editor.OnSubmit = OnEditorSubmit;
            editor.OnKeyDownEvent += this.OnEditorKeyDown;
            editor.OnKeyDownEvent += this.OnKeyDown;
            editor.OnFocused = OnEditorFocused;
            editor.OnValueUpdate = OnEditorValueUpdate;
            editor.Hide();
            overlay.Add(editor);

            this.Add(overlay);
        }

        private void ClearExtraSelectors()
        {
            this.extraSelectorsLayer.Clear();
            this.state.extraSelectedRanges.Clear();
        }

        private void AddNewSelector(CellRange range)
        {
            var rect = this.state.GetCellRectByRange(range);
            var newSelector = new Selector();
            newSelector.SetRect(rect);
            newSelector.HideHandle();
            newSelector.SetToExtraMode();

            this.extraSelectorsLayer.Add(newSelector);
            this.state.extraSelectedRanges.Add(range);
        }

        private void RenderResizerLayer()
        {
            resizerLayer = new Container();
            resizerLayer.SetEdgeDistance(0, this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT - this.state.GetExtraHeaderHeight(), 12, 36);
            resizerLayer.style.overflow = Overflow.Hidden;
            resizerLayer.style.unityOverflowClipBox = OverflowClipBox.PaddingBox;

            horizontalResizer = new Resizer(false);
            horizontalResizer.Hide();
            verticalResizer = new Resizer(true);
            verticalResizer.Hide();

            resizerLayer.Add(horizontalResizer);
            resizerLayer.Add(verticalResizer);
            this.Add(resizerLayer);
        }

        private void UpdateContents(CellRange hideCells = null)
        {
            if (content == null)
            {
                RenderContentLayer();
            }

            assetLabelsPool.PreparePooling();
            assetsPackPool.PreparePooling();
            labelCellsPool.PreparePooling();
            dropdownLabelPools.PreparePooling();

            var viewRange = this.GetViewRange();
            viewRange.ForEach((row, column) =>
            {
                if (hideCells != null && hideCells.Contains(row, column))
                {
                    return;
                }

                AddOrRecyleCellEelementToContents(row, column);
            });
        }

        private void UpdateSelectCellContent(CellRange selectedCell)
        {
            if (selectedCell == null)
            {
                return;
            }

            if (content == null)
            {
                RenderContentLayer();
            }

            var cellElements = content.Children().ToArray();
            for (int i = 0; i < cellElements.Length; i++)
            {
                var element = cellElements[i];
                if (element.name == string.Format("{0}_{1}", selectedCell.startRow, selectedCell.startColumn))
                {
                    element.visible = false;
                }
            }

            AddOrRecyleCellEelementToContents(selectedCell.startRow, selectedCell.startColumn);
        }

        private void AddOrRecyleCellEelementToContents(int row, int column)
        {
            var cell = this.state.GetCell(row, column);
            if (cell != null)
            {
                var indexKey = GetIndexKey(row, column);

                if (cell.IsFormulaCell())
                {
                    this.state.data.UpdateCell(row, column);

                    var rawValue = cell.GetRawValue().ToLower();

                    // If it's formula cell, but it's not valid, we set it to #N/A
                    bool validFormula = state.data.FormulaEngine.IsValidFormula(rawValue.Substring(1));
                    if (!validFormula)
                    {
                        RenderNotAvailableLabel(row, column);
                        return;
                    }

                    if (rawValue.StartsWith("=asset("))
                    {
                        if (cell.HasUnityObject())
                        {
                            var assetPath = AssetDatabase.GetAssetPath(cell.GetUnityObject());
                            var realtimeRawValue = string.Format("=ASSET(\"{0}\")", assetPath);
                            var element = GetAssetLabel(row, column, assetPath);
                            element.name = indexKey;

                            if (!rawValue.Equals(realtimeRawValue, StringComparison.OrdinalIgnoreCase))
                            {
                                state.SetCellRawValue(row, column, realtimeRawValue);
                            }
                        }
                        else
                        {
                            RenderNotAvailableLabel(row, column);
                        }
                    }
                    else if (rawValue.StartsWith("=assets("))
                    {
                        if (cell.HasUnityObject())
                        {
                            var assets = cell.GetUnityObjects();
                            if (assets != null)
                            {
                                var realtimeRawValue = AssetsFormula.ToRaw(assets);
                                var element = GetAssetsLabel(row, column, assets);
                                element.name = indexKey;

                                if (!rawValue.Equals(realtimeRawValue, StringComparison.OrdinalIgnoreCase))
                                {
                                    state.SetCellRawValue(row, column, realtimeRawValue);
                                }
                            }
                        }
                        else
                        {
                            RenderNotAvailableLabel(row, column);
                        }
                    }
                    else if (rawValue.StartsWith("=enum("))
                    {
                        var value = cell.GetValue();
                        if (!string.IsNullOrEmpty(value))
                        {
                            var temp = cell.GetRawValue().Split(new char[] { '(', '"' }, System.StringSplitOptions.RemoveEmptyEntries);
                            var typestring = temp[1];
                            var options = YadeGlobal.GetEnumValues(typestring);

                            // Not match value
                            if (!options.Any(item => item.ToLower().Trim() == value.ToLower().Trim()))
                            {
                                RenderNotAvailableLabel(row, column);
                                return;
                            }

                            if (options != null)
                            {
                                var element = GetDropdownLabel(row, column, value, options);
                                element.ClearMenus();

                                for (int i = 0; i < options.Length; i++)
                                {
                                    var optionText = options[i];
                                    AddActionToEnumDropdown(element, optionText, typestring, row, column);
                                }

                                element.name = indexKey;
                            }
                        }
                        else
                        {
                            RenderNotAvailableLabel(row, column);
                        }
                    }
                    else if (rawValue.Replace(" ", "").Equals("=concat()"))
                    {
                        RenderNotAvailableLabel(row, column);
                    }
                    else
                    {
                        var label = GetCellLabel(row, column);
                        label.name = indexKey;
                        label.text = cell.GetValue();
                    }
                }
                else
                {
                    var label = GetCellLabel(row, column);
                    label.name = indexKey;
                    label.text = cell.GetRawValue();
                }
            }
        }

        private void RenderNotAvailableLabel(int row, int column)
        {
            var label = GetCellLabel(row, column);
            label.text = Constants.NotAvailableText;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private string GetIndexKey(int row, int column)
        {
            return string.Format("{0}_{1}", row, column);
        }

        private DropdownLabel GetDropdownLabel(int row, int column, string text, string[] options)
        {
            var element = dropdownLabelPools.GetOne();
            element.text = text;

            var rect = this.state.GetCellRectByRange(new CellRange(row, row, column, column));
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;

            return element;
        }

        private AssetsLabel GetAssetsLabel(int row, int column, UnityEngine.Object[] assets)
        {
            var element = assetsPackPool.GetOne();
            var names = assets.Select(AssetDatabase.GetAssetPath).Select(Path.GetFileNameWithoutExtension);
            element.SetAssets(assets);
            var rect = this.state.GetCellRectByRange(new CellRange(row, row, column, column));
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
            return element;
        }

        private AssetLabel GetAssetLabel(int row, int column, string assetPath)
        {
            var element = assetLabelsPool.GetOne();
            element.Update(assetPath);
            var rect = this.state.GetCellRectByRange(new CellRange(row, row, column, column));
            element.style.left = rect.x;
            element.style.top = rect.y;
            element.style.width = rect.width;
            element.style.height = rect.height;
            return element;
        }

        private Label GetCellLabel(int row, int column)
        {
            Label label = labelCellsPool.GetOne();
            label.style.position = Position.Absolute;
            label.style.unityTextAlign = TextAnchor.UpperLeft;
            label.SetPadding(6, 5, 6, 5);
            label.style.overflow = Overflow.Hidden;
            label.style.unityOverflowClipBox = OverflowClipBox.ContentBox;

            var rect = this.state.GetCellRectByRange(new CellRange(row, row, column, column));
            label.style.left = rect.x;
            label.style.top = rect.y;
            label.style.width = rect.width;
            label.style.height = rect.height;

            return label;
        }

        private void UpdateLayersTop()
        {
            this.scrollView.style.top = this.state.fixedHeaderHeight;
            this.content.style.top = this.state.fixedHeaderHeight;
            this.resizerLayer.style.top = this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT;
            this.overlay.style.top = this.state.fixedHeaderHeight;
            this.extraSelectorsLayer.style.top = this.state.fixedHeaderHeight;

            // Need update resizer layer also
            resizerLayer.SetEdgeDistance(0, this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT - this.state.GetExtraHeaderHeight(), 12, 36);
        }

        private void UpdateSelectorLayout(ViewRange viewRange)
        {
            if (viewRange.Intersects(this.state.selectedRange))
            {
                var rect = this.state.GetCellRectByRange(this.state.selectedRange);
                selector.SetRect(rect);
            }
            else
            {
                selector.Hide();
            }

            this.extraSelectorsLayer.Clear();
            for (int i = 0; i < this.state.extraSelectedRanges.Count; i++)
            {
                var range = this.state.extraSelectedRanges[i];
                if (viewRange.Intersects(range))
                {
                    var newRect = this.state.GetCellRectByRange(range);
                    var newSelector = new Selector();
                    newSelector.SetRect(newRect);
                    newSelector.HideHandle();
                    newSelector.SetToExtraMode();
                    this.extraSelectorsLayer.Add(newSelector);
                }
            }

            if (!this.state.copyFromRange.Equals(CellRange.None))
            {
                var cRect = this.state.GetCellRectByRange(this.state.copyFromRange);
                this.copyOrCutFromSelector.SetRect(cRect);
            }
        }

        private void UpdateEditorLayout()
        {
            var selectRange = this.state.selectedRange;
            if (this.state.selectedRange.HasMultipleCells())
            {
                selectRange = new CellRange(selectRange.startRow, selectRange.startRow, selectRange.startColumn, selectRange.startColumn);
            }

            var rect = this.state.GetCellRectByRange(selectRange);
            editor.SetRect(rect);
        }

        private void RenderScrollView()
        {
            scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scrollView.StretchToParentSize();
            scrollView.style.top = this.state.fixedHeaderHeight;
            scrollView.style.bottom = 24;
            scrollView.style.left = this.state.fixedIndexWidth;
            scrollView.horizontalScroller.valueChanged += OnHorizontalScroll;
            scrollView.verticalScroller.valueChanged += OnVerticalScroll;

            scrollViewContentPanel = new VisualElement();
            scrollViewContentPanel.visible = false;
            scrollView.contentContainer.Add(scrollViewContentPanel);
            this.Add(scrollView);
            this.UpdateScrollViewSize();

            scrollView.RegisterCallback<GeometryChangedEvent>(OnScrollViewInit);
        }

        private void UpdateScrollViewSize()
        {
            float width = this.state.GetColumnTotalWidth(0, this.state.columnCount - 1);
            float height = this.state.GetRowTotalHeight(0, this.state.rowCount - 1);
            scrollViewContentPanel.style.width = width;
            scrollViewContentPanel.style.height = height;
        }

        private void OnScrollViewInit(GeometryChangedEvent evt)
        {
            scrollView.horizontalScroller.value = this.state.scrollLeft;
            scrollView.verticalScroller.value = this.state.scrollTop;
            scrollView.UnregisterCallback<GeometryChangedEvent>(OnScrollViewInit);
        }

        private void RenderGridContainer()
        {
            grids = new Container();
            grids.style.right = 12;
            grids.style.overflow = Overflow.Hidden;
            grids.style.unityOverflowClipBox = OverflowClipBox.ContentBox;
            this.Add(grids);
        }

        private void RenderHeaderContainers()
        {
            headers = new Container();
            this.Add(headers);
        }

        private void UpdateHeaders(ViewRange viewRange)
        {
            if (headers == null)
            {
                RenderHeaderContainers();
            }

            headers.Clear();
            headersPool.PreparePooling();

            this.RenderSelectAllCell();
            this.UpdateFixedIndexHeaders(viewRange);
            this.UpdateFixedTopHeaders(viewRange);
        }

        private void RenderSelectAllCell()
        {
            var label = this.RenderFixedHeaderLabel("");
            label.style.width = this.state.fixedIndexWidth;
            label.style.height = Row.DEFAULT_HEIGHT;
            label.style.left = 0;
            label.style.top = this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT;
            this.headers.Add(label);
        }

        private void UpdateFixedTopHeaders(ViewRange viewRange)
        {
            float xPos = this.state.fixedIndexWidth;
            float yPos = this.state.fixedHeaderHeight - Row.DEFAULT_HEIGHT;

            // Render fixed columns for visual headers
            for (int i = 0; i < this.state.GetExtraHeaderCount(); i++)
            {
                float y = yPos;
                float width = this.state.fixedIndexWidth;
                if (state.IsVisualHeaderVisible(VisualHeaderType.Alias))
                {
                    y -= Row.DEFAULT_HEIGHT;
                    var label = RenderColumnHeaderLabel("Alias", false, width, 0, y);
                    headers.Add(label);
                }

                if (state.IsVisualHeaderVisible(VisualHeaderType.Field))
                {
                    y -= Row.DEFAULT_HEIGHT;
                    var label = RenderColumnHeaderLabel("Field", false, width, 0, y);
                    headers.Add(label);
                }

                if (state.IsVisualHeaderVisible(VisualHeaderType.DataType))
                {
                    y -= Row.DEFAULT_HEIGHT;
                    var label = RenderColumnHeaderLabel("Type", false, width, 0, y);
                    headers.Add(label);
                }
            }

            // Render other columns
            for (int i = viewRange.startColumn; i <= viewRange.endColumn; i++)
            {
                var width = this.state.GetColumnWidth(i);
                bool selected = i >= this.state.selectedRange.startColumn && i <= this.state.selectedRange.endColumn;
                var alphaIndexHeader = IndexHelper.IntToAlphaIndex(i);
                var indexLabel = RenderColumnHeaderLabel(alphaIndexHeader, selected, width, xPos, yPos);
                headers.Add(indexLabel);

                float y = yPos;
                if (state.IsVisualHeaderVisible(VisualHeaderType.Alias))
                {
                    y -= Row.DEFAULT_HEIGHT;
                    var header = this.state.data.GetColumnHeaderAlias(i);
                    var label = RenderColumnHeaderLabel(header, selected, width, xPos, y);
                    headers.Add(label);
                }

                if (state.IsVisualHeaderVisible(VisualHeaderType.Field))
                {
                    y -= Row.DEFAULT_HEIGHT;
                    var header = this.state.data.GetColumnHeaderField(i);
                    var label = RenderColumnHeaderLabel(header, selected, width, xPos, y);
                    headers.Add(label);
                }

                if (state.IsVisualHeaderVisible(VisualHeaderType.DataType))
                {
                    y -= Row.DEFAULT_HEIGHT;
                    int dataType = this.state.data.GetColumnHeaderType(i);
                    var header = DataTypeMapper.KeyToName(dataType);
                    if (string.IsNullOrEmpty(this.state.data.GetColumnHeaderAlias(i))
                        && string.IsNullOrEmpty(this.state.data.GetColumnHeaderField(i)))
                    {
                        header = string.Empty;
                    }

                    header = Utilities.GetTypeClassName(header);
                    var label = RenderColumnHeaderLabel(header, selected, width, xPos, y);
                    headers.Add(label);
                }

                xPos += width;
            }
        }

        private Label RenderColumnHeaderLabel(string header, bool selected, float width, float xPos, float yPos)
        {
            var label = RenderFixedHeaderLabel(header, selected);
            label.style.height = Row.DEFAULT_HEIGHT;
            label.style.width = width;
            label.style.top = yPos;
            label.style.left = xPos;
            label.style.overflow = Overflow.Hidden;
            label.style.unityOverflowClipBox = OverflowClipBox.ContentBox;

            return label;
        }

        private void UpdateFixedIndexHeaders(ViewRange viewRange)
        {
            float yPos = this.state.fixedHeaderHeight;
            for (int i = viewRange.startRow; i <= viewRange.endRow; i++)
            {
                bool selected = i >= this.state.selectedRange.startRow && i <= this.state.selectedRange.endRow;
                Label label = RenderFixedHeaderLabel((i + 1).ToString(), selected);
                float height = this.state.GetRowHeight(i);
                label.style.height = height;
                label.style.width = this.state.fixedIndexWidth;
                label.style.left = 0;
                label.style.top = yPos;

                yPos += height;
                headers.Add(label);
            }
        }

        private Label RenderFixedHeaderLabel(string header, bool selected = false)
        {
            var label = headersPool.GetOne();
            label.text = header;
            label.style.position = Position.Absolute;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.backgroundColor = ColorHelper.Parse(selected ? Theme.Current.HeaderSelectedBackground : Theme.Current.HeaderBackground);

            return label;
        }

        private void UpdateGrids(ViewRange viewRange)
        {
            if (grids == null)
            {
                RenderGridContainer();
            }

            linesPool.PreparePooling();
            DrawGridLines(viewRange);
        }

        private void DrawGridLines(ViewRange range)
        {
            var cellRect = this.state.GetCellRectByRange(range);

            // Draw horizontal lines
            float yPos = this.state.fixedHeaderHeight;

            for (int i = 0; i < this.state.GetExtraHeaderCount(); i++)
            {
                var y = yPos - (i + 1) * Row.DEFAULT_HEIGHT;
                this.DrawLine(0, y, cellRect.width + this.state.fixedIndexWidth, y);
            }

            for (int i = range.startRow; i <= range.endRow; i++)
            {
                float height = this.state.GetRowHeight(i);
                this.DrawLine(0, yPos, cellRect.width + this.state.fixedIndexWidth, yPos);
                yPos += height;

                if (yPos > cellRect.height + this.state.fixedHeaderHeight) break;
            }

            this.DrawLine(0, yPos, cellRect.width + this.state.fixedIndexWidth, yPos);

            // draw vertical lines
            float xPos = this.state.fixedIndexWidth;
            float startY = this.state.fixedHeaderHeight;
            for (int i = range.startColumn; i <= range.endColumn; i++)
            {
                float width = this.state.GetColumnWidth(i);
                this.DrawLine(xPos, startY - Row.DEFAULT_HEIGHT - this.state.GetExtraHeaderHeight(), xPos, cellRect.height + startY);
                xPos += width;
                if (xPos > cellRect.width + this.state.fixedIndexWidth) break;
            }

            this.DrawLine(xPos, startY - Row.DEFAULT_HEIGHT, xPos, cellRect.height + startY);
        }

        private void DrawLine(float x1, float y1, float x2, float y2)
        {
            var line = linesPool.GetOne();
            line.style.backgroundColor = ColorHelper.Parse(Theme.Current.GridLine);

            float width = Mathf.Abs(x2 - x1);
            float height = Mathf.Abs(y2 - y1);
            if (width > height)
            {
                height = 1f;
            }
            else
            {
                width = 1f;
            }

            line.SetOffset(new Offset(x1, y1));
            line.SetSize(new Size(width, height));
            grids.Add(line);
        }
    }
}
