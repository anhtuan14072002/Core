using UnityEngine;

namespace RogueliteToolkit.Resources
{
    [CreateAssetMenu(fileName = "Resource", menuName = "Roguelite Toolkit/Resource Definition")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private int _id;
        [SerializeField] private string _displayName;
        [SerializeField] private Sprite _icon;

        public int Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public Sprite Icon => _icon;
    }
}
