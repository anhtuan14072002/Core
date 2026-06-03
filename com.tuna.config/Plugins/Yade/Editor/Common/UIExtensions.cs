//  Copyright (c) 2020-present amlovey
//  
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public static class UIExtensions
    {
        public static void SetEdgeDistance(this VisualElement element, float left, float top, float right, float bottom)
        {
            element.style.left = left;
            element.style.right = right;
            element.style.top = top;
            element.style.bottom = bottom;
        }

        public static void SetMarign(this VisualElement element, float left, float top, float right, float bottom)
        {
            element.style.marginLeft = left;
            element.style.marginTop = top;
            element.style.marginRight = right;
            element.style.marginBottom = bottom;
        }

        public static void SetMargin(this VisualElement element, float leftRight, float topBottom)
        {
            element.style.marginLeft = leftRight;
            element.style.marginTop = topBottom;
            element.style.marginRight = leftRight;
            element.style.marginBottom = topBottom;
        }

        public static void SetMargin(this VisualElement element, float margin)
        {
            element.style.marginLeft = margin;
            element.style.marginTop = margin;
            element.style.marginRight = margin;
            element.style.marginBottom = margin;
        }

        public static void SetPadding(this VisualElement element, float width)
        {
            element.style.paddingLeft = width;
            element.style.paddingRight = width;
            element.style.paddingTop = width;
            element.style.paddingBottom = width;
        }

        public static void SetPadding(this VisualElement element, float topDown, float leftRight)
        {
            element.style.paddingLeft = leftRight;
            element.style.paddingRight = leftRight;
            element.style.paddingBottom = topDown;
            element.style.paddingTop = topDown;
        }

        public static void SetPadding(this VisualElement element, float left, float top, float right, float bottom)
        {
            element.style.paddingLeft = left;
            element.style.paddingRight = right;
            element.style.paddingBottom = bottom;
            element.style.paddingTop = top;
        }

        public static void SetBorderWidth(this VisualElement element, float width)
        {
            element.style.borderBottomWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
        }

        public static void SetBorderRadius(this VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
            element.style.borderTopRightRadius = radius;
        }

        public static void SetBorderWidth(this VisualElement element, float left, float top, float right, float bottom)
        {
            element.style.borderBottomWidth = bottom;
            element.style.borderTopWidth = top;
            element.style.borderLeftWidth = left;
            element.style.borderRightWidth = right;
        }

        public static void SetBorderColor(this VisualElement element, Color color)
        {
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
            element.style.borderTopColor = color;
        }

        public static void BindMaterialIconFontIfNeeds(this VisualElement element)
        {
#if UNITY_2021_2_OR_NEWER
            var fontAsset = Utilities.GetFontAsset();
            element.style.unityFontDefinition = new StyleFontDefinition(fontAsset);
#endif
        }

        public static void SetItemHeight(this ListView listView, int height)
        {
#if UNITY_2021_2_OR_NEWER
            listView.fixedItemHeight = height;
#else
            listView.itemHeight = height;
#endif
        }

        public static void RebuildView(this ListView listView)
        {
#if UNITY_2021_2_OR_NEWER
            listView.Rebuild();
#else
            listView.Refresh();
#endif 
        }
    }
}