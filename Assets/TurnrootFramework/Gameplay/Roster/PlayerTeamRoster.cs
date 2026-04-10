using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Turnroot.Characters
{
    /// <summary>
    /// ScriptableObject roster definition for the player's team of characters.
    /// </summary>
    [CreateAssetMenu(
        fileName = "NewPlayerTeamRoster",
        menuName = "Turnroot/Characters/Player Team Roster"
    )]
    public class PlayerTeamRoster : Roster
    {
        /// <summary>
        /// Unit placement entry for player team roster with battle selection tracking.
        /// </summary>
        [Serializable]
        public class PlayerTeamRosterUnitPlacement : UnitPlacement
        {
            public bool IsAvatar => CharacterData.Which == Components.CharacterWhich.AVATAR;
            public bool IsAlly => CharacterData.Which == Components.CharacterWhich.ALLY;

            public bool ChosenForThisBattle { get; private set; }

            public void SetChosenForThisBattle(bool isChosen) => ChosenForThisBattle = isChosen;
        }

        [ReorderableList]
        [FormerlySerializedAs("characters")]
        [SerializeField]
        private PlayerTeamRosterUnitPlacement[] _playerCharacters;

        public override UnitPlacement[] characters
        {
            get => _playerCharacters;
            set
            {
                if (value == null)
                {
                    _playerCharacters = null;
                    return;
                }

                // Fast path: if the incoming array is already the specialized type, cast it.
                _playerCharacters = value as PlayerTeamRosterUnitPlacement[];
                if (_playerCharacters != null)
                {
                    return;
                }

                // Otherwise convert elements individually
                _playerCharacters = new PlayerTeamRosterUnitPlacement[value.Length];
                for (int i = 0; i < value.Length; i++)
                {
                    var v = value[i];
                    if (v is PlayerTeamRosterUnitPlacement p)
                    {
                        _playerCharacters[i] = p;
                    }
                    else if (v != null)
                    {
                        var copy = new PlayerTeamRosterUnitPlacement
                        {
                            CharacterData = v.CharacterData,
                            SpawnPosition = v.SpawnPosition,
                            Order = v.Order,
                        };
                        copy.SetStatus(v.Status);
                        copy.SetActiveRightNow(v.IsActiveRightNow);
                        _playerCharacters[i] = copy;
                    }
                }
            }
        }

        public void AddCharacter(CharacterData characterData)
        {
            var newUnit = new PlayerTeamRosterUnitPlacement
            {
                CharacterData = characterData,
                SpawnPosition = Vector2Int.zero,
                Order = _playerCharacters != null ? _playerCharacters.Length : 0,
            };

            var tempList = new List<PlayerTeamRosterUnitPlacement>(_playerCharacters) { newUnit };
            _playerCharacters = tempList.ToArray();
        }
    }
}
