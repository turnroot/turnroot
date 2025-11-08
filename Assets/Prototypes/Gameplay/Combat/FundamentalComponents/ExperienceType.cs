using Assets.Prototypes.Gameplay.Objects.Components;
using UnityEngine;

namespace Assets.Prototypes.Gameplay.Combat.FundamentalComponents
{
    [System.Serializable]
    public class ExperienceType
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private string _id;

        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private WeaponType _associatedWeaponType;

        [SerializeField]
        private bool _hasWeaponType;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                // Auto-generate ID from name
                _id = string.IsNullOrEmpty(value) ? "" : value.ToLower().Replace(" ", "");
            }
        }

        public string Id
        {
            get => _id;
            private set => _id = value;
        }

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public WeaponType AssociatedWeaponType
        {
            get => _associatedWeaponType;
            set => _associatedWeaponType = value;
        }

        public bool HasWeaponType
        {
            get => _hasWeaponType;
            set => _hasWeaponType = value;
        }
    }
}
