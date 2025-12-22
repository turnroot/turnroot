using System;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "NewPlayerTeamRoster",
        menuName = "Turnroot/Characters/Player Team Roster"
    )]
    public class PlayerTeamRoster : Roster
    {
        [Serializable]
        public class PlayerTeamRosterUnitPlacement : UnitPlacement
        {
            public bool IsAvatar => CharacterData.Which == Components.CharacterWhich.AVATAR;
            public bool IsAlly => CharacterData.Which == Components.CharacterWhich.ALLY;

            public bool ChosenForThisBattle { get; private set; }

            public void SetChosenForThisBattle(bool isChosen) => ChosenForThisBattle = isChosen;
        }

        [ReorderableList]
        public new PlayerTeamRosterUnitPlacement[] characters;
    }
}
