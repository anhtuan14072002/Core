//  Copyright (c) 2022-present amlovey
//  
using UnityEditor;
using Yade.Runtime;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;

namespace Yade.Editor
{
    [CustomEditor(typeof(YadeDatabase))]
    public class YadeDatabaseInspector : UnityEditor.Editor
    {
        private List<YadeSheetData> sheets;
        private StyleSheet stylesheet;

        private ListView listView;
        private Label noDataLabel;

        private void OnEnable()
        {
            if (sheets == null)
            {
                sheets = new List<YadeSheetData>();
            }

            RefershSheets();

            var styleSheetPath = Utilities.GetAppStyleSheetPath();
            this.stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
        }

        private void OnDisable()
        {
            sheets = null;
            stylesheet = null;
            AssetDatabase.SaveAssets();
        }

        private void RefershSheets()
        {
            sheets.Clear();
            var ids = AssetDatabase.FindAssets("t:YadeSheetData");
            foreach (var id in ids)
            {
                var path = AssetDatabase.GUIDToAssetPath(id);
                var sheet = AssetDatabase.LoadAssetAtPath<YadeSheetData>(path);
                sheets.Add(sheet);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.styleSheets.Add(this.stylesheet);
            root.style.flexDirection = FlexDirection.Column;
            root.SetEdgeDistance(-8, 0, 0, 0);
            root.style.marginTop = 8;
            root.style.height = 600;

            root.schedule.Execute(() =>
            {
                Theme.DetectTheme();
                root.ToggleInClassList(EditorGUIUtility.isProSkin ? "pro" : "");
            });

            root.RegisterCallback<DragUpdatedEvent>(OnDragging);
            root.RegisterCallback<DragPerformEvent>(OnDragPerform);
            root.RegisterCallback<DragPerformEvent>(OnDragExit);

            RenderHelpBox(root);
            RenderTitle(root);
            RenderButtons(root);
            RenderListView(root);
            RenderNoDataText(root);

            return root;
        }

        private void OnDragExit(DragPerformEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.None;
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            var targetDB = this.target as YadeDatabase;
            bool hasNew = false;
            foreach (var item in DragAndDrop.objectReferences)
            {
                if (item is YadeSheetData)
                {
                    var key = item.name;
                    if (!targetDB.Sheets.ContainsKey(key))
                    {
                        targetDB.Sheets.Add(key, item as YadeSheetData);
                        hasNew = true;
                        EditorUtility.SetDirty(this.target);
                        AssetDatabase.SaveAssets();
                    }
                }
            }

            if (hasNew)
            {
                ReBindListView();
            }

            DragAndDrop.AcceptDrag();
        }

        private void OnDragging(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
        }

        private void RenderNoDataText(VisualElement root)
        {
            noDataLabel = new Label("Click '+' button to add a sheet, \nor drag a sheet to inspector");
            noDataLabel.style.position = Position.Absolute;
            noDataLabel.SetEdgeDistance(8, float.NaN, 8, float.NaN);
            noDataLabel.style.top = 220;
            noDataLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(noDataLabel);

            noDataLabel.visible = listView.itemsSource.Count == 0;
        }

        private void RenderHelpBox(VisualElement root)
        {
#if UNITY_2020_1_OR_NEWER
            var box = new HelpBox();
            box.messageType = HelpBoxMessageType.Info;
            box.text = @"  DOUBLE CLICK sheet name will open the sheet.";
#else
            var box = new Box();
            box.style.paddingLeft = 8;
            box.style.paddingTop = 6;
            box.style.paddingRight = 8;
            box.style.paddingBottom = 6;
            box.style.marginLeft = 4;
            box.style.marginRight = 8;
            box.Add(new Label("DOUBLE CLICK sheet name will open the sheet."));
#endif

            root.Add(box);
        }

        private void RenderListView(VisualElement root)
        {
            listView = new ListView();
            listView.SetEdgeDistance(4, 6, 8, 0);
            listView.style.height = 360;
            listView.style.borderTopWidth = 1;
            listView.style.borderTopColor = ColorHelper.Parse(Theme.Current.InspectorLine);
            listView.style.borderBottomWidth = 1;
            listView.style.borderBottomColor = ColorHelper.Parse(Theme.Current.InspectorLine);
            listView.style.paddingTop = 4;
            listView.style.marginRight = 8;
            listView.selectionType = SelectionType.Single;

            listView.makeItem = () =>
            {
                var label = new Label();
                label.style.paddingLeft = 4;
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                return label;
            };

            listView.SetItemHeight(24);
            listView.bindItem = (element, index) =>
            {
                var label = element as Label;
                var key = (listView.itemsSource as string[])[index];
                var sheet = (target as YadeDatabase).Sheets[key];
                if (!sheet)
                {
                    var path = AssetDatabase.GetAssetPath(sheet);
                    sheet = AssetDatabase.LoadAssetAtPath<YadeSheetData>(path);
                    (target as YadeDatabase).Sheets[key] = sheet;
                }

                if (!sheet)
                {
                    label.text = string.Format("Sheet '{0}' is missing", key);
                    label.style.unityFontStyleAndWeight = FontStyle.Italic;
                    return;
                }
                
                var labelText = sheet.name;
                label.text = string.Format("{0:d2}.   {1}", index + 1, labelText);
                label.tooltip = AssetDatabase.GetAssetPath(sheet);
            };

            listView.itemsSource = (target as YadeDatabase).Sheets.Keys;

#if !UNITY_2020_1_OR_NEWER
            listView.onItemChosen += OnItemDoubleClick;
            listView.onSelectionChanged += PingObject;

#else
            listView.onItemsChosen += (items) =>
            {
                foreach (var item in items)
                {
                    OnItemDoubleClick(item);
                    break;
                }
            };

            listView.onSelectionChange += PingObject;
#endif

            root.Add(listView);
        }

        private void PingObject(IEnumerable<object> objects)
        {
            foreach (var item in objects)
            {
                var key = item as string;
                var sheet = (target as YadeDatabase).Sheets[key];
                if (sheet)
                {
                    EditorGUIUtility.PingObject(sheet);
                }

                break;
            }
        }

        private void OnItemDoubleClick(object data)
        {
            var key = data as string;
            var sheet = (target as YadeDatabase).Sheets[key];
            var path = AssetDatabase.GetAssetPath(sheet);
            if (!string.IsNullOrEmpty(path))
            {
                YadeEditor.OpenSheet(path);
            }
        }

        private void RenderTitle(VisualElement root)
        {
            var title = new Label("INCLUDED SHEETS:");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginTop = 12;
            title.style.marginLeft = 4;

            root.Add(title);
        }

        private void ReBindListView()
        {
            listView.itemsSource = (target as YadeDatabase).Sheets.Keys;
            noDataLabel.visible = listView.itemsSource.Count == 0;
            this.listView.RebuildView();
        }

        private void BuildAddSheetMenuAndShow()
        {
            if (this.sheets.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "Please create a yade sheet first", "Ok");
                return;
            }

            var targetDB = this.target as YadeDatabase;
            GenericMenu menu = new GenericMenu();

            RefershSheets();

            var usedList = new List<string>();
            var unUsedList = new List<string>();

            foreach (var item in sheets)
            {
                if (targetDB.Sheets.ContainsKey(item.name))
                {
                    usedList.Add(item.name);
                }
                else
                {
                    unUsedList.Add(item.name);
                }
            }

            // Render Unused
            foreach (var item in unUsedList)
            {
                menu.AddItem(new GUIContent(item), false, () =>
                {
                    var sheet = sheets.Find(s => s.name == item);
                    if (sheet != null)
                    {
                        targetDB.Sheets.Add(item, sheet);
                        ReBindListView();
                        EditorUtility.SetDirty(this.target);
                        AssetDatabase.SaveAssets();
                    }
                });
            }

            if (unUsedList.Count > 0)
            {
                menu.AddSeparator("");
            }

            // Render used
            foreach (var item in usedList)
            {
                menu.AddDisabledItem(new GUIContent(item));
            }

            menu.ShowAsContext();
        }

        private void RenderButtons(VisualElement root)
        {
            var buttonGroups = new Container();
            buttonGroups.AddToClassList("yade");
            buttonGroups.style.flexDirection = FlexDirection.RowReverse;
            buttonGroups.style.top = 436;

            IconButton addButton = new IconButton(Icons.Add);
            addButton.tooltip = "Add a sheet to database";
            addButton.OnClick = BuildAddSheetMenuAndShow;

            IconButton removeButton = new IconButton(Icons.Remove);
            removeButton.tooltip = "Remove selected sheet form database";
            removeButton.OnClick = () =>
            {
                if (listView.selectedIndex < 0)
                {
                    return;
                }

                var key = (listView.itemsSource as string[])[listView.selectedIndex];
                var targetDB = target as YadeDatabase;
                targetDB.Sheets.Remove(key);
                ReBindListView();

                EditorUtility.SetDirty(this.target);
                AssetDatabase.SaveAssets();
            };

            buttonGroups.Add(removeButton);
            buttonGroups.Add(addButton);

            root.Add(buttonGroups);
        }
    }
}