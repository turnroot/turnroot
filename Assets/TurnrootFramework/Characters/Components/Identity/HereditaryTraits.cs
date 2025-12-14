using System;
using UnityEngine;

namespace Turnroot.Characters.Configuration
{
    [Serializable]
    public class HereditaryTraits
    {
        [SerializeField]
        private bool _hairColor = true;

        [SerializeField]
        private bool _faceShape = false;

        [SerializeField]
        private bool _eyeColor = true;

        [SerializeField]
        private bool _skinColor = true;

        [SerializeField]
        private bool _height = true;

        [SerializeField]
        private bool _aptitudes = false;

        [SerializeField]
        private bool _statGrowths = false;

        public bool HairColor
        {
            get => _hairColor;
            set => _hairColor = value;
        }
        public bool FaceShape
        {
            get => _faceShape;
            set => _faceShape = value;
        }
        public bool EyeColor
        {
            get => _eyeColor;
            set => _eyeColor = value;
        }
        public bool SkinColor
        {
            get => _skinColor;
            set => _skinColor = value;
        }
        public bool Height
        {
            get => _height;
            set => _height = value;
        }
        public bool Aptitudes
        {
            get => _aptitudes;
            set => _aptitudes = value;
        }
        public bool StatGrowths
        {
            get => _statGrowths;
            set => _statGrowths = value;
        }
    }
}
