using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace WZ
{
    public static class UltilsTween
    {
        public static MotionHandle DoScaleRect(this RectTransform rect, bool instant, Vector3 scale, float duration, Ease ease)
        {
            if (instant)
            {
                rect.localScale = scale;
                return default;
            }

            return LMotion.Create(rect.localScale, scale, duration)
                .WithEase(ease)
                .BindToLocalScale(rect);
        }

        public static MotionHandle DoSetImageColor(this Image image, bool instant, Color color, float duration, Ease ease)
        {
            if (instant)
            {
                image.color = color;
                return default;
            }

            return LMotion.Create(image.color, color, duration)
                .WithEase(ease)
                .BindToColor(image);
        }
    }
}
