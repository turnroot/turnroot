using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs
{
    public partial class EnemySupervisor : MonoBehaviour
    {
        private static readonly Vector2Int[] AdjacentSpawnOffsets =
        {
            new(-1, -1),
            new(-1, 0),
            new(-1, 1),
            new(0, -1),
            new(0, 1),
            new(1, -1),
            new(1, 1),
        };

        [System.Serializable]
        public struct GenericEnemyStartingPlacement
        {
            public CharacterData Enemy;
            public Vector2Int StartingPosition;

            [InfoBox(
                "If true, and the starting position is occupied by another unit, this enemy will attempt to spawn on an adjacent tile instead. If false, the enemy will not spawn if the starting position is occupied"
            )]
            public bool SpawnOnAdjacentIfOccupied;
            public GameplayPlayerSettings.DifficultyLevel MinimumDifficultyLevelToSpawn;

            [InfoBox(
                "If set to a positive number, this enemy will respawn every N turns. If set to -1, it will not respawn."
            )]
            public int SpawnEveryNTurns;

            public GenericEnemyStartingPlacement(CharacterData enemy, Vector2Int startingPosition)
            {
                Enemy = enemy;
                StartingPosition = startingPosition;
                MinimumDifficultyLevelToSpawn = GameplayPlayerSettings.DifficultyLevel.Easy;
                SpawnEveryNTurns = -1;
                SpawnOnAdjacentIfOccupied = false;
            }
        }

        [System.Serializable]
        public class GenericEnemyPlacementContainer
        {
            public GenericEnemyStartingPlacement[] placements;
        }

        [System.Serializable]
        public struct UniqueEnemyStartingPlacement
        {
            public CharacterData Enemy;
            public Vector2Int StartingPosition;

            [InfoBox(
                "If true, and the starting position is occupied by another unit, this enemy will attempt to spawn on an adjacent tile instead. If false, the enemy will not spawn if the starting position is occupied"
            )]
            public bool SpawnOnAdjacentIfOccupied;

            [InfoBox(
                "If true, this unique enemy will only spawn when Brain.PublishUniqueEnemySpawn('EnemyName') is called. If false, it will spawn at the start of the battle."
            )]
            public bool SpawnOnlyOnEvent;
            public GameplayPlayerSettings.DifficultyLevel MinimumDifficultyLevelToSpawn;

            public UniqueEnemyStartingPlacement(CharacterData enemy, Vector2Int startingPosition)
            {
                Enemy = enemy;
                StartingPosition = startingPosition;
                MinimumDifficultyLevelToSpawn = GameplayPlayerSettings.DifficultyLevel.Easy;
                SpawnOnAdjacentIfOccupied = false;
                SpawnOnlyOnEvent = false;
            }
        }

        [System.Serializable]
        public class UniqueEnemyPlacementContainer
        {
            public UniqueEnemyStartingPlacement[] placements;
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

            var ctx = BattleGameObject.Context;
            if (ctx == null)
            {
                return OperationResult.Failure("BattleContext is not available for enemy spawn");
            }

            if (
                !TryResolveSpawnPosition(
                    placement.StartingPosition,
                    placement.SpawnOnAdjacentIfOccupied,
                    out var spawnPosition,
                    out var positionError
                )
            )
            {
                return OperationResult.Failure(
                    $"Could not place {created.CharacterTemplate.DisplayName}: {positionError}"
                );
            }

            if (!ctx.SpawnAtPosition(created, spawnPosition))
            {
                return OperationResult.Failure(
                    $"SpawnAtPosition failed for {created.CharacterTemplate.DisplayName} at {spawnPosition}"
                );
            }

            EnemyInstancesByStartingPlacement ??=
                new Dictionary<GenericEnemyStartingPlacement, CharacterInstance>();
            EnemyInstancesByStartingPlacement[placement] = created;
            return OperationResult.Successful();
        }

        private OperationResult EnsureAndSpawnUniqueEnemyInstance(
            UniqueEnemyStartingPlacement placement
        )
        {
            var ltm = BattleGameObject.Brain.ltm;
            var factory = new CharacterFactory(ltm);
            var persistentInstance = factory.CreateOrRecall(placement.Enemy);
            if (persistentInstance == null)
            {
                return OperationResult.Failure(
                    $"CharacterFactory failed to create instance for {placement.Enemy.DisplayName}"
                );
            }

            var created = persistentInstance.CreateBattleCopy();
            var context = BattleGameObject.Context;
            if (context == null)
            {
                return OperationResult.Failure(
                    "BattleContext is not available for unique enemy spawn"
                );
            }

            if (
                !TryResolveSpawnPosition(
                    placement.StartingPosition,
                    placement.SpawnOnAdjacentIfOccupied,
                    out var spawnPosition,
                    out var positionError
                )
            )
            {
                return OperationResult.Failure(
                    $"Could not place {created.CharacterTemplate.DisplayName}: {positionError}"
                );
            }

            if (!context.SpawnAtPosition(created, spawnPosition))
            {
                return OperationResult.Failure(
                    $"SpawnAtPosition failed for {created.CharacterTemplate.DisplayName} at {spawnPosition}"
                );
            }

            UniqueEnemyInstancesByStartingPlacement ??=
                new Dictionary<UniqueEnemyStartingPlacement, CharacterInstance>();
            UniqueEnemyInstancesByStartingPlacement[placement] = created;
            return OperationResult.Successful();
        }

        private bool TryResolveSpawnPosition(
            Vector2Int requestedPosition,
            bool spawnAdjacentIfOccupied,
            out Vector2Int resolvedPosition,
            out string error
        )
        {
            resolvedPosition = requestedPosition;
            error = null;

            var mapGrid = BattleGameObject.Context?.MapGrid;
            if (mapGrid == null)
            {
                error = "MapGrid is not available";
                return false;
            }

            var requestedPoint = mapGrid.GetGridPoint(
                requestedPosition.x,
                requestedPosition.y
            );
            if (requestedPoint == null)
            {
                error = $"starting position {requestedPosition} is outside the map";
                return false;
            }

            if (requestedPoint.CurrentInstance == null)
            {
                return true;
            }

            if (!spawnAdjacentIfOccupied)
            {
                error = $"starting position {requestedPosition} is occupied";
                return false;
            }

            foreach (var offset in AdjacentSpawnOffsets)
            {
                var candidatePosition = requestedPosition + offset;
                var candidatePoint = mapGrid.GetGridPoint(
                    candidatePosition.x,
                    candidatePosition.y
                );
                if (candidatePoint != null && candidatePoint.CurrentInstance == null)
                {
                    resolvedPosition = candidatePosition;
                    return true;
                }
            }

            error = $"starting position {requestedPosition} is occupied and has no free adjacent tile";
            return false;
        }

        private static int StableStringHash(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return 0;
            }
            unchecked
            {
                int hash = 23;
                foreach (var c in s)
                {
                    hash = (hash * 31) + c;
                }
                return hash;
            }
        }

        private static double DeterministicDouble(
            double min,
            double max,
            int battleSeed,
            string salt
        )
        {
            unchecked
            {
                int seed =
                    (battleSeed == 0 ? (int)0x9E3779B9 : battleSeed) ^ StableStringHash(salt ?? "");
                var rnd = new System.Random(seed);
                return min + (rnd.NextDouble() * (max - min));
            }
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
