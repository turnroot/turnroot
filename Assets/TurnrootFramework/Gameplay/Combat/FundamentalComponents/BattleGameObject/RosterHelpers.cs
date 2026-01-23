using System.Collections.Generic;
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

            res = InitializeRuntimePlacements();
            if (!res.Success)
            {
                TurnrootLogger.Log(
                    $"BattleGameObject.InitializeBattleRosters: {res.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            res = ApplyPreBattlePlacements();
            if (!res.Success)
            {
                TurnrootLogger.Log(
                    $"BattleGameObject.InitializeBattleRosters: {res.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
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

                if (EnemyTeamRoster == null)
                {
                    var go = new GameObject("BattleRoster - Enemy Team");
                    go.transform.SetParent(transform);
                    EnemyTeamRoster = go.AddComponent<GenericRosterInstance>();
                }
                else
                {
                    EnemyTeamRoster.Clear();
                }

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

        private OperationResult InitializeRuntimePlacements()
        {
            try
            {
                var persistentPlayer =
                    Brain?.gamewideContextBrain?.GetPersistentPlayerTeamRosterInstance();
                if (persistentPlayer != null)
                {
                    PlayerTeamRoster.ApplyDecodedPlacements(persistentPlayer.GetPlacements());
                }
                else
                {
                    PlayerTeamRoster.InitializeRuntimePlacementsFromTemplate();
                }

                EnemyTeamRoster?.InitializeRuntimePlacementsFromTemplate();
                ThirdPartyTeamRoster?.InitializeRuntimePlacementsFromTemplate();

                return OperationResult.Successful();
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"InitializeRuntimePlacements failed: {ex.Message}");
            }
        }

        private OperationResult ApplyPreBattlePlacements()
        {
            try
            {
                var prep = Brain?.battleBrain?.PreparationObject;
                // If no pre-battle placements exist yet, InitializePlacements
                if (prep != null && (prep.placements == null || prep.placements.Count == 0))
                {
                    var res = prep.InitializePlacements();
                    if (!res.Success)
                    {
                        return res;
                    }
                }

                {
                    var list = new List<Characters.Roster.UnitPlacement>();
                    foreach (var kvp in prep.placements)
                    {
                        var pos = kvp.Key;
                        var inst = kvp.Value;
                        if (inst == null || inst.CharacterTemplate == null)
                        {
                            continue;
                        }

                        var up = new Characters.Roster.UnitPlacement
                        {
                            CharacterData = inst.CharacterTemplate,
                            SpawnPosition = pos,
                            Order = list.Count,
                        };
                        up.SetStatus(Characters.Roster.UnitStatus.NotSpawned);
                        up.SetActiveRightNow(true);

                        list.Add(up);
                    }

                    if (list.Count > 0)
                    {
                        PlayerTeamRoster.ApplyDecodedPlacements(list.ToArray());
                        TurnrootLogger.Log(
                            $"BattleGameObject: Applied PreBattle placements to PlayerTeamRoster ({list.Count})"
                        );
                    }
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
            if (battleBrain == null)
            {
                return OperationResult.Failure("Brain or battleBrain is null");
            }

            var playerInstance = battleBrain.InstantiatePlayerTeamRoster();
            if (playerInstance == null)
            {
                return OperationResult.Failure("Could not instantiate player team roster");
            }

            PlayerTeamRoster.AddInstances(playerInstance.Instances);

            if (_enemyRoster != null)
            {
                var enemyInstance = battleBrain.InstantiateGenericRoster(_enemyRoster);
                EnemyTeamRoster.AddInstances(enemyInstance.Instances);
            }

            if (HasThirdParty && _thirdPartyRoster != null)
            {
                var thirdPartyInstance = battleBrain.InstantiateGenericRoster(_thirdPartyRoster);
                ThirdPartyTeamRoster.AddInstances(thirdPartyInstance.Instances);
            }

            return OperationResult.Successful();
        }

        public OperationResult ClearBattleRosters()
        {
            try
            {
                PlayerTeamRoster?.Clear();
                EnemyTeamRoster?.Clear();
                ThirdPartyTeamRoster?.Clear();
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
