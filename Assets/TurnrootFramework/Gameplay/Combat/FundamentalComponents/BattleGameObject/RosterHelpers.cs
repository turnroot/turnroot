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

            // SINGLE SOURCE OF TRUTH: Only ApplyPreBattlePlacements sets positions
            res = ApplyPreBattlePlacements();
            if (!res.Success)
            {
                this.LogWarning($"InitializeBattleRosters: {res.ErrorMessage}");
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

                // CRITICAL: Ensure roster has NO template reference that would override our placements
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

        private OperationResult ApplyPreBattlePlacements()
        {
            try
            {
                var prep = Brain.battleBrain.PreparationObject;

                if (prep == null)
                {
                    return OperationResult.Successful(); // Continue without prep placements
                }

                // If no pre-battle placements exist yet, InitializePlacements
                if (prep.placements == null || prep.placements.Count == 0)
                {
                    var res = prep.InitializePlacements();
                    if (!res.Success)
                    {
                        return res;
                    }
                }

                if (prep.placements == null || prep.placements.Count == 0)
                {
                    return OperationResult.Successful();
                }

                var decoded = PreBattle.BattlePlacementSync.ToDecodedPlacementArray(
                    prep.placements
                );

                if (decoded.Length > 0)
                {
                    PlayerTeamRoster.ApplyDecodedPlacements(decoded);
                    $"BattleGameObject: ApplyPreBattlePlacements: Applied {decoded.Length} placements to PlayerTeamRoster".LogInfo();
                    // Notify systems that placements have been applied for this battle (cursor, UI, etc.)
                    Brain?.PublishPlacementsInitialized();
                }

                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"ApplyPreBattlePlacements failed: {ex.Message}");
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

            // CRITICAL: Only add CharacterInstance objects. Do NOT set roster reference.
            // Positions come ONLY from ApplyPreBattlePlacements.
            PlayerTeamRoster.AddInstances(playerInstance.Instances);

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
