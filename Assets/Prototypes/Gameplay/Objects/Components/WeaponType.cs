using UnityEngine;

namespace Assets.Prototypes.Gameplay.Objects.Components
{
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
            get => _trianglePosition;
            set => _trianglePosition = value;
        }
    }
}
