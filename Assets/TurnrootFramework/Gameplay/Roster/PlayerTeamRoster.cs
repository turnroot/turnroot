using System;
using NaughtyAttributes;
using UnityEngine;
using static Turnroot.Characters.GenericRoster;

namespace Turnroot.Characters
{
    [CreateAssetMenu(
        fileName = "NewPlayerTeamRoster",
        menuName = "Turnroot/Characters/Player Team Roster"
    )]
    public class PlayerTeamRoster : Roster
    {
        [Serializable]
        public struct PlayerTeamRosterUnitPlacement
        {
            public CharacterData Character;

            public readonly bool IsAvatar => Character.Which == Components.CharacterWhich.AVATAR;
            public readonly bool IsAlly => Character.Which == Components.CharacterWhich.ALLY;
            public UnitStatus Status { get; private set; }

            public void SetStatus(UnitStatus newStatus) => Status = newStatus;

            public bool IsActiveRightNow { get; private set; }

            public void SetActiveRightNow(bool isActive) => IsActiveRightNow = isActive;

            public bool ChosenForThisBattle { get; private set; }

            public void SetChosenForThisBattle(bool isChosen) => ChosenForThisBattle = isChosen;
        }

        [ReorderableList]
        public PlayerTeamRosterUnitPlacement[] characters;
    }
}
