using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [CreateAssetMenu(fileName = "EquipmentType", menuName = "Roguelite Toolkit/Equipment/Type")]
    public sealed class EquipmentType : ScriptableObject
    {
        [SerializeField] private Sprite _icon;
        public Sprite Icon => _icon;
    }
}
