//  Copyright (c) 2020-present amlovey
//  
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Yade.Editor
{
    public class AssetLabel : VisualElement
    {
        private Image image;
        private Label label;

        public AssetLabel()
        {
            this.style.position = Position.Absolute;
            this.style.unityTextAlign = TextAnchor.UpperLeft;
            this.style.overflow = Overflow.Hidden;
            this.style.unityOverflowClipBox = OverflowClipBox.ContentBox;
            this.SetPadding(6, 5, 6, 5);
            this.style.flexDirection = FlexDirection.Row;
            this.style.alignItems = Align.Center;
            this.style.flexShrink = 0;

            image = new Image();
            image.style.width = 14;
            image.style.height = 14;
            image.style.marginRight = 6;
            image.style.flexShrink = 0;
            this.Add(image);

            label = new Label();
            label.style.flexShrink = 0;
            this.Add(label);
        }

        public void Update(Texture icon, string name)
        {
            image.image = icon;
            label.text = name;
        }

        public void Update(string assetPath)
        {
            image.image = AssetDatabase.GetCachedIcon(assetPath);
            label.text = Utilities.GetFileName(assetPath);
        }
    }

    public class AssetsLabel : VisualElement
    {
        public AssetsLabel()
        {
            this.style.position = Position.Absolute;
            this.style.unityTextAlign = TextAnchor.UpperLeft;
            this.style.overflow = Overflow.Hidden;
            this.style.unityOverflowClipBox = OverflowClipBox.ContentBox;
            this.SetPadding(6, 5, 6, 5);
            this.style.display = DisplayStyle.Flex;
            this.style.flexDirection = FlexDirection.Column;
        }

        public void SetAssets(UnityEngine.Object[] assets)
        {
            this.Clear();

            foreach (var item in assets)
            {
                AssetLabel label = new AssetLabel();
                label.style.position = Position.Relative;
                label.SetPadding(6, 0, 6, 0);
                label.style.marginBottom = 6;
                label.Update(AssetDatabase.GetAssetPath(item));
                this.Add(label);
            }
        }
    }
}
