//  Copyright (c) 2021-present amlovey
//  
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using Yade.Runtime;
using UnityEditor;

namespace Yade.Editor
{
    public class CodeGeneratorEditor : Container
    {
        private AppState state;
        private TextField classNameInput;
        private VisualElement dialog;
        private TextField codePreview;
        private Toggle includeAllToggle;

        private Clickable clickable;

        public bool Hidden
        {
            get
            {
                return !this.state.ShowCodeGeneratorEditor;
            }
        }

        public CodeGeneratorEditor(AppState state)
        {
            this.state = state;

            this.style.backgroundColor = ColorHelper.Parse(Theme.Current.LayerMask);
            this.style.flexDirection = FlexDirection.Column;

            this.clickable = new Clickable(this.OnClicked);
            this.AddManipulator(this.clickable);

            RenderDialogBackground();
            RenderClassNameInput();
            RenderTitle();
            RenderPreview();
            RenderButtons();
            RenderIncludeAllToggle();
        }

        private void RenderIncludeAllToggle()
        {
            includeAllToggle = new Toggle();
            includeAllToggle.text = " Include columns without field setting";
            includeAllToggle.SetEdgeDistance(float.NaN, 78, 15, float.NaN);
            includeAllToggle.style.height = 24;
            includeAllToggle.style.position = Position.Absolute;
            includeAllToggle.value = true;
            includeAllToggle.RegisterValueChangedCallback(OnToggleValueChanged);

            dialog.Add(includeAllToggle);
        }

        private void OnToggleValueChanged(ChangeEvent<bool> evt)
        {
            codePreview.value = GetCodeGenerated();
        }

        private void RenderPreview()
        {
            var top = 82;

            Label previewLabel = new Label();
            previewLabel.text = "Preview";
            previewLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            previewLabel.style.position = Position.Absolute;
            previewLabel.style.left = 14;
            previewLabel.style.top = top;
            previewLabel.style.width = 78.5f;
            previewLabel.style.fontSize = 13;

            dialog.Add(previewLabel);

            var list = new ScrollView();
            list.style.position = Position.Absolute;
            list.style.left = 12;
            list.style.top = top + 21;
            list.style.fontSize = 11;
            list.style.width = 456;
            list.style.height = 240;

            codePreview = new TextField();
            codePreview.isReadOnly = true;
            codePreview.AddToClassList("code-preview");
            codePreview.multiline = true;
            codePreview.style.minHeight = 238;

            list.Add(codePreview);
            dialog.Add(list);
        }

        private void RenderClassNameInput()
        {
            var top = 52;

            Label prefix = new Label();
            prefix.text = "ClassName:";
            prefix.style.unityFontStyleAndWeight = FontStyle.Bold;
            prefix.style.position = Position.Absolute;
            prefix.style.left = 14;
            prefix.style.top = top;
            prefix.style.width = 78.5f;
            prefix.style.fontSize = 13;

            dialog.Add(prefix);

            classNameInput = new TextField();
            classNameInput.RegisterCallback<FocusInEvent>(evt => Input.imeCompositionMode = IMECompositionMode.On);
            classNameInput.RegisterCallback<FocusOutEvent>(evt => Input.imeCompositionMode = IMECompositionMode.Auto);
            classNameInput.RegisterCallback<KeyDownEvent>(evt => evt.StopImmediatePropagation());
            
            classNameInput.style.width = 298;
            classNameInput.style.left = 94;
            classNameInput.style.top = top - 4;
            classNameInput.style.height = 24;
            classNameInput.style.fontSize = 13;
            classNameInput.RegisterValueChangedCallback(OnClassNameChanged);

            dialog.Add(classNameInput);
        }

        private void OnClassNameChanged(ChangeEvent<string> evt)
        {
            var regex = new Regex(@"([^a-zA-Z0-9_]+)");
            var cleanValue = regex.Replace(evt.newValue, "");
            if (!string.IsNullOrEmpty(cleanValue) && char.IsDigit(cleanValue[0]))
            {
                cleanValue = evt.previousValue;
            }

            classNameInput.value = cleanValue;
            codePreview.value = GetCodeGenerated();
        }

        private string GetCodeGenerated()
        {
            if (string.IsNullOrEmpty(classNameInput.value))
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder namespaces = new StringBuilder();
            namespaces.AppendLine("using System;");
            namespaces.AppendLine("using Yade.Runtime;");

            StringBuilder body = new StringBuilder();

            body.AppendLine(string.Format("public class {0}", classNameInput.value));
            body.AppendLine("{");

            var columnCount = this.state.data.GetColumnCount();
            for (int i = 0; i < columnCount; i++)
            {
                var alias = this.state.data.GetColumnHeaderAlias(i);
                if (!string.IsNullOrEmpty(alias))
                {
                    body.AppendLine("\t/// <summary>");
                    body.AppendLine(string.Format("\t/// {0}", alias)); 
                    body.AppendLine("\t/// </summary>");
                }

                var field = this.state.data.GetColumnHeaderField(i);
                var typeIndex = this.state.data.GetColumnHeaderType(i);
                var type = DataTypeMapper.KeyToType(typeIndex);

                if (type == null)
                {
                    continue;
                }

                var typeString = type.ToString();
                if (typeString.Contains("+"))
                {
                    var temp = typeString.Split(new char[] { '+' }, System.StringSplitOptions.RemoveEmptyEntries);
                    namespaces.AppendLine(string.Format("using static {0};", temp[0]));
                    typeString = temp[1];
                }
                else if (typeString.StartsWith("System."))
                {
                    typeString = typeString.Substring(7);
                }

                if (string.IsNullOrEmpty(field))
                {
                    if (includeAllToggle.value)
                    {
                        field = "_" + IndexHelper.IntToAlphaIndex(i);
                        body.AppendLine(string.Format("\t[DataField({0})] public {1} {2};", i, typeString, field));
                    }
                }
                else
                {
                    body.AppendLine(string.Format("\t[DataField({0})] public {1} {2};", i, typeString, field));
                }

                if (!string.IsNullOrEmpty(alias) && i < columnCount - 1)
                {
                    body.AppendLine();
                }
            }

            body.AppendLine("}");
            body.AppendLine();

            sb.AppendLine(namespaces.ToString());
            sb.AppendLine(body.ToString());
            sb.AppendLine(@"// Generated by Yade (http://u3d.as/1VtP)");
            return sb.ToString();
        }

        private void OnClicked()
        {
            Hide();
        }

        private void RenderTitle()
        {
            var title = new Label("Code Generator");
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

        private void RenderButtons()
        {
            var buttonGroups = new Container();
            buttonGroups.SetEdgeDistance(float.NaN, 210, 15, float.NaN);
            buttonGroups.style.top = 48;
            buttonGroups.style.height = 24;
            buttonGroups.style.flexDirection = FlexDirection.RowReverse;

            Button addButton = new Button();
            addButton.text = "Generate";
            addButton.style.height = 24;
            addButton.clickable.clicked += Create;

            buttonGroups.Add(addButton);
            dialog.Add(buttonGroups);
        }

        private void Create()
        {
            if (string.IsNullOrEmpty(classNameInput.value))
            {
                EditorUtility.DisplayDialog("Failed", "Class name cannot be empty", "Ok");
                return;
            }

            var folder = Path.Combine(Application.dataPath, "Yade", "CodeGen");
            Directory.CreateDirectory(folder);

            var filePath = Path.Combine(folder, classNameInput.value + ".cs");
            if (File.Exists(filePath))
            {
                bool ret = EditorUtility.DisplayDialog("Opps!", "Class name is already exits! Are you want to replace it?", "Replace", "Cancel");
                if (!ret)
                {
                    return;
                }
            }

            var code = GetCodeGenerated();
            File.WriteAllText(filePath, code);

            Hide();

            EditorUtility.DisplayDialog("Successfully!", string.Format("Code is generated. Script file is {0}", filePath), "Ok");
            AssetDatabase.Refresh();
        }

        public void Hide()
        {
            this.style.display = DisplayStyle.None;
            this.state.ShowCodeGeneratorEditor = false;
        }

        public void Show()
        {
            this.style.display = DisplayStyle.Flex;
            this.state.ShowCodeGeneratorEditor = true;

            classNameInput.SetValueWithoutNotify(string.Empty);
            codePreview.SetValueWithoutNotify(string.Empty);
        }
    }
}