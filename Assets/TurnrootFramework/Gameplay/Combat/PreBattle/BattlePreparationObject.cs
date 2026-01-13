using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Roster;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    public enum PlacementState
    {
        NonePlaced,
        DefaultPlaced,
        PlayerPlaced,
        PlayerConfirmed,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    public class BattlePreparationObject : MonoBehaviour
    {
        public Brain.Brain Brain { get; private set; }

        public MapGrid MapGrid { get; private set; }

        [HideInInspector]
        public EnvironmentalConditions EnvironmentalConditions { get; private set; }

        [HideInInspector]
        public int MaxPlayerTeamUnits;

        [field: SerializeField, HideInInspector]
        public List<CharacterData> RequiredPlayerUnits { get; private set; } = new();

        [HideInInspector]
        public List<Vector2Int> PlayerTeamSpawnPoints;

        public OperationResult Initialize(Brain.Brain brain)
        {
            Brain = brain;
            EnvironmentalConditions = GetComponentInChildren<EnvironmentalConditions>(true);
            MapGrid = GetComponentInChildren<MapGrid>(true);
            PlayerTeamSpawnPoints = MapGrid.PlayerTeamSpawnPoints;

            // Copy MaxPlayerTeamUnits and RequiredPlayerUnits from a BattleGameObject when available.
            if (brain?.battleBrain?.BattleObject != null)
            {
                MaxPlayerTeamUnits = brain.battleBrain.BattleObject.MaxPlayerTeamUnits;
                RequiredPlayerUnits =
                    brain.battleBrain.BattleObject.RequiredPlayerUnits ?? new List<CharacterData>();
            }
            else
            {
                var parentBattleObject = GetComponentInParent<BattleGameObject>();
                if (parentBattleObject != null)
                {
                    MaxPlayerTeamUnits = parentBattleObject.MaxPlayerTeamUnits;
                    RequiredPlayerUnits =
                        parentBattleObject.RequiredPlayerUnits ?? new List<CharacterData>();
                }
            }

            var gamewideContextBrain = brain.gamewideContextBrain;
            PlayerTeamRosterAllUnits = gamewideContextBrain?.GamewidePersistentPlayerRoster;
            InitializePlacements();

            return EnvironmentalConditions == null
                ? OperationResult.Failure("EnvironmentalConditions not found")
                : OperationResult.SuccessResult();
        }

        /* --------------------------- Starting Positions --------------------------- */
        [HideInInspector]
        // In BattlePreparationObject.cs

        public Dictionary<Vector2Int, CharacterInstance> placements; // Changed from CharacterData

        public OperationResult InitializePlacements()
        {
            // Get the FILTERED roster from BattleBrain
            var selectedUnits = Brain?.battleBrain?.PlayerTeamRoster?.Instances;

            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                return OperationResult.Failure("No units available for positioning");
            }

            placements = new Dictionary<Vector2Int, CharacterInstance>();

            // Place units at spawn points based on their roster order
            for (int i = 0; i < selectedUnits.Count && i < PlayerTeamSpawnPoints.Count; i++)
            {
                if (i >= MaxPlayerTeamUnits)
                {
                    break;
                }

                var spawnPos = PlayerTeamSpawnPoints[i];
                var unit = selectedUnits[i];

                placements[spawnPos] = unit;
            }

            CurrentPlacementState = PlacementState.DefaultPlaced;
            return OperationResult.SuccessResult();
        }

        [HideInInspector]
        public PlacementState CurrentPlacementState = PlacementState.NonePlaced;

        [HideInInspector]
        public Vector2Int? selectedPosition;

        [HideInInspector]
        public Vector2Int? potentialSwapPosition;

        [HideInInspector]
        public CharacterInstance selectedUnit;

        [HideInInspector]
        public CharacterInstance potentialSwapUnit;

        [HideInInspector]
        public bool CanSwap => selectedUnit != null && potentialSwapUnit != null;

        [HideInInspector]
        public PlayerTeamRoster PlayerTeamRosterSelectedForBattle;

        [HideInInspector]
        public PlayerTeamRoster PlayerTeamRosterAllUnits;

        public OperationResult PlaceUnit(Vector2Int pos, CharacterInstance unit)
        {
            if (!PlayerTeamSpawnPoints.Contains(pos))
            {
                return OperationResult.Failure("Cannot place unit: invalid position");
            }
            else
            {
                placements[pos] = unit;
                return OperationResult.SuccessResult();
            }
        }

        public OperationResult SwapUnits()
        {
            if (!CanSwap || selectedPosition == null || potentialSwapPosition == null)
            {
                return OperationResult.Failure("Cannot swap units: selection incomplete");
            }
            else
            {
                (placements[potentialSwapPosition.Value], placements[selectedPosition.Value]) = (
                    placements[selectedPosition.Value],
                    placements[potentialSwapPosition.Value]
                );
                // TODO: Call a brain event to trigger visual changes
                return OperationResult.SuccessResult();
            }
        }
    }
}
