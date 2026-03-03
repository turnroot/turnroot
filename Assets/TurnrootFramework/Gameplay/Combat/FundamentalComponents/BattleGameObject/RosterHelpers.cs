using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat
{
    public partial class BattleGameObject : MonoBehaviour
    {
        #region Roster Management

        public void InitializeBattleRosters()
        {
            var res = EnsureRostersExist();
            if (!res.Success)
            {
                return;
            }
        }

        private OperationResult EnsureRostersExist()
        {
            try
            {
                if (PlayerTeamRoster == null)
                {
                    var go = new GameObject("BattleRoster - Player Team");
                    go.transform.SetParent(transform);
                    PlayerTeamRoster = go.AddComponent<PlayerTeamRosterInstance>();
                }
                else
                {
                    PlayerTeamRoster.Clear();
                }

                // Don't set roster template - positions come from placements only
                PlayerTeamRoster.roster = null;

                if (HasThirdParty)
                {
                    if (ThirdPartyTeamRoster == null)
                    {
                        var go = new GameObject("BattleRoster - Third Party Team");
                        go.transform.SetParent(transform);
                        ThirdPartyTeamRoster = go.AddComponent<GenericRosterInstance>();
                    }
                    else
                    {
                        ThirdPartyTeamRoster.Clear();
                    }
                }

                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"EnsureRostersExist failed: {ex.Message}");
            }
        }

        public OperationResult PopulateBattleRostersFromTemplates()
        {
            var battleBrain = Brain?.battleBrain;
            var validation = OperationResultGuards.RequireNotNull(battleBrain, nameof(battleBrain));
            if (!validation.Success)
            {
                return validation;
            }

            var playerInstance = battleBrain.InstantiatePlayerTeamRoster();
            if (playerInstance == null)
            {
                return OperationResult.Failure("Could not instantiate player team roster");
            }

            // CRITICAL: Create battle copies of ONLY selected units
            // This decouples battle roster from persistent roster
            var selectedUnits = playerInstance
                .Instances.Where(inst => inst != null && inst.IsSelectedForBattle)
                .ToList();

            var battleCopies = new List<CharacterInstance>();
            foreach (var unit in selectedUnits)
            {
                battleCopies.Add(unit.CreateBattleCopy());
            }

            PlayerTeamRoster.AddInstances(battleCopies);

            $"PopulateBattleRostersFromTemplates: Created {battleCopies.Count} battle copies from {playerInstance.Instances.Count} persistent roster units".LogInfo();

            if (HasThirdParty && _thirdPartyRoster != null)
            {
                var thirdPartyInstance = battleBrain.InstantiateGenericRoster(_thirdPartyRoster);
                if (thirdPartyInstance != null)
                {
                    ThirdPartyTeamRoster.roster = thirdPartyInstance.roster;
                    ThirdPartyTeamRoster.AddInstances(thirdPartyInstance.Instances);
                }
            }

            return OperationResult.Successful();
        }

        public OperationResult ClearBattleRosters()
        {
            try
            {
                PlayerTeamRoster.Clear();
                ThirdPartyTeamRoster.Clear();
                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"ClearBattleRosters failed: {ex.Message}");
            }
        }

        #endregion
    }
}
