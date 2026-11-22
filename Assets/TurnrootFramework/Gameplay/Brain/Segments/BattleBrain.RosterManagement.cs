using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        #region Roster Management API
        public GenericRosterInstance InstantiateGenericRoster(
            GenericRoster roster,
            bool register = false
        ) => Brain.gamewideContextBrain.GetOrCreateGenericRoster(roster, register);

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster()
        {
            if (_playerTeamRoster == null)
            {
                "BattleBrain.InstantiatePlayerTeamRoster: _playerTeamRoster is null! Make sure roster is initialized before starting battle.".LogError();
                return null;
            }

            if (Brain.gamewideContextBrain == null)
            {
                "BattleBrain.InstantiatePlayerTeamRoster: gamewideContextBrain is null!".LogError();
                return null;
            }

            var instance = Brain.gamewideContextBrain.GetOrCreatePlayerTeamRoster(
                _playerTeamRoster
            );
            if (instance == null)
            {
                $"BattleBrain.InstantiatePlayerTeamRoster: Failed to get/create roster instance for '{_playerTeamRoster.name}'".LogError();
            }

            return instance;
        }

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            Brain.gamewideContextBrain.RecallGenericRosters(rosters);

        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            Brain.gamewideContextBrain.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            Brain.gamewideContextBrain.GetAllActiveInstances();

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            Brain.gamewideContextBrain.SaveUniqueCharacterProgress(instance);

        #endregion
    }
}