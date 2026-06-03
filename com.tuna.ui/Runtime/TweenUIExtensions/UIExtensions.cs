using PrimeTween;
using UnityEngine;

namespace Core
{
    public static class UIExtensions
    {
        public static Tween TweenAnchoredY(this RectTransform rt, float targetY, float duration, Ease ease)
        {
            float startY = rt.anchoredPosition.y;
            return Tween.Custom(
                startY, targetY, duration, value =>
                {
                    var pos = rt.anchoredPosition;
                    pos.y = value;
                    rt.anchoredPosition = pos;
                }, ease
            );
        }

        public static Tween TweenAnchoredX(this RectTransform rt, float targetX, float duration, Ease ease, int cycles, CycleMode cycleMode)
        {
            float startX = rt.anchoredPosition.x;
            return Tween.Custom(
                startX, targetX, duration, value =>
                {
                    var pos = rt.anchoredPosition;
                    pos.x = value;
                    rt.anchoredPosition = pos;
                }, ease, cycles, cycleMode
            );
        }
    }
}