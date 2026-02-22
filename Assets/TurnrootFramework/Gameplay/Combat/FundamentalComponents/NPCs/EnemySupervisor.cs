using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs
{
    [RequireComponent(typeof(BattleGameObject))]
    /// <summary>
    /// Controls when and where enemies spawn. Handles pre-battle setup for enemy units, such as setting their initial positions and stats, and
    /// mid-battle reinforcement
    /// </summary>
    public partial class EnemySupervisor : MonoBehaviour
    {
        [HideInInspector]
        public BattleGameObject BattleGameObject => GetComponent<BattleGameObject>();

        [InfoBox(
            "Generic enemies will have their stats, weapons, and classes changed at runtime, based on the battle conditions and player team. If you want an enemy to have a specific class or weapons, you MUST make them unique. Do not add unique enemies to this list!"
        )]
        public GenericEnemyPlacementContainer GenericEnemyStartingPlacements;

        [HideInInspector]
        public Dictionary<
            GenericEnemyStartingPlacement,
            CharacterInstance
        > EnemyInstancesByStartingPlacement;

        [HideInInspector]
        public GameplayPlayerSettings.DifficultyLevel CurrentDifficulty;

        public PlayerTeamDetails Details;

        [
            InfoBox(
                "This should, generally speaking, slowly increase over the course of your game. At 1, enemies will be approximately the same level as the player team. At 2, they will be significantly stronger than the player team. Avoid large jumps unless you want a spike in difficulty for a particular battle. The number and details of enemies has an impact on difficulty, this multiplier is not the only factor in how challenging a battle is"
            ),
            Range(.9f, 1.6f)
        ]
        public float GenericEnemyDifficultyMultiplierForThisBattle = 1f;

        public PlayerTeamDetails ComputeCurrentPlayerTeamDetails(
            PlayerTeamRosterInstance CurrentPlayerTeamRosterInstance
        ) => ComputeCurrentPlayerTeamDetails(CurrentPlayerTeamRosterInstance?.Instances);

        public OperationResult InitializePreBattleEnemies()
        {
            var failures = new List<string>();

            foreach (var placement in GenericEnemyStartingPlacements.placements)
            {
                if (placement.MinimumDifficultyLevelToSpawn > CurrentDifficulty)
                {
                    continue;
                }

                if (placement.Enemy.IsUnique)
                {
                    TurnrootLogger.Log(
                        $"Enemy {placement.Enemy.DisplayName} is unique but is included in the EnemySupervisor's GenericEnemyStartingPlacements. Skipping this placement.",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                var createRes = EnsureAndSpawnEnemyInstance(placement);
                if (!createRes.Success)
                {
                    failures.Add(
                        createRes.ErrorMessage ?? $"Failed to create/spawn {placement.Enemy.name}"
                    );
                }
            }

            UpdateGenericEnemiesBasedOnPlayerTeamDetails();

            return failures.Count == 0
                ? OperationResult.Successful()
                : OperationResult.Failure(string.Join("; ", failures));
        }

        private Dictionary<CharacterInstance, int> CalculateAdjustedLevels()
        {
            var averagePlayerLevel =
                Details.PlayerTeamSize > 0
                    ? (int)
                        System.Math.Round(
                            System.Math.Round(
                                System.Linq.Enumerable.Average(Details.PlayerTeamLevels)
                            )
                        )
                    : 1;

            var h = GameplayPlayerSettings.Instance.GameDifficulty switch
            {
                GameplayPlayerSettings.DifficultyLevel.Easy => 3f,
                GameplayPlayerSettings.DifficultyLevel.Normal => 4f,
                GameplayPlayerSettings.DifficultyLevel.Hard => 5f,
                GameplayPlayerSettings.DifficultyLevel.Extreme => 6f,
                _ => 1f,
            };
            var highest =
                (averagePlayerLevel * 10)
                + System.Math.Ceiling(h * (GenericEnemyDifficultyMultiplierForThisBattle * 10));

            highest = (int)System.Math.Round(highest / 10f);

            var lowest =
                (System.Linq.Enumerable.Min(Details.PlayerTeamLevels) * 10)
                - System.Math.Ceiling(
                    (7f - h) * (GenericEnemyDifficultyMultiplierForThisBattle * 10)
                );
            lowest = (int)System.Math.Round(lowest / 10f);
            if (lowest < 1)
            {
                lowest = 1;
            }

            Dictionary<CharacterInstance, int> adjustedLevels =
                new Dictionary<CharacterInstance, int>();
            // Load deterministic per-battle seed from LTM (fallback to instance-based hash if absent)
            int battleSeed = 0;
            try
            {
                var prep = BattleGameObject.Brain.battleBrain.PreparationObject;
                var mapName = prep.MapGrid.MapName ?? "<unknown>";
                var battleKey =
                    prep != null ? $"{prep.name}.{mapName}" : BattleGameObject?.name ?? mapName;
                battleSeed =
                    BattleGameObject.Brain?.ltm?.RecallInt(LtmKeys.BattleSeedKey(battleKey)) ?? 0;
            }
            catch { }

            foreach (var kv in EnemyInstancesByStartingPlacement)
            {
                var instance = kv.Value;

                var localSkew =
                    GenericEnemyDifficultyMultiplierForThisBattle
                    - 1f
                    + DeterministicDouble(
                        GameplayGeneralSettings.Instance.GenericEnemySkewAdjustmentRange.x,
                        GameplayGeneralSettings.Instance.GenericEnemySkewAdjustmentRange.y,
                        battleSeed,
                        instance?.Id ?? instance?.CharacterTemplate?.name ?? ""
                    );

                if (localSkew <= 0)
                {
                    var modLowest = DeterministicDouble(
                        (float)lowest,
                        averagePlayerLevel,
                        battleSeed,
                        instance?.Id ?? instance?.CharacterTemplate?.name ?? ""
                    );
                    adjustedLevels[instance] = (int)modLowest;
                }
                else
                {
                    var modHighest = DeterministicDouble(
                        averagePlayerLevel,
                        (float)highest,
                        battleSeed,
                        instance?.Id ?? instance?.CharacterTemplate?.name ?? ""
                    );
                    adjustedLevels[instance] = (int)modHighest;
                }
            }
            return adjustedLevels;
        }

        private void UpdateGenericEnemiesBasedOnPlayerTeamDetails()
        {
            // Post-spawn adjustments go here
            var adjustedLevels = CalculateAdjustedLevels();

            var appearance = BattleGameObject.Brain.unitAppearanceBrain;
            foreach (var kv in EnemyInstancesByStartingPlacement)
            {
                var placement = kv.Key;
                var instance = kv.Value;

                var spawnRes = appearance.PrecomputeSpawnModelAt(
                    instance,
                    placement.StartingPosition,
                    prebattle: false
                );
                if (!spawnRes.Success)
                {
                    TurnrootLogger.Log(
                        $"EnemySupervisor: UnitAppearance precompute failed for {instance.Id}: {spawnRes.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
        }
    }
}
