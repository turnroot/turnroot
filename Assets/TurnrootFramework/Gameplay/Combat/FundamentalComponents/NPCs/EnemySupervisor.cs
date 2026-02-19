using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.PlayerSettings;
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
            Range(0.75f, 2f)
        ]
        public float GenericEnemyDifficultyMultiplierForThisBattle = 1.05f;

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

                if (!placement.Enemy.IsUnique)
                {
                    TurnrootLogger.Log(
                        $"Enemy {placement.Enemy.DisplayName} is not unique but is included in the EnemySupervisor's GenericEnemyStartingPlacements. Skipping this placement.",
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

        private void UpdateGenericEnemiesBasedOnPlayerTeamDetails()
        {
            // Post-spawn adjustments go here

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
