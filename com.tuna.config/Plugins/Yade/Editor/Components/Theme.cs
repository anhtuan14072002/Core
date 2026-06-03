//  Copyright (c) 2020-present amlovey
//  
using UnityEditor;

namespace Yade
{
    public class Theme
    {
        private static ThemeData _current;
        public static ThemeData Current
        {
            get
            {
                if (_current == null)
                {
                    DetectTheme();
                }

                return _current;
            }
        }

        public static void DetectTheme()
        {
            bool isPro = EditorGUIUtility.isProSkin;
            _current = isPro ? Pro : Personal;
        }

        public static ThemeData Personal = new ThemeData() 
        {
            LayerMask = "#00000080",
            Background = "#fefefe",
            BorderColor = "#d0d0d0",
            DialogBackground = "#efefef",
            EditorLine = "#d0d0d0",
            InspectorLine = "#808080",
            AutoFillSelector = "#4b89ff70",
            DragSelectorBorder = "#00800080",
            Selector = "#4b89ff",
            SelectorBackgroud = "#4b89ff10",
            SelectorExtraBackround = "#d0d0d060",
            SelectorExtraBorder = "#d0d0d0",
            SelectorCopyModeBackground = "#e0e0e080",
            SelectorCopyModeBorder = "#4b89ff",
            TopCellBorder = "#e0e0e0",
            Divider = "#e0e0e0",
            HeaderBackground = "#f4f5f8",
            HeaderSelectedBackground = "#4b89ff20",
            GridLine = "#e6e6e6",
            StatusBarBackround = "#e0e0e0",
            ForegroundColor = "#303030",
        };

        public static ThemeData Pro = new ThemeData()
        {
            LayerMask = "#00000080",
            Background = "#383838",
            BorderColor = "#d0d0d0",
            DialogBackground = "#404040",
            EditorLine = "#d0d0d0",
            InspectorLine = "#d0d0d0",
            AutoFillSelector = "#4b89ff70",
            DragSelectorBorder = "#00800080",
            Selector = "#4b89ff",
            SelectorBackgroud = "#4b89ff10",
            SelectorExtraBackround = "#4b89ff60",
            SelectorExtraBorder = "#4b89ff",
            SelectorCopyModeBackground = "#4b89ff60",
            SelectorCopyModeBorder = "#4b89ff",
            TopCellBorder = "#4f4f4f",
            Divider = "#4f4f4f",
            HeaderBackground = "#404040",
            HeaderSelectedBackground = "#4b89ff20",
            GridLine = "#4f4f4f",
            StatusBarBackround = "#4f4f4f",
            ForegroundColor = "#a0a0a0",
        };
    }

    public class ThemeData
    {
       public string LayerMask;
       public string Background;
       public string BorderColor;
       public string DialogBackground;
       public string EditorLine;
       public string InspectorLine;
       public string AutoFillSelector;
       public string DragSelectorBorder;
       public string Selector;
       public string SelectorBackgroud;
       public string SelectorExtraBackround;
       public string SelectorExtraBorder;
       public string SelectorCopyModeBackground;
       public string SelectorCopyModeBorder;
       public string TopCellBorder;
       public string Divider;
       public string HeaderBackground;
       public string HeaderSelectedBackground;
       public string GridLine;
       public string StatusBarBackround;
       public string ForegroundColor;
    }
}