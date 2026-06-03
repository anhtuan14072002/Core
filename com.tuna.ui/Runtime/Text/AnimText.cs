using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;

namespace Core
{
    public static class AnimText
    {
        public static async UniTask AnimateNumberChange(TextMeshProUGUI textComponent, int startValue, int endValue,
            float duration, float frequency, GameObject icon)
        {
            float elapsedTime = 0;
            var startColor = Color.white;
            var targetColor = Color.red;
            float tweenInterval = 0.2f;
            float lastTweenTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / duration;
                var currentValue = (int)Mathf.Lerp(startValue, endValue, progress);
                textComponent.text = currentValue.ToString();

                if (endValue < startValue)
                {
                    var colorLerp = (Mathf.Sin(Time.time * 10f) + 1f) * 0.5f;
                    textComponent.color = Color.Lerp(startColor, targetColor, colorLerp);
                }
                else if (endValue > startValue)
                {
                    if (Time.time - lastTweenTime >= tweenInterval)
                    {
                        if (icon == null) return;
                        Tween.PunchScale(icon.transform, new Vector3(0.35f, 0.35f), 0.1f, frequency);
                        lastTweenTime = Time.time;
                    }
                }
                await UniTask.Yield();
            }

            textComponent.text = endValue.ToString();
            if (endValue < startValue)
            {
                textComponent.color = Color.white;
            }
        }
    }
}