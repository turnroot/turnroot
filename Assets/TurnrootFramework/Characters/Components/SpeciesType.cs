using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Defines a species/race type (e.g., Human, Beast, Dragon, Manakete).
    /// Used for class restrictions and gameplay mechanics.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SpeciesType",
        menuName = "Turnroot/Game Settings/Characters/Species Type"
    )]
    [System.Serializable]
    public class SpeciesType : ScriptableObject
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private string _id;

        [SerializeField, TextArea(2, 4)]
        private string _description;

        [SerializeField]
        private Sprite _icon;

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public Sprite Icon
        {
            get => _icon;
            set => _icon = value;
        }

        public override string ToString() => _name;
    }
}
