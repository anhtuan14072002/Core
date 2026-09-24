using UnityEngine;

namespace RogueliteToolkit.SkillTree.Authoring
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SkillTreeAuthoring : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private RectTransform _nodesRoot;
        [SerializeField] private RectTransform _connectionsRoot;
        [SerializeField] private SkillNodeAuthoring _nodePrefab;
        [SerializeField, Min(0.1f)] private float _connectionWidth = 6f;
        [SerializeField] private SkillTreeDefinition _output;

        public string Id => _id;
        public RectTransform NodesRoot => _nodesRoot;
        public RectTransform ConnectionsRoot => _connectionsRoot;
        public SkillNodeAuthoring NodePrefab => _nodePrefab;
        public float ConnectionWidth => Mathf.Max(0.1f, _connectionWidth);
        public SkillTreeDefinition Output => _output;

        public void SetOutput(SkillTreeDefinition output) => _output = output;

        private void OnValidate() => _id = _id?.Trim();
    }
}
