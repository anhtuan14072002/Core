using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;

namespace Core
{
    public static class UltilsTween
    {
        public static void DoScaleRect(this RectTransform rect, bool instant, Vector3 scale, float duration, Ease ease)
        {
            if (rect.localScale == scale) return;
            if (instant) rect.localScale = scale;
            else Tween.Scale(rect, new TweenSettings<Vector3>(scale, duration, ease, useUnscaledTime: true));
        }

        public static void DoSetImageColor(this Image image, bool instant, Color color, float duration, Ease ease)
        {
            if (instant) image.color = color;
            else Tween.Color(image, color, duration, ease, useUnscaledTime: true);
        }
    }
}
