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
    // Pre-battle placement state
    public enum PlacementState
    {
        NonePlaced,
        DefaultPlaced,
        PlayerPlaced,
        PlayerConfirmed,
    }

    [RequireComponent(typeof(EnvironmentalConditions))]
    public partial class BattlePreparationObject : MonoBehaviour
    {
        public Brain.Brain Brain { get; private set; }

        public MapGrid MapGrid { get; private set; }

        [HideInInspector]
        public EnvironmentalConditions EnvironmentalConditions { get; private set; }

        [HideInInspector]
        public int MaxPlayerTeamUnits;

        [SerializeField, HideInInspector]
        private List<CharacterData> _requiredPlayerUnits = new();

        public List<CharacterData> RequiredPlayerUnits
        {
            get => _requiredPlayerUnits;
            private set => _requiredPlayerUnits = value;
        }

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
            if (brain?.battleBrain.BattleObject != null)
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

            // Keep placement view in sync with gamewide selection
            if (brain != null)
            {
                ConfigureEventSubscriptions(brain, subscribe: true);
            }

            // Set the map grid for Camera Brain
            var cameraBrain = brain?.cameraBrain;
            cameraBrain.SetMapGrid(MapGrid);

            if (EnvironmentalConditions == null)
            {
                return OperationResult.Failure("EnvironmentalConditions not found");
            }

            Brain?.PublishBattlePrepObjectInitialized(this);
            return OperationResult.Successful();
        }

        /* --------------------------- Starting Positions --------------------------- */
        [HideInInspector]
        public Dictionary<Vector2Int, CharacterData> placements;

        // When true, placement updates should not be applied (used to prevent precompute from
        // mutating placements during the authoritative roster initialization flow).
        [HideInInspector]
        public bool PlacementsLocked { get; set; } = false;

        // Per-battle selection state: this is intentionally separate from CharacterInstance.IsSelectedForBattle
        // so changing selections in the pre-battle UI does NOT mutate persistent roster selection state.
        private readonly HashSet<string> _battleSelectedIds = new();

        private bool _isInitializingPlacements = false;
        private bool _needsReinitialize = false;

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
                var prep = Brain?.battleBrain.PreparationObject;

                var persistent =
                    gw?.GamewidePersistentPlayerRoster
                    ?? gw?.CreateOrRecallGamewidePersistentPlayerRoster();
                var runtimeInstance =
                    persistent != null ? gw.GetOrCreatePlayerTeamRoster(persistent) : null;

                if (TryUseRuntimePlacements(gw, persistent, runtimeInstance))
                {
                    return OperationResult.Successful();
                }

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

                ApplyPlacementsFromSelectedUnits(finalSelected);
            }
            finally
            {
                _isInitializingPlacements = false;
            }

            if (_needsReinitialize)
            {
                _needsReinitialize = false;
                return InitializePlacements();
            }

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

        private void OnDestroy()
        {
            if (Brain != null)
            {
                ConfigureEventSubscriptions(Brain, subscribe: false);
            }
        }

        // Helper: centralize adding/removing event subscriptions to avoid duplication
        private void ConfigureEventSubscriptions(Brain.Brain brain, bool subscribe)
        {
            if (brain == null)
            {
                return;
            }

            if (subscribe)
            {
                // Unsubscribe first to ensure idempotency
                brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;

                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPositioningModeEntered += HandlePositioningModeEntered;

                brain.OnPlacementsSyncRequested -= HandlePlacementsSyncRequested;
                brain.OnPlacementsSyncRequested += HandlePlacementsSyncRequested;

                brain.Unsubscribe<Gameplay.Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);
                brain.Subscribe<Gameplay.Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);

                brain.Unsubscribe<Gameplay.Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
                brain.Subscribe<Gameplay.Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
            }
            else
            {
                brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPlacementsSyncRequested -= HandlePlacementsSyncRequested;

                brain.Unsubscribe<Gameplay.Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);
                brain.Unsubscribe<Gameplay.Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
            }
        }

        // Ensure placements dictionary exists before use
        private void EnsurePlacementsExists()
        {
            if (placements == null)
            {
                placements = new Dictionary<Vector2Int, CharacterData>();
            }
        }

        // Safe wrapper around publishing placements sync requests to centralize logging
        private void SafePublishPlacementsSync(bool persist, bool forceApplyPlacementsOnLoad)
        {
            try
            {
                Brain?.PublishPlacementsSyncRequested(persist, forceApplyPlacementsOnLoad);
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"SafePublishPlacementsSync: PublishPlacementsSyncRequested failed: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }

        public bool IsBattleSelected(CharacterInstance inst) =>
            inst != null && _battleSelectedIds.Contains(inst.Id);

        private bool _battleSelectionsChanged = false;

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

            if (markChanged)
            {
                _battleSelectionsChanged = true;
            }

            try
            {
                var template = inst.CharacterTemplate;
                if (template != null && Brain?.ltm != null)
                {
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
                Brain?.PublishUnitSelectionChanged(inst, selected);
            }
        }

        // Sync handler
        private void HandlePlacementsSyncRequested(bool persist, bool forceApplyPlacementsOnLoad)
        {
            if (PlacementsLocked && !persist)
            {
                return;
            }

            try
            {
                SyncPlacementsToRuntimeRoster(persist, forceApplyPlacementsOnLoad);
            }
            catch (System.Exception ex)
            {
                TurnrootLogger.Log(
                    $"HandlePlacementsSyncRequested: SyncPlacementsToRuntimeRoster failed: {ex.Message}",
                    TurnrootLogger.LogLevel.Warning
                );
            }

            // Notify listeners that placements are initialized/updated after a successful sync.
            Brain?.PublishPlacementsInitialized();
        }

        // Reconcile model move events from the UI into authoritative prep placements.
        private void HandleModelMovedEvent(Gameplay.Brain.Events.ModelMovedEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            try
            {
                // Resolve instance if not provided
                var inst = ev.Unit;
                if (inst == null && !string.IsNullOrEmpty(ev.UnitId))
                {
                    var all = Brain?.gamewideContextBrain?.GetAllActiveInstances();
                    inst = all?.FirstOrDefault(u => u != null && u.Id == ev.UnitId);
                }

                if (inst == null)
                {
                    return;
                }

                var data = inst.CharacterTemplate;
                if (data == null)
                {
                    return;
                }

                // Ensure placements exists
                EnsurePlacementsExists();

                // If placement already matches the desired state, skip
                if (placements.TryGetValue(ev.To, out var existing) && existing == data)
                {
                    return;
                }

                // Remove any old mapping for this template so we don't duplicate
                var keysToRemove = new System.Collections.Generic.List<Vector2Int>();
                foreach (var kvp in placements)
                {
                    if (kvp.Value == data && kvp.Key != ev.To)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                foreach (var k in keysToRemove)
                {
                    placements.Remove(k);
                }

                placements[ev.To] = data;
                placements.Remove(ev.From);
                CurrentPlacementState = PlacementState.PlayerPlaced;

                SafePublishPlacementsSync(persist: false, forceApplyPlacementsOnLoad: false);
            }
            catch { }
        }

        private void HandleModelSwappedEvent(Gameplay.Brain.Events.ModelSwappedEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            try
            {
                var all = Brain?.gamewideContextBrain?.GetAllActiveInstances();
                Turnroot.Characters.CharacterInstance a = null,
                    b = null;
                if (!string.IsNullOrEmpty(ev.UnitIdA))
                {
                    a = all?.FirstOrDefault(u => u != null && u.Id == ev.UnitIdA);
                }
                if (!string.IsNullOrEmpty(ev.UnitIdB))
                {
                    b = all?.FirstOrDefault(u => u != null && u.Id == ev.UnitIdB);
                }

                var dataA = a?.CharacterTemplate;
                var dataB = b?.CharacterTemplate;
                EnsurePlacementsExists();

                // Swap in placements dictionary
                if (dataA != null)
                {
                    placements[ev.PosB] = dataA;
                }
                else
                {
                    placements.Remove(ev.PosB);
                }

                if (dataB != null)
                {
                    placements[ev.PosA] = dataB;
                }
                else
                {
                    placements.Remove(ev.PosA);
                }

                CurrentPlacementState = PlacementState.PlayerPlaced;

                SafePublishPlacementsSync(persist: false, forceApplyPlacementsOnLoad: false);
            }
            catch { }
        }

        public List<CharacterInstance> GetBattleSelectedInstances()
        {
            var list = new List<CharacterInstance>();
            var gw = Brain?.gamewideContextBrain; // ensure gw is available throughout the method

            // If we have explicitly selected ids for this session, resolve them against the
            // game's active instances so selections are honored even if placements are currently empty.
            if (_battleSelectedIds != null && _battleSelectedIds.Count > 0)
            {
                /* reuse outer gw */
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
                        foreach (var data in placements.Values)
                        {
                            if (data == null)
                            {
                                continue;
                            }
                            var instFromPlacement = gw.FindInstanceByTemplate(data);
                            if (
                                instFromPlacement != null
                                && _battleSelectedIds.Contains(instFromPlacement.Id)
                                && !list.Contains(instFromPlacement)
                            )
                            {
                                list.Add(instFromPlacement);
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

            foreach (var data in placements.Values)
            {
                if (data == null)
                {
                    continue;
                }
                var inst = gw?.FindInstanceByTemplate(data);
                if (inst != null && _battleSelectedIds.Contains(inst.Id))
                {
                    list.Add(inst);
                }
            }
            return list;
        }

        public void SyncPlacementsToRuntimeRoster(
            bool persist,
            bool forceApplyPlacementsOnLoad = false
        )
        {
            Turnroot.Gameplay.Combat.PreBattle.BattlePlacementSync.ApplyPlacements(
                Brain,
                placements,
                persist,
                forceApplyPlacementsOnLoad
            );
        }
    }
}
