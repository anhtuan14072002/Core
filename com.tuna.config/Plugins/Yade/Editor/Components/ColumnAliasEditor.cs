//  Copyright (c) 2020-present amlovey
//  
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Yade.Runtime;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;

namespace Yade.Editor
{
    public class ColumnEditor : Container
    {
        public bool Hidden
        {
            get
            {
                return !this.state.showColumnEditor;
            }
        }

        public Action OnClose;

        private Clickable clickable;
        private AppState state;
        private VisualElement dialog;
        private ListView list;
        private IconButton copySettings;
        private IconButton pasteSettings;
        private const string PREFIX = "#$#BENGIN";
        private const string POSTFIX = "#$#END";

        private List<ColumnDefinition> columns;

        public ColumnEditor(AppState state)
        {
            columns = new List<ColumnDefinition>();
            this.state = state;
            InitColumns();

            this.clickable = new Clickable(this.OnClicked);
            this.AddManipulator(this.clickable);
            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.LayerMask);
            this.style.flexDirection = FlexDirection.Column;

            RenderDialogBackground();
            RenderTitle();
            RenderHeaders();
            RenderList();
            RenderButtons();
            RenderCopyPasteButtons();
        }

        private void RenderCopyPasteButtons()
        {
            copySettings = new IconButton(Icons.ContentCopy);
            copySettings.SetEdgeDistance(float.NaN, 0, 42, float.NaN);
            copySettings.style.position = Position.Absolute;
            copySettings.style.top = 6;
            copySettings.OnClick = OnCopy;
            copySettings.tooltip = "Copy all column header settings to clipboard";

            pasteSettings = new IconButton(Icons.ContentPaste);
            pasteSettings.SetEdgeDistance(float.NaN, 0, 16, float.NaN);
            pasteSettings.style.position = Position.Absolute;
            pasteSettings.style.top = 6;
            pasteSettings.OnClick = OnPaste;
            pasteSettings.tooltip = "Paste all column header settings from clipboard";

            dialog.Add(copySettings);
            dialog.Add(pasteSettings);

            dialog.schedule.Execute(evt =>
            {
                if (Hidden || pasteSettings == null)
                {
                    return;
                }

                var clipData = Utilities.GetClipboardData();
                if (clipData.StartsWith(PREFIX) && clipData.EndsWith(POSTFIX))
                {
                    pasteSettings.SetEnabled(true);
                }
                else
                {
                    pasteSettings.SetEnabled(false);
                }

            }).Every(100);
        }

