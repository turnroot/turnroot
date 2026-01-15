using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.GameSettings;
using Turnroot.UI.Components;
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

        [HideInInspector]
        public StartingPositions StartingPositionsComponent;

        public OperationResult Initialize(Brain.Brain brain)
        {
            Brain = brain;
            EnvironmentalConditions = GetComponentInChildren<EnvironmentalConditions>(true);
            MapGrid = GetComponentInChildren<MapGrid>(true);
            PlayerTeamSpawnPoints = MapGrid.PlayerTeamSpawnPoints;
#if UNITY_EDITOR
            Debug.Log(
                $"BattlePreparationObject.Initialize: MapGrid={MapGrid?.name}, PlayerTeamSpawnPoints.Count={PlayerTeamSpawnPoints?.Count ?? 0}"
            );
#endif

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

            // Keep placement view in sync with gamewide selection. When selection changes we
            // will reinitialize placements, but we avoid overwriting user edits.
            if (brain != null)
            {
                brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;

                // When UI enters positioning mode, ensure roster is filtered and placements are set
                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPositioningModeEntered += HandlePositioningModeEntered;
            }

            // Initialize placements from the current gamewide selection.
            _ = InitializePlacements();

            // Set the map grid for Camera Brain
            var cameraBrain = brain?.cameraBrain;
            cameraBrain.SetMapGrid(MapGrid);

            return EnvironmentalConditions == null
                ? OperationResult.Failure("EnvironmentalConditions not found")
                : OperationResult.SuccessResult();
        }

        /* --------------------------- Starting Positions --------------------------- */
        [HideInInspector]
        public Dictionary<Vector2Int, CharacterInstance> placements;

        public OperationResult InitializePlacements()
        {
            // Use gamewide selection as the single source of truth for which units are selected.
            var selectedUnits = Brain?.gamewideContextBrain?.GetSelectedForBattlePlayerTeamUnits();

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
            Brain?.PublishPlacementsInitialized();
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

        public OperationResult SelectPosition(Vector2Int pos)
        {
            if (!PlayerTeamSpawnPoints.Contains(pos))
            {
                return OperationResult.Failure("Invalid position");
            }

            if (!placements.ContainsKey(pos))
            {
                return OperationResult.Failure("Cannot select empty position");
            }

            selectedPosition = pos;
            selectedUnit = placements[pos];
            Debug.Log($"Selected unit: {selectedUnit.CharacterTemplate.DisplayName} at {pos}");

            // Update visuals: position the selected projector and show unit data immediately
            StartingPositionsComponent?.SetSelected(pos);

            if (StartingPositionsComponent != null && selectedUnit != null)
            {
                var name = selectedUnit?.CharacterTemplate?.DisplayName ?? "";
                var className = ""; // TODO: Get current class from instance
                var portrait =
                    selectedUnit?.CharacterTemplate?.DefaultPortrait?.RuntimeSprite
                    ?? (
                        selectedUnit?.CharacterTemplate?.Sprites?.Length > 0
                            ? selectedUnit.CharacterTemplate.Sprites[0]
                            : null
                    );

                StartingPositionsComponent.SetSelectedUnit(name, className, portrait);
            }

            return OperationResult.SuccessResult();
        }

        public OperationResult ClearSelection()
        {
            selectedPosition = null;
            potentialSwapPosition = null;
            selectedUnit = null;
            potentialSwapUnit = null;
            StartingPositionsComponent.Clears();
            return OperationResult.SuccessResult();
        }

        public OperationResult ExecutePositionAction()
        {
            // Called when second Confirm happens
            if (selectedPosition == null || potentialSwapPosition == null)
            {
                return OperationResult.Failure("Invalid action state");
            }

            // Determine if target is occupied
            bool targetOccupied = placements.ContainsKey(potentialSwapPosition.Value);

            if (targetOccupied)
            {
                // Swap
                StartingPositionsComponent.SetSwap(potentialSwapPosition.Value);
                (placements[selectedPosition.Value], placements[potentialSwapPosition.Value]) = (
                    placements[potentialSwapPosition.Value],
                    placements[selectedPosition.Value]
                );
            }
            else
            {
                // Move
                StartingPositionsComponent.SetSelected(potentialSwapPosition.Value);
                placements[potentialSwapPosition.Value] = placements[selectedPosition.Value];
                placements.Remove(selectedPosition.Value);
            }

            ClearSelection();
            CurrentPlacementState = PlacementState.PlayerPlaced; // Mark as modified

            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Preview a potential swap/move to <paramref name="pos"/>. This updates
        /// swap projector and swap unit UI immediately without committing the action.
        /// If the target tile is empty, swap unit data is cleared. If the cursor
        /// is on the selected unit, the swap preview is cleared.
        /// </summary>
        public OperationResult PreviewPotentialSwap(Vector2Int pos)
        {
            if (selectedPosition == null)
            {
                return OperationResult.Failure("No selected unit to preview against");
            }

            // Invalid positions (not a player spawn point) should clear preview
            if (PlayerTeamSpawnPoints == null || !PlayerTeamSpawnPoints.Contains(pos))
            {
                potentialSwapPosition = null;
                potentialSwapUnit = null;
                StartingPositionsComponent?.ClearSwapPreview();
                return OperationResult.Failure("Invalid position");
            }

            // If cursor is on the same tile as the selected unit, clear swap preview
            if (pos == selectedPosition.Value)
            {
                potentialSwapPosition = null;
                potentialSwapUnit = null;
                StartingPositionsComponent?.SetSelected(selectedPosition.Value);
                StartingPositionsComponent?.ClearSwapPreview();
#if UNITY_EDITOR
                Debug.Log(
                    $"PreviewPotentialSwap: cursor is on selected tile {pos}, cleared swap preview"
                );
#endif
                return OperationResult.SuccessResult();
            }

            potentialSwapPosition = pos;

            // Show swap projector at the target
            StartingPositionsComponent?.SetSwap(pos);

            if (placements.ContainsKey(pos))
            {
                var unit = placements[pos];
                potentialSwapUnit = unit;

                // Prepare display data
                var name = unit?.CharacterTemplate?.DisplayName ?? "";
                var className =
                    unit?.CurrentClassTemplate?.Identity?.ClassName
                    ?? unit?.CharacterTemplate?.StartingClass?.Identity?.ClassName
                    ?? "";
                var portrait =
                    unit?.CharacterTemplate?.DefaultPortrait?.RuntimeSprite
                    ?? (
                        unit?.CharacterTemplate?.Sprites?.Length > 0
                            ? unit.CharacterTemplate.Sprites[0]
                            : null
                    );

                StartingPositionsComponent?.SetSwapUnit(name, className, portrait);
#if UNITY_EDITOR
                Debug.Log($"PreviewPotentialSwap: target occupied by '{name}' at {pos}");
#endif
            }
            else
            {
                potentialSwapUnit = null;
                StartingPositionsComponent?.ClearSwapUnit();
#if UNITY_EDITOR
                Debug.Log($"PreviewPotentialSwap: target empty at {pos} (cleared swap unit)");
#endif
            }

            return OperationResult.SuccessResult();
        }

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected)
        {
            // Recompute placements when selection changes
            if (
                CurrentPlacementState == PlacementState.NonePlaced
                || CurrentPlacementState == PlacementState.DefaultPlaced
            )
            {
                InitializePlacements();
            }
        }

        private void HandlePositioningModeEntered()
        {
            // Ensure there is a runtime player roster instance so selection queries work.
            var gw = Brain?.gamewideContextBrain;
            if (gw != null)
            {
                // Ensure the persistent player roster asset is present and has a runtime instance.
                var persistent =
                    gw.GamewidePersistentPlayerRoster
                    ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
                if (persistent != null)
                {
                    var rosterInstance = gw.GetOrCreatePlayerTeamRoster(persistent);
                    PreBattleSelectionHelper.EnsureDefaultPreBattleSelections(
                        Brain,
                        persistent,
                        rosterInstance,
                        MaxPlayerTeamUnits,
                        RequiredPlayerUnits
                    );
                }

                // Set the "battle" camera  (at this point, it behaves like a battle camera)
                // in camerabrain, and move the camera to the first player spawn point
                // TODO: Possibly refactor this to use a "prebattle camera"?
                var cameraBrain = Brain?.cameraBrain;
                var cameraChildren = GetComponentsInChildren<Camera>();
                foreach (var cam in cameraChildren)
                {
                    if (cam != null && cam.CompareTag("BattleMapCamera"))
                    {
                        cameraBrain.SetBattleMapCamera(cam);
                        break;
                    }
                }
                cameraBrain.MoveCameraToPosition(PlayerTeamSpawnPoints.FirstOrDefault());

                var result = InitializePlacements();
                if (!result.Success)
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        $"BattlePreparationObject: InitializePlacements failed: {result.ErrorMessage}"
                    );
                    var selectedUnits =
                        Brain?.gamewideContextBrain?.GetSelectedForBattlePlayerTeamUnits();
                    var count = selectedUnits?.Count ?? 0;
                    Debug.Log($"BattlePreparationObject: Selected units count: {count}");
                    var persistentChars =
                        Brain
                            ?.gamewideContextBrain
                            ?.GamewidePersistentPlayerRoster
                            ?.characters
                            ?.Length ?? 0;
                    Debug.Log(
                        $"BattlePreparationObject: Persistent roster template placements: {persistentChars}"
                    );
#endif
                }
            }
        }

        private void OnDestroy()
        {
            if (Brain != null)
            {
                Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                Brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
            }
        }
    }
}
