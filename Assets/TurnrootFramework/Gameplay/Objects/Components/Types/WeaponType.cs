using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    /// <summary>
    /// Defines a weapon type with its properties including name, icon, and triangle position.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeaponType",
        menuName = "Turnroot/Game Settings/Gameplay/Weapon Type"
    )]
    [System.Serializable]
    public class WeaponType : ScriptableObject
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private string _id;

        [SerializeField]
        private bool _isMagic;

        [HideInInspector]
        [SerializeField]
        private TrianglePosition _trianglePosition;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public Sprite Icon
        {
            get => _icon;
            set => _icon = value;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public bool IsMagic
        {
            get => _isMagic;
            set => _isMagic = value;
        }

        public TrianglePosition TrianglePosition
        {
            get
            {
                var settings = GameplayGeneralSettings.Instance;
                if (settings != null && !string.IsNullOrEmpty(Id))
                {
                    var pos = settings.GetWeaponTrianglePosition(Id);
                    return new TrianglePosition(pos);
                }
                return new TrianglePosition(TrianglePositionEnum.NotOnTriangle);
            }
            set => _trianglePosition = value;
        }

        public override string ToString() => Name;
    }
}
