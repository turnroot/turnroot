using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Objects;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs
{
    public partial class EnemySupervisor : MonoBehaviour
    {
        [System.Serializable]
        public struct GenericEnemyStartingPlacement
        {
            public CharacterData Enemy;
            public Vector2Int StartingPosition;
            public GameplayPlayerSettings.DifficultyLevel MinimumDifficultyLevelToSpawn;

            [InfoBox(
                "If true, this enemy will be significantly stronger than the player team. They will also give a much larger XP reward. Use sparingly, generally save these for challenge battles and only have a couple there"
            )]
            public bool StrongEnemy;
            public ObjectItem StealableItem;
            public bool WeaponsAreStealable;

            public GenericEnemyStartingPlacement(CharacterData enemy, Vector2Int startingPosition)
            {
                Enemy = enemy;
                StartingPosition = startingPosition;
                MinimumDifficultyLevelToSpawn = GameplayPlayerSettings.DifficultyLevel.Easy;
                StrongEnemy = false;
                StealableItem = null;
                WeaponsAreStealable = false;
            }
        }

        [System.Serializable]
        public class GenericEnemyPlacementContainer
        {
            [InfoBox(
                "Generic enemies will have their stats, weapons, and classes changed at runtime, based on the battle conditions and player team. If you want an enemy to have a specific class or weapons, you MUST make them unique. Do not add unique enemies to this list"
            )]
            public GenericEnemyStartingPlacement[] placements;
        }

        public struct PlayerTeamDetails
        {
            public int PlayerTeamSize;
            public List<ProgressionLevel> PlayerTeamClassTiers;
            public int[] PlayerTeamLevels;
        }

        private OperationResult EnsureAndSpawnEnemyInstance(GenericEnemyStartingPlacement placement)
        {
            var ltm = BattleGameObject.Brain.ltm;

            var factory = new CharacterFactory(ltm);
            var created = factory.CreateOrRecall(placement.Enemy);
            if (created == null)
            {
                return OperationResult.Failure(
                    $"CharacterFactory failed to create instance for {placement.Enemy.DisplayName}"
                );
            }

            EnemyInstancesByStartingPlacement ??=
                new Dictionary<GenericEnemyStartingPlacement, CharacterInstance>();
            EnemyInstancesByStartingPlacement[placement] = created;

            var ctx = BattleGameObject.Context;
            if (ctx != null)
            {
                var spawned = ctx.SpawnAtPosition(created, placement.StartingPosition);
                if (!spawned)
                {
                    return OperationResult.Failure(
                        $"SpawnAtPosition failed for {created.CharacterTemplate.DisplayName} at {placement.StartingPosition}"
                    );
                }
            }

            return OperationResult.Successful();
        }

        // Overload: compute from an explicit list of CharacterInstance (used during precompute when the
        // per-battle `PlayerTeamRoster` may not yet be initialized but `BattleContext.Participants` is available).
        public PlayerTeamDetails ComputeCurrentPlayerTeamDetails(
            IEnumerable<CharacterInstance> instancesEnumerable
        )
        {
            var instances =
                instancesEnumerable?.Where(i => i != null).ToList()
                ?? new List<CharacterInstance>();

            Details = new PlayerTeamDetails
            {
                PlayerTeamSize = instances.Count,
                PlayerTeamClassTiers = new List<ProgressionLevel>(),
            };

            Details.PlayerTeamLevels = new int[Details.PlayerTeamSize];
            for (int i = 0; i < Details.PlayerTeamSize; i++)
            {
                var characterInstance = instances[i];
                Details.PlayerTeamLevels[i] = characterInstance?.CurrentLevel ?? 1;
                Details.PlayerTeamClassTiers.Add(
                    characterInstance?.CurrentClass?.ClassData?.Identity?.ClassTier
                        ?? ProgressionLevel.Base
                );
            }

            return Details;
        }
    }
}
