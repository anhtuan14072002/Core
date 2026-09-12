using System;
using UnityEngine;
using UnityEngine.UI;

namespace RogueliteToolkit.SkillTree.Authoring
{
    public enum SkillConnectionCondition
    {
        Unlocked = 0,
        MinimumLevel = 1
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class SkillConnectionAuthoring : MonoBehaviour
    {
        [SerializeField] private SkillNodeAuthoring _from;
        [SerializeField] private SkillNodeAuthoring _to;
        [SerializeField] private SkillConnectionCondition _condition;
        [SerializeField, Min(1)] private int _minimumLevel = 1;

        [NonSerialized] private Vector2 _lastFromPosition;
        [NonSerialized] private Vector2 _lastToPosition;
        [NonSerialized] private bool _hasPreviewCache;

        public SkillNodeAuthoring From => _from;
        public SkillNodeAuthoring To => _to;
        public SkillConnectionCondition Condition => _condition;
        public int MinimumLevel => Mathf.Max(1, _minimumLevel);

        public void Configure(
            SkillNodeAuthoring from,
            SkillNodeAuthoring to,
            SkillConnectionCondition condition,
            int minimumLevel)
        {
            _from = from;
            _to = to;
            _condition = condition;
            _minimumLevel = Mathf.Max(1, minimumLevel);
            _hasPreviewCache = false;
            RefreshPreview();
        }

        private void OnEnable()
        {
            _hasPreviewCache = false;
            RefreshPreview();
        }

        private void OnValidate()
        {
            _hasPreviewCache = false;
            RefreshPreview();
        }
        private void Update() => RefreshPreview();

        private void RefreshPreview()
        {
            _minimumLevel = Mathf.Max(1, _minimumLevel);
            if (_from == null || _to == null)
                return;
            Vector2 fromPosition = _from.RectTransform.anchoredPosition;
            Vector2 toPosition = _to.RectTransform.anchoredPosition;
            if (_hasPreviewCache &&
                (_lastFromPosition - fromPosition).sqrMagnitude < 0.0001f &&
                (_lastToPosition - toPosition).sqrMagnitude < 0.0001f)
                return;
            _lastFromPosition = fromPosition;
            _lastToPosition = toPosition;
            _hasPreviewCache = true;
            SkillConnectionLayout.Apply(
                (RectTransform)transform, _from.RectTransform, _to.RectTransform);
        }
    }
}
