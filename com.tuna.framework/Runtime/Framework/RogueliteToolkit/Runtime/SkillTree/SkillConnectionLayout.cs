using UnityEngine;

namespace RogueliteToolkit.SkillTree
{
    public static class SkillConnectionLayout
    {
        public static void Apply(
            RectTransform line, RectTransform from, RectTransform to)
        {
            if (line == null || from == null || to == null)
                return;

            Vector2 start = from.anchoredPosition;
            Vector2 end = to.anchoredPosition;
            Vector2 delta = end - start;
            line.anchoredPosition = (start + end) * 0.5f;
            line.sizeDelta = new Vector2(delta.magnitude, line.sizeDelta.y);
            line.localRotation = Quaternion.Euler(
                0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
