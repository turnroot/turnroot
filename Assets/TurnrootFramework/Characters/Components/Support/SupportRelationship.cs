using System;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using UnityEngine;

namespace Turnroot.Characters.Components.Support
{
    [Serializable]
    public class SupportRelationship
    {
        [SerializeField]
        private CharacterData _character;

        [SerializeField]
        private SupportLevels _supportLevels = new();

        [SerializeField]
        private string _maxLevel;

        [SerializeField]
        private int _supportSpeed = 1;

        public CharacterData Character
        {
            get => _character;
            set => _character = value;
        }

        public SupportLevels SupportLevels => _supportLevels;

        public string MaxLevel
        {
            get => _maxLevel;
            set => _maxLevel = value;
        }

        public int SupportSpeed
        {
            get => _supportSpeed;
            set => _supportSpeed = value;
        }
    }
}
