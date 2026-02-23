using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
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
                    $"Enemy {placement.Enemy.DisplayName} is unique but is included in the EnemySupervisor's GenericEnemyStartingPlacements. Skipping this placement.".LogWarning();
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
                    ? (int)Math.Round(Math.Round(Enumerable.Average(Details.PlayerTeamLevels)))
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
                + Math.Ceiling(h * (GenericEnemyDifficultyMultiplierForThisBattle * 10));

            highest = (int)Math.Round(highest / 10f);

            var lowest =
                (Enumerable.Min(Details.PlayerTeamLevels) * 10)
                - Math.Ceiling((7f - h) * (GenericEnemyDifficultyMultiplierForThisBattle * 10));
            lowest = (int)Math.Round(lowest / 10f);
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

            // if a character template supplies a class progression ladder, choose
            // the most advanced class whose minimum level requirement does not
            // exceed the unit's adjusted battle level and apply it.  afterwards roll
            // level-ups to push the instance to that level using the chosen class's
            // growth rates.
            foreach (var kv in EnemyInstancesByStartingPlacement)
            {
                var instance = kv.Value;
                if (instance == null)
                {
                    continue;
                }

                int adjustedLevel = adjustedLevels.ContainsKey(instance)
                    ? adjustedLevels[instance]
                    : instance.CurrentLevel;

                if (instance.CharacterTemplate?.UseClassProgressionLadder == true)
                {
                    // determine the best-fit class and record the tier used
                    CharacterClassData selected = null;
                    ProgressionLevel tierUsed = ProgressionLevel.Starter;
                    int bestReq = -1;
                    var ladder = instance.CharacterTemplate.ProgressionLadder;
                    foreach (ProgressionLevel tier in Enum.GetValues(typeof(ProgressionLevel)))
                    {
                        var candidate = ladder.GetClassForTier(tier);
                        if (candidate == null)
                        {
                            continue;
                        }
                        int req = candidate.Requirements?.MinimumLevelRequirement ?? 1;
                        if (req <= adjustedLevel && req > bestReq)
                        {
                            bestReq = req;
                            selected = candidate;
                            tierUsed = tier;
                        }
                    }

                    if (selected != null && selected != instance.CurrentClass?.ClassData)
                    {
                        instance.ChangeClass(selected, applyClassChangeBonuses: false);
                    }

                    // level up to adjustedLevel (rolling growths each time)
                    int delta = adjustedLevel - instance.CurrentLevel;
                    for (int lev = 0; lev < delta; lev++)
                    {
                        instance.LevelUp();
                    }

                    // spawn any loadout items for the chosen tier
                    var loadout = instance.CharacterTemplate.GetLoadoutForProgression(tierUsed);
                    if (loadout != null && loadout.Count > 0)
                    {
                        foreach (var entry in loadout)
                        {
                            if (entry.Item == null)
                            {
                                continue;
                            }

                            if (UnityEngine.Random.Range(0f, 100f) <= entry.Chance)
                            {
                                var itemInst = new Turnroot.Gameplay.Objects.ObjectItemInstance(
                                    entry.Item
                                );
                                instance.InventoryInstance?.AddToInventory(itemInst);
                                // optional equip heuristics
                                if (
                                    itemInst.Template?.Subtype
                                        == Objects.Components.ObjectSubtype.Weapon
                                    && instance.InventoryInstance != null
                                )
                                {
                                    instance.InventoryInstance.EquipItem(itemInst.Slot);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // todo: consider adjusting level/stats for non‑ladder templates
                }
            }

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
                    $"EnemySupervisor: UnitAppearance precompute failed for {instance.Id}: {spawnRes.ErrorMessage}".LogWarning();
                }
            }
        }
    }
}
