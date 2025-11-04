using System;
using UnityEngine;
using Assets.Prototypes.Characters;

namespace Assets.Prototypes.Characters.Subclasses
{
    [Serializable]
    public class SupportRelationship
    {
        [SerializeField]
        private Character _character;

        [SerializeField]
        private SupportLevels.Level _currentLevel = SupportLevels.Level.None;

        [SerializeField]
        private SupportLevels.Level _maxLevel = SupportLevels.Level.S;

        [SerializeField]
        private int _supportPoints = 0;

        [SerializeField]
        private int _supportSpeed = 1;

        public Character Character
        {
            get => _character;
            set => _character = value;
        }

        public SupportLevels.Level CurrentLevel
        {
            get => _currentLevel;
            set => _currentLevel = value;
        }

        public SupportLevels.Level MaxLevel
        {
            get => _maxLevel;
            set => _maxLevel = value;
        }

        public int SupportPoints
        {
            get => _supportPoints;
            set => _supportPoints = value;
        }

        public int SupportSpeed
        {
            get => _supportSpeed;
            set => _supportSpeed = value;
        }
    }
}
