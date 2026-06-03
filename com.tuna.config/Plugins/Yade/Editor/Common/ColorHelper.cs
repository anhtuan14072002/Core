//  Copyright (c) 2020-present amlovey
//  
using UnityEngine;
using System.Collections.Generic;

namespace Yade.Editor
{
    public static class ColorHelper
    {
        private static Dictionary<string, Color> colorMap = new Dictionary<string, Color>();
        public static Color Parse(string colorString)
        {
            if (colorMap.ContainsKey(colorString))
            {
                return colorMap[colorString];
            }

            Color color;
            if (ColorUtility.TryParseHtmlString(colorString, out color))
            {
                colorMap.Add(colorString, color);
                return color;
            }

            throw new System.ArgumentException("color string is incorrect!");
        }

        public static string ToColorString(this Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }
    }
}