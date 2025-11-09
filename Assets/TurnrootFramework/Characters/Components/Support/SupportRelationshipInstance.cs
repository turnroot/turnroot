using System;
using Turnroot.Characters;
using Turnroot.Characters.Subclasses;
using UnityEngine;

namespace Turnroot.Characters.Components.Support
{
    [Serializable]
    public class SupportRelationshipInstance
    {
        [SerializeField]
        private CharacterData _character;

        [SerializeField]
        private SupportLevels _supportLevels;

        [SerializeField]
        private string _maxLevel;

        [SerializeField]
        private int _supportSpeed = 1;

        [SerializeField]
        private int _supportPoints = 0;

        public SupportRelationshipInstance()
        {
            _supportLevels = new SupportLevels { Value = "E" };
            _maxLevel = "A";
            _supportSpeed = 1;
        }

        public SupportRelationshipInstance(SupportRelationship template)
        {
            _character = template.Character;
            _supportLevels = new SupportLevels { Value = template.SupportLevel.Value };
            _maxLevel = template.MaxLevel;
            _supportSpeed = template.SupportSpeed;
        }

        public CharacterData Character
        {
            get => _character;
            set => _character = value;
        }
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
        public int SupportPoints
        {
            get => _supportPoints;
            set => _supportPoints = value;
        }

        public string CurrentLevel => _supportLevels.Value;

        public void Increase(int points)
        {
            _supportPoints += points * _supportSpeed;
            if (_supportPoints >= 100)
                _supportLevels.Increase();
        }

        public void Decrease(int points)
        {
            _supportPoints -= points;

            if (_supportPoints < 0)
                _supportPoints = 0;
        }
    }
}
