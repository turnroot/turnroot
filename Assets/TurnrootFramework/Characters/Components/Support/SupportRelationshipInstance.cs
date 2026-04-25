using System;
using Newtonsoft.Json;
using Turnroot.Characters.Subclasses;
using UnityEngine;

namespace Turnroot.Characters.Components.Support
{
    /// <summary>
    /// Represents an instance of a support relationship between characters with progression tracking.
    /// </summary>
    [Serializable]
    public class SupportRelationshipInstance
    {
        [SerializeField, JsonProperty("_character")]
        private CharacterData _character;

        [SerializeField, JsonProperty("_supportLevels")]
        private SupportLevels _supportLevels;

        [SerializeField, JsonProperty("_maxLevel")]
        private string _maxLevel;

        [SerializeField, JsonProperty("_supportSpeed")]
        private int _supportSpeed = 1;

        [SerializeField, JsonProperty("_supportGainMultiplier")]
        private float _supportGainMultiplier = 1f;

        [SerializeField, JsonProperty("_supportPoints")]
        private float _supportPoints = 0f;

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

        public SupportRelationshipInstance(
            SupportRelationshipTable.SupportPairing pairing,
            CharacterData partner
        )
        {
            _character = partner;
            _supportLevels = new SupportLevels { Value = "E" };
            _maxLevel = pairing.MaxSupportLevel?.Value ?? "A";
            _supportSpeed = 1;
            _supportGainMultiplier =
                pairing.SupportGainMultiplier > 0f ? pairing.SupportGainMultiplier : 1f;
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
        public float SupportPoints
        {
            get => _supportPoints;
            set => _supportPoints = value;
        }

        public string CurrentLevel
        {
            get => _supportLevels.Value;
            set
            {
                _supportLevels ??= new SupportLevels();

                _supportLevels.Value = value;
            }
        }

        public void Increase(float points)
        {
            _supportPoints += points * _supportSpeed * _supportGainMultiplier;
            while (_supportPoints >= 100f)
            {
                _supportLevels.Increase();
                _supportPoints -= 100f;
            }
        }

        public void Decrease(float points)
        {
            _supportPoints -= points;

            if (_supportPoints < 0f)
            {
                _supportPoints = 0f;
            }
        }

        /// <summary>
        /// Subtracts 100 points and advances the support level by one rank.
        /// Call only when <see cref="SupportPoints"/> >= 100.
        /// Fires <see cref="Brain.PublishSupportLevelIncreased"/> via the conversational brain.
        /// </summary>
        public void IncreaseSupportLevel(
            CharacterInstance owner,
            Gameplay.Brain.ConversationalBrain conversationalBrain
        )
        {
            _supportPoints -= 100f;
            if (_supportPoints < 0f)
            {
                _supportPoints = 0f;
            }

            _supportLevels.Increase();
            conversationalBrain?.NotifySupportLevelIncreased(owner, this);
        }
    }
}
