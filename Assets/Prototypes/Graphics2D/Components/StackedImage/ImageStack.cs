using System.Collections.Generic;
using Assets.Prototypes.Characters;
using UnityEngine;

namespace Assets.Prototypes.Graphics.Portrait
{
    [CreateAssetMenu(fileName = "NewImageStack", menuName = "Turnroot/Graphics/ImageStack")]
    public class ImageStack : ScriptableObject
    {
        [SerializeField]
        private CharacterData _ownerCharacter;

        [SerializeField]
        private List<ImageStackLayer> _layers = new();

        public List<ImageStackLayer> Layers => _layers;
        public CharacterData OwnerCharacter => _ownerCharacter;
    }
}
