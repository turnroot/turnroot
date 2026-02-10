using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Maps;
using Turnroot.UI.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.PreBattle
{
    /// <summary>
    /// Represents the current state of player unit placement in pre-battle preparation.
    /// </summary>
    public enum PlacementState
    {
        NonePlaced,
        DefaultPlaced,
        PlayerPlaced,
        PlayerConfirmed,
    }

    /// <summary>
    /// Manages pre-battle preparation including unit selection, placement, and starting positions.
    /// </summary>
    [RequireComponent(typeof(EnvironmentalConditions))]
    public partial class BattlePreparationObject : MonoBehaviour
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

            if (PlayerTeamSpawnPoints == null || PlayerTeamSpawnPoints.Count == 0)
            {
                TurnrootLogger.Log(
                    "BattlePreparationObject.Initialize: PlayerTeamSpawnPoints is empty or missing. Verify the map's spawn point data.",
                    TurnrootLogger.LogLevel.Warning
                );
            }

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
                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPositioningModeEntered += HandlePositioningModeEntered;
            }

            // Set the map grid for Camera Brain
            var cameraBrain = brain?.cameraBrain;
            cameraBrain.SetMapGrid(MapGrid);

            if (EnvironmentalConditions == null)
            {
                return OperationResult.Failure("EnvironmentalConditions not found");
            }

            // Notify the Brain that this BattlePreparationObject has been initialized.
            Brain?.PublishBattlePrepObjectInitialized(this);

            return OperationResult.Successful();
        }

        /* --------------------------- Starting Positions --------------------------- */
        [HideInInspector]
        public Dictionary<Vector2Int, CharacterInstance> placements;

        // Per-battle selection state: this is intentionally separate from CharacterInstance.IsSelectedForBattle
        // so changing selections in the pre-battle UI does NOT mutate persistent roster selection state.
        private readonly System.Collections.Generic.HashSet<string> _battleSelectedIds = new();

        private bool _isInitializingPlacements = false;
        private bool _needsReinitialize = false;

        // Simplified approach: do not queue per-unit selection publishes during initialization.
        // `InitializePlacements()` now only publishes PlacementsInitialized and does not fire
        // `UnitSelectionChanged` for each unit. UI components should request reinitialization
        // through debounced handlers instead of reacting to per-unit publishes.

        public OperationResult InitializePlacements()
        {
            // If we're already initializing, mark that we need another pass and return quickly.
            // This avoids re-entrant calls from PreBattle selection helper which publishes
            // UnitSelectionChanged for each unit and can cause multiple partial runs.
            if (_isInitializingPlacements)
            {
                _needsReinitialize = true;
                return OperationResult.Successful();
            }

            _isInitializingPlacements = true;
            try
            {
                // Use gamewide selection as the single source of truth for which units are selected.
                var gw = Brain?.gamewideContextBrain;
                var selectedUnits = gw?.GetSelectedForBattlePlayerTeamUnits();

                // If the player modified selections during this pre-battle session, honor the per-battle selections
                var prep = Brain?.battleBrain?.PreparationObject;
                if (prep != null && _battleSelectionsChanged)
                {
                    var prepSelected = prep.GetBattleSelectedInstances();
                    TurnrootLogger.Log(
                        $"InitializePlacements: honoring per-battle selection changes (count={(prepSelected?.Count ?? 0)})",
                        TurnrootLogger.LogLevel.Info
                    );
                    if (prepSelected == null || prepSelected.Count == 0)
                    {
                        // Player explicitly deselected all units for this battle
                        placements = new System.Collections.Generic.Dictionary<
                            Vector2Int,
                            CharacterInstance
                        >();
                        StartingPositionsComponent?.DespawnAllModels();
                        CurrentPlacementState = PlacementState.NonePlaced;
                        Brain?.PublishPlacementsInitialized();
                        _isInitializingPlacements = false;
                        return OperationResult.Successful();
                    }

                    selectedUnits = prepSelected;
                    TurnrootLogger.Log(
                        "InitializePlacements: Using per-battle selection set:",
                        TurnrootLogger.LogLevel.Info
                    );
                    foreach (var s in prepSelected)
                    {
                        TurnrootLogger.Log(
                            $"  - {s?.CharacterTemplate?.DisplayName ?? "<null>"}",
                            TurnrootLogger.LogLevel.Info
                        );
                    }
                }

                // First, attempt to load explicit placements from the runtime player roster instance (LTM/persisted placements)
                var persistent =
                    gw?.GamewidePersistentPlayerRoster
                    ?? gw?.CreateOrRecallGamewidePersistentPlayerRoster();
                var runtimeInstance =
                    persistent != null ? gw.GetOrCreatePlayerTeamRoster(persistent) : null;

                // Try an extracted helper to validate and apply runtime placements
                if (TryUseRuntimePlacements(gw, persistent, runtimeInstance))
                {
                    // The helper published placements; finish early.
                    return OperationResult.Successful();
                }

                // Compute the final selected units using extracted helper logic
                var computeResult = ComputeFinalSelectedUnits(
                    gw,
                    persistent,
                    runtimeInstance,
                    (BattlePreparationObject)prep
                );
                if (!computeResult.hasSelection)
                {
                    return computeResult.failure;
                }

                var finalSelected = computeResult.finalSelected;

                // Apply placements from the computed final selection using an extracted helper
                ApplyPlacementsFromSelectedUnits(finalSelected);

                // Do NOT publish per-unit `UnitSelectionChanged` here — that caused re-entrancy.
                // UI updates should be driven by `PlacementsInitialized` (published above).
                // This keeps initialization single-pass and prevents infinite re-entry loops.

                // Do NOT sync placements into the runtime roster here; only persist on explicit commit (persist:true).
                // This avoids overwriting the player's roster template during UI-only interactions.
            }
            finally
            {
                _isInitializingPlacements = false;
            }

            // If another InitializePlacements was requested during our run, run it once more now
            if (_needsReinitialize)
            {
                _needsReinitialize = false;
                return InitializePlacements();
            }

            // Flush complete; pending publishes have been delivered.

            return OperationResult.Successful();
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

        /// <summary>
        /// Preview a potential swap/move to <paramref name="pos"/>. This updates
        /// swap projector and swap unit UI immediately without committing the action.
        /// If the target tile is empty, swap unit data is cleared. If the cursor
        /// is on the selected unit, the swap preview is cleared.
        /// </summary>
        private void OnDestroy()
        {
            if (Brain != null)
            {
                Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                Brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
            }
        }

        // Per-battle selection API (does not mutate CharacterInstance.IsSelectedForBattle)
        public bool IsBattleSelected(CharacterInstance inst) =>
            inst != null && _battleSelectedIds.Contains(inst.Id);

        private bool _battleSelectionsChanged = false; // Track whether user modified selections in this session

        public void SetBattleSelected(
            CharacterInstance inst,
            bool selected,
            bool publish = true,
            bool markChanged = true
        )
        {
            if (inst == null)
            {
                return;
            }

            if (selected)
            {
                _battleSelectedIds.Add(inst.Id);
            }
            else
            {
                _battleSelectedIds.Remove(inst.Id);
            }

            // Mark that selections were changed during this pre-battle session only when requested
            if (markChanged)
            {
                _battleSelectionsChanged = true;
            }

            // Persist selection choice to LTM so the player's preference is remembered.
            try
            {
                var template = inst.CharacterTemplate;
                if (template != null && Brain?.ltm != null)
                {
                    // If this unit is required for the battle, don't overwrite LTM
                    if (RequiredPlayerUnits == null || !RequiredPlayerUnits.Contains(template))
                    {
                        var key = LtmKeys.UnitSelectedForBattlePrefix + template.name;
                        Brain.ltm.RememberBool(key, selected);
                    }
                }
            }
            catch { }

            if (publish)
            {
                // Publish selection changes immediately — UI handlers should debounce if needed.
                Brain?.PublishUnitSelectionChanged(inst, selected);
            }
        }

        public System.Collections.Generic.List<CharacterInstance> GetBattleSelectedInstances()
        {
            var list = new System.Collections.Generic.List<CharacterInstance>();

            // If we have explicitly selected ids for this session, resolve them against the
            // game's active instances so selections are honored even if placements are currently empty.
            if (_battleSelectedIds != null && _battleSelectedIds.Count > 0)
            {
                var gw = Brain?.gamewideContextBrain;
                if (gw != null)
                {
                    var all = gw.GetAllActiveInstances();
                    foreach (var inst in all)
                    {
                        if (inst != null && _battleSelectedIds.Contains(inst.Id))
                        {
                            list.Add(inst);
                        }
                    }

                    // Also include any instances referenced in placements that might not be in the active list.
                    if (placements != null)
                    {
                        foreach (var inst in placements.Values)
                        {
                            if (
                                inst != null
                                && _battleSelectedIds.Contains(inst.Id)
                                && !list.Contains(inst)
                            )
                            {
                                list.Add(inst);
                            }
                        }
                    }

                    return list;
                }
            }

            // Fallback: if we don't have active instances available yet, use placements as the source.
            if (placements == null || placements.Count == 0)
            {
                return list;
            }

            foreach (var inst in placements.Values)
            {
                if (inst != null && _battleSelectedIds.Contains(inst.Id))
                {
                    list.Add(inst);
                }
            }
            return list;
        }

        // Apply the current placements into the runtime player roster instance. If persist is true,
        // save the runtime roster into Long Term Memory so placements survive reloads and are used
        // to initialize the battle roster later.
        public void SyncPlacementsToRuntimeRoster(bool persist)
        {
            var gw = Brain?.gamewideContextBrain;
            if (gw == null)
            {
                return;
            }

            var persistent =
                gw.GamewidePersistentPlayerRoster
                ?? gw.CreateOrRecallGamewidePersistentPlayerRoster();
            if (persistent == null)
            {
                return;
            }

            var runtimeInstance = gw.GetOrCreatePlayerTeamRoster(persistent);
            if (runtimeInstance == null)
            {
                return;
            }

            var list = new System.Collections.Generic.List<Characters.Roster.UnitPlacement>();
            foreach (var kvp in placements)
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

            runtimeInstance.ApplyDecodedPlacements(list.ToArray());

            if (persist)
            {
                gw.SavePlayerRoster(lastSavedBattleTurn: 1);
            }
        }
    }
}
