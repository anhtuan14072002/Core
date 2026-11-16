using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Core
{
    public static class UIExtensions
    {
        public static MotionHandle TweenAnchoredY(this RectTransform rt, float targetY, float duration, Ease ease)
        {
            return LMotion.Create(rt.anchoredPosition.y, targetY, duration)
                .WithEase(ease)
                .BindToAnchoredPositionY(rt);
        }

        public static MotionHandle TweenAnchoredX(this RectTransform rt, float targetX, float duration, Ease ease, int cycles, LoopType loopType)
        {
            return LMotion.Create(rt.anchoredPosition.x, targetX, duration)
                .WithEase(ease)
                .WithLoops(cycles, loopType)
                .BindToAnchoredPositionX(rt);
        }
    }
}
