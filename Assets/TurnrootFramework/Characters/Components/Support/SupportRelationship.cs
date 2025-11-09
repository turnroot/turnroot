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
        private SupportLevels _supportLevel;

        [SerializeField]
        private SupportLevels _maxLevel;

        [SerializeField, Range(1, 5)]
        private int _supportSpeed = 1;

        public SupportRelationship()
        {
            _supportLevel = new SupportLevels { Value = "E" };
            _maxLevel = new SupportLevels { Value = "A" };
            _supportSpeed = 1;
        }

        public CharacterData Character
        {
            get => _character;
            set => _character = value;
        }

        public SupportLevels SupportLevel
        {
            get
            {
                if (_supportLevel == null)
                    _supportLevel = new SupportLevels { Value = "E" };
                return _supportLevel;
            }
        }

        public string MaxLevel
        {
            get
            {
                if (_maxLevel == null)
                    _maxLevel = new SupportLevels { Value = "A" };
                return _maxLevel.Value;
            }
            set => _maxLevel = new SupportLevels() { Value = value };
        }

        public int SupportSpeed
        {
            get => _supportSpeed > 0 ? _supportSpeed : 1;
            set => _supportSpeed = Mathf.Clamp(value, 1, 5);
        }

        public void InitializeDefaults()
        {
            if (_supportLevel == null)
                _supportLevel = new SupportLevels { Value = "E" };
            if (_maxLevel == null)
                _maxLevel = new SupportLevels { Value = "A" };
            if (_supportSpeed <= 0)
                _supportSpeed = 1;
        }
    }
}