        private void OnPaste()
        {
            var clipData = Utilities.GetClipboardData();
            columns.Clear();

            using (StringReader reader = new StringReader(clipData))
            {
                var line = reader.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith(PREFIX) || line.StartsWith(POSTFIX))
                    {
                        line = reader.ReadLine();
                        continue;
                    }

                    var items = line.Split(new char[] { '+' }, 4);
                    int index = int.Parse(items[0]);
                    int type = int.Parse(items[1]);
                    string field = items[2];
                    string alias = items[3];

                    columns.Add(new ColumnDefinition()
                    {
                        Index = index,
                        Alias = alias,
                        Type = type,
                        Field = field
                    });

                    line = reader.ReadLine();
                }
            }

            list.RebuildView();
        }

        private void OnCopy()
        {
            var keys = this.state.data.columnHeaders.items.Keys;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(PREFIX);
            foreach (var index in keys)
            {
                var element = state.data.columnHeaders.items[index];
                if (!string.IsNullOrEmpty(element.Alias) || !string.IsNullOrEmpty(element.Field))
                {
                    sb.AppendLine(string.Format("{0}+{1}+{2}+{3}", index, element.Type, element.Field, element.Alias));
                }
            }
            sb.Append(POSTFIX);

            Utilities.SetClipboardData(sb.ToString());
            
            EditorUtility.DisplayDialog("Success", "All column header settings are copied to clipboard!", "Ok");
        }

        public void UpdateColumnSettings()
        {
            InitColumns();
            list.RebuildView();
        }

        private void RenderHeaders()
        {
            var indexLabel = CreateHeaderLabel("Index");
            dialog.Add(indexLabel);
            indexLabel.style.left = 16;

            var aliasLabel = CreateHeaderLabel("Alias");
            aliasLabel.style.left = 85;
            dialog.Add(aliasLabel);

            var fieldLabel = CreateHeaderLabel("Field");
            fieldLabel.style.left = 220;
            dialog.Add(fieldLabel);

            var dateTypeLabel = CreateHeaderLabel("Type");
            dateTypeLabel.style.left = 350;
            dialog.Add(dateTypeLabel);
        }

        private Label CreateHeaderLabel(string header)
        {
            var label = new Label(header);
            label.style.position = Position.Absolute;
            label.style.height = 30;
            label.style.fontSize = 13;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.top = 46;

            return label;
        }

        private void InitColumns()
        {
            columns.Clear();
            var keys = this.state.data.columnHeaders.items.Keys;
            keys = keys.OrderBy(key => key).ToArray();
            foreach (var index in keys)
            {
                var element = state.data.columnHeaders.items[index];
                if (!string.IsNullOrEmpty(element.Alias) || !string.IsNullOrEmpty(element.Field))
                {
                    columns.Add(new ColumnDefinition()
                    {
                        Index = index,
                        Alias = element.Alias,
                        Type = element.Type,
                        Field = element.Field
                    });
                }
            }
        }

        private void RenderButtons()
        {
            var buttonGroups = new Container();
            buttonGroups.SetEdgeDistance(15, 210, 15, 0);
            buttonGroups.style.top = 332;
            buttonGroups.style.flexDirection = FlexDirection.RowReverse;

            IconButton addButton = new IconButton(Icons.Add);
            addButton.OnClick = this.AddColumn;

            IconButton removeButton = new IconButton(Icons.Remove);
            removeButton.OnClick = this.RemoveColumn;

            buttonGroups.Add(removeButton);
            buttonGroups.Add(addButton);
            dialog.Add(buttonGroups);
        }

        private void RemoveColumn()
        {
            var index = this.list.selectedIndex;
            if (index < 0)
            {
                return;
            }

            columns.RemoveAt(index);
            list.RebuildView();
        }

        private void AddColumn()
        {
            int initIndex = 0;
            if (columns.Count() > 0)
            {
                initIndex = columns[columns.Count() - 1].Index + 1;
            }

            var newColumn = new ColumnDefinition();
            newColumn.Index = initIndex;
            newColumn.Alias = string.Empty;

            this.columns.Add(newColumn);
            list.RebuildView();
        }

        private void RenderList()
        {
            list = new ListView();
            list.SetItemHeight(32);
            list.selectionType = SelectionType.Single;
            list.style.position = Position.Absolute;
            list.style.alignSelf = Align.Center;
            list.style.top = 72;
            list.style.width = 460;
            list.style.height = 258;
            list.style.borderBottomColor = ColorHelper.Parse(Theme.Current.BorderColor);
            list.style.borderBottomWidth = 1;
            list.style.paddingBottom = 6;

            list.makeItem = () => new ColumnDefinitionElement();
            list.bindItem = (visualElement, index) =>
            {
                var element = visualElement as ColumnDefinitionElement;
                var column = columns[index];
                element.SetValue(column);
            };

            list.itemsSource = columns;

            dialog.Add(list);
        }

        private void RenderDialogBackground()
        {
            dialog = new VisualElement();
            dialog.SetBorderRadius(5);
            dialog.style.backgroundColor = ColorHelper.Parse(Theme.Current.DialogBackground);
            dialog.style.top = 120;
            dialog.style.alignSelf = Align.Center;
            dialog.style.width = 490;
            dialog.style.height = 360;
            dialog.RegisterCallback<MouseDownEvent>(evt => evt.StopImmediatePropagation());

            this.Add(dialog);
        }

        private void RenderTitle()
        {
            var title = new Label("Edit Column Header");
            title.style.position = Position.Absolute;
            title.style.fontSize = 18;
            title.style.left = 15;
            title.style.height = 36;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            dialog.Add(title);

            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.backgroundColor = ColorHelper.Parse(Theme.Current.EditorLine);
            line.style.top = 36;
            line.style.width = 460;
            line.style.height = 1;
            line.style.alignSelf = Align.Center;

            dialog.Add(line);
        }

        private void OnClicked()
        {
            this.SubmitChanges();

            if (OnClose != null)
            {
                OnClose();
            }
        }

        private void SubmitChanges()
        {
            state.RecordUndo("Edit Column Alias");

            // Set alia of column headers
            foreach (var item in this.columns)
            {
                this.state.data.SetColumnHeaderColumn(item.Index, item.Alias, item.Type, item.Field);
            }

            // Delete alias of column headers that are in current setting in this editor
            var keys = this.state.data.columnHeaders.items.Keys;
            foreach (var key in keys)
            {
                if (columns.Any(c => c.Index == key))
                {
                    continue;
                }

                this.state.data.DeleteColumnHeaderSettings(key);
            }

            EditorUtility.SetDirty(state.data);
        }

        public void Hide()
        {
            this.style.display = DisplayStyle.None;
            this.state.showColumnEditor = false;
        }

        public void Show()
        {
            this.style.display = DisplayStyle.Flex;
            this.state.showColumnEditor = true;
            
            this.InitColumns();
            this.list.RebuildView();
        }
    }

    public class ColumnDefinition
    {
        public int Index;
        public string Alias;
        public int Type;
        public string Field;
    }

    public class ColumnDefinitionElement : VisualElement
    {
        private DropdownTextButton indexDropdown;
        private DropdownTextButton dataTypeDropDown;
        private TextField aliasTextField;
        private TextField fieldTextField;
        private ColumnDefinition column;

        public ColumnDefinitionElement()
        {
            this.style.flexDirection = FlexDirection.Row;
            this.style.alignContent = Align.Center;
            this.style.paddingTop = 2;
            this.style.paddingBottom = 2;
            this.AddToClassList("list-row");

            this.RenderIndexDropDown();
            this.RenderAliasTextField();
            this.RenderFieldTextField();
            this.RenderDataTypeList();
        }

        private void RenderFieldTextField()
        {
            fieldTextField = new TextField();
            fieldTextField.RegisterCallback<FocusInEvent>(evt => Input.imeCompositionMode = IMECompositionMode.On);
            fieldTextField.RegisterCallback<FocusOutEvent>(evt => Input.imeCompositionMode = IMECompositionMode.Auto);
            fieldTextField.RegisterCallback<KeyDownEvent>(evt => evt.StopImmediatePropagation());
            
            fieldTextField.style.marginLeft = 10;
            fieldTextField.style.width = 120;
            fieldTextField.RegisterValueChangedCallback(OnFieldChanged);
            this.Add(fieldTextField);
        }

        private void OnFieldChanged(ChangeEvent<string> evt)
        {
            var regex = new Regex(@"([^a-zA-Z0-9_]+)");
            var cleanValue = regex.Replace(evt.newValue, "");
            if (!string.IsNullOrEmpty(cleanValue) && char.IsDigit(cleanValue[0]))
            {
                cleanValue = evt.previousValue;
            }

            column.Field = cleanValue;
            fieldTextField.SetValueWithoutNotify(column.Field);
        }

        private void RenderDataTypeList()
        {
            dataTypeDropDown = new DropdownTextButton(string.Empty);
            dataTypeDropDown.style.width = 116;
            dataTypeDropDown.style.top = 2;
            dataTypeDropDown.style.left = 8;
            dataTypeDropDown.style.fontSize = 13;

            this.Add(dataTypeDropDown);
        }

        private void UpdateDataTypeDropDownSelection()
        {
            dataTypeDropDown.menu.MenuItems().Clear();
            DataTypeMapper.ForEach((i, meta) =>
            {
                dataTypeDropDown.menu.AppendAction(
                    meta.Name,
                    a => OnDateTypeChanged(i),
                    column.Type == i ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                );
            });
        }

        private void OnDateTypeChanged(int index)
        {
            column.Type = index;
            var typeString = DataTypeMapper.KeyToName(index);
            typeString = Utilities.GetTypeClassName(typeString);
            dataTypeDropDown.text = typeString;

            UpdateDataTypeDropDownSelection();
        }

        public void SetValue(ColumnDefinition column)
        {
            this.column = column;
            indexDropdown.text = IndexHelper.IntToAlphaIndex(column.Index);
            aliasTextField.SetValueWithoutNotify(column.Alias);
            fieldTextField.SetValueWithoutNotify(column.Field);

            var typeString = DataTypeMapper.KeyToName(column.Type);
            typeString = Utilities.GetTypeClassName(typeString);
            dataTypeDropDown.text = typeString;

            UpdateDataTypeDropDownSelection();
        }

        private void RenderIndexDropDown()
        {
            indexDropdown = new DropdownTextButton(string.Empty);
            indexDropdown.style.width = 55;
            indexDropdown.style.top = 2;
            indexDropdown.style.left = 2;
            indexDropdown.style.fontSize = 13;

            for (int i = 0; i < 26 * 2; i++)
            {
                var index = i;
                indexDropdown.menu.AppendAction(
                    IndexHelper.IntToAlphaIndex(index),
                    a => OnIndexMenuClick(index),
                    DropdownMenuAction.AlwaysEnabled
                );
            }

            this.Add(indexDropdown);
        }

        private void OnIndexMenuClick(int index)
        {
            indexDropdown.text = IndexHelper.IntToAlphaIndex(index);
            column.Index = index;
        }

        private void RenderAliasTextField()
        {
            aliasTextField = new TextField();
            aliasTextField.RegisterCallback<FocusInEvent>(evt => Input.imeCompositionMode = IMECompositionMode.On);
            aliasTextField.RegisterCallback<FocusOutEvent>(evt => Input.imeCompositionMode = IMECompositionMode.Auto);
            aliasTextField.style.marginLeft = 16;
            aliasTextField.style.width = 120;
            aliasTextField.RegisterValueChangedCallback(OnAliasChanged);

            this.Add(aliasTextField);
        }

        private void OnAliasChanged(ChangeEvent<string> evt)
        {
            column.Alias = evt.newValue;
        }
    }
}