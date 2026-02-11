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

            // Keep placement view in sync with gamewide selection. When selection changes we
            // will reinitialize placements, but we avoid overwriting user edits.
            if (brain != null)
            {
                brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPositioningModeEntered += HandlePositioningModeEntered;

                // Listen for requests to sync placements centrally so callers don't need to know
                // the details of runtime roster persistence or placement locking.
                brain.OnPlacementsSyncRequested -= HandlePlacementsSyncRequested;
                brain.OnPlacementsSyncRequested += HandlePlacementsSyncRequested;

                // Reconcile visual model moves/swaps back into prep placements when the user moves models directly.
                brain.Unsubscribe<Gameplay.Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);
                brain.Subscribe<Gameplay.Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);

                brain.Unsubscribe<Gameplay.Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
                brain.Subscribe<Gameplay.Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
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
                Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                Brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                Brain.OnPlacementsSyncRequested -= HandlePlacementsSyncRequested;

                Brain.Unsubscribe<Gameplay.Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);
                Brain.Unsubscribe<Gameplay.Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
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

        // Central handler for sync requests published on the Brain. This consolidates
        // placement → runtime roster syncing behavior and ensures proper handling of
        // placement locks and post-sync notification to listeners.
        private void HandlePlacementsSyncRequested(bool persist, bool forceApplyPlacementsOnLoad)
        {
            if (PlacementsLocked && !persist)
            {
                TurnrootLogger.Log(
                    "HandlePlacementsSyncRequested: Placements are locked; skipping non-persistent sync.",
                    TurnrootLogger.LogLevel.Info
                );
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            try
            {
                var dbg = "";
                if (placements != null)
                {
                    foreach (var kvp in placements)
                    {
                        dbg += $"[{kvp.Key}->{kvp.Value?.name}] ";
                    }
                }
                TurnrootLogger.Log(
                    $"HandlePlacementsSyncRequested: entering persist={persist} forceApply={forceApplyPlacementsOnLoad}; prep placements: {dbg}",
                    TurnrootLogger.LogLevel.Info
                );
            }
            catch { }
#endif

            try
            {
                SyncPlacementsToRuntimeRoster(persist, forceApplyPlacementsOnLoad);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                try
                {
                    var gw = Brain?.gamewideContextBrain;
                    var persistent =
                        gw?.GamewidePersistentPlayerRoster
                        ?? gw?.CreateOrRecallGamewidePersistentPlayerRoster();
                    var runtime =
                        persistent != null ? gw.GetOrCreatePlayerTeamRoster(persistent) : null;
                    var runtimeDbg = "";
                    if (runtime != null)
                    {
                        var rplacements = runtime.GetPlacements();
                        foreach (var r in rplacements)
                        {
                            runtimeDbg += $"[{r.SpawnPosition}->{r.CharacterData?.name}] ";
                        }
                    }
                    TurnrootLogger.Log(
                        $"HandlePlacementsSyncRequested: runtime placements after sync: {runtimeDbg}",
                        TurnrootLogger.LogLevel.Info
                    );
                }
                catch { }
#endif
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
                placements ??= new Dictionary<Vector2Int, CharacterData>();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                TurnrootLogger.Log(
                    $"HandleModelMovedEvent: reconciled {data.name} from {ev.From} to {ev.To}",
                    TurnrootLogger.LogLevel.Info
                );
#endif

                Brain?.PublishPlacementsSyncRequested(
                    persist: false,
                    forceApplyPlacementsOnLoad: false
                );
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
                placements ??= new System.Collections.Generic.Dictionary<
                    Vector2Int,
                    CharacterData
                >();

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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                TurnrootLogger.Log(
                    $"HandleModelSwappedEvent: swapped ids {ev.UnitIdA} <-> {ev.UnitIdB} at {ev.PosA}/{ev.PosB}",
                    TurnrootLogger.LogLevel.Info
                );
#endif

                Brain?.PublishPlacementsSyncRequested(
                    persist: false,
                    forceApplyPlacementsOnLoad: false
                );
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

        // Apply the current placements into the runtime player roster instance. If persist is true,
        // save the runtime roster into Long Term Memory so placements survive reloads and are used
        // to initialize the battle roster later. If forceApplyPlacementsOnLoad is true the saved
        // record will be marked so subsequent loads will re-apply the placements automatically.
        public void SyncPlacementsToRuntimeRoster(
            bool persist,
            bool forceApplyPlacementsOnLoad = false
        )
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

            var list = new List<Characters.Roster.UnitPlacement>();
            foreach (var kvp in placements)
            {
                var pos = kvp.Key;
                var data = kvp.Value;
                if (data == null)
                {
                    continue;
                }

                var up = new Characters.Roster.UnitPlacement
                {
                    CharacterData = data,
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
                // When the user explicitly persists placements we store them so they will be applied on load.
                var lastSaved = forceApplyPlacementsOnLoad ? 2 : 1;
                // Use the Brain event to request a parameterized save so GamewideContext handles persistence.
                Brain?.PublishSavePlayerRosterRequested(lastSaved);
            }
        }
    }
}
