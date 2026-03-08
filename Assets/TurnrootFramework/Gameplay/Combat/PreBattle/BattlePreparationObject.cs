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
                "BattlePreparationObject.Initialize: PlayerTeamSpawnPoints is empty or missing. Verify the map's spawn point data.".LogWarning();
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

            // Initialize placements dictionary
            placements = new Dictionary<Vector2Int, CharacterData>();

            Brain?.PublishBattlePrepObjectInitialized(this);
            return OperationResult.Successful();
        }

        /* --------------------------- Starting Positions --------------------------- */
        [HideInInspector]
        public Dictionary<Vector2Int, CharacterData> placements;

        private readonly HashSet<string> _battleSelectedIds = new();

        public OperationResult InitializePlacements()
        {
            var gw = Brain?.gamewideContextBrain;
            var selectedUnits = gw?.GetSelectedForBattlePlayerTeamUnits();

            if (selectedUnits == null || selectedUnits.Count == 0)
            {
                placements = new Dictionary<Vector2Int, CharacterData>();
                StartingPositionsComponent?.DespawnAllModels();
                CurrentPlacementState = PlacementState.NonePlaced;
                Brain?.PublishPlacementsInitialized();

                // Only log as warning if we expected to have units (not during initial setup)
                if (CurrentPlacementState != PlacementState.NonePlaced)
                {
                    return OperationResult.Failure("No units selected for battle");
                }

                // During initial setup, this is expected - just return success with empty placements
                return OperationResult.Successful();
            }

            placements = new Dictionary<Vector2Int, CharacterData>();
            var spawnIndex = 0;

            foreach (var inst in selectedUnits)
            {
                if (inst?.CharacterTemplate == null)
                {
                    continue;
                }

                if (spawnIndex >= PlayerTeamSpawnPoints.Count)
                {
                    $"InitializePlacements: More units ({selectedUnits.Count}) than spawn points ({PlayerTeamSpawnPoints.Count})".LogWarning();
                    break;
                }

                var spawnPos = PlayerTeamSpawnPoints[spawnIndex];
                placements[spawnPos] = inst.CharacterTemplate;
                spawnIndex++;
            }

            CurrentPlacementState = PlacementState.DefaultPlaced;

            // Store these as default placements for potential reset
            StoreDefaultPlacements();

            Brain?.PublishPlacementsInitialized();

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

                brain.Unsubscribe<Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);
                brain.Subscribe<Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);

                brain.Unsubscribe<Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
                brain.Subscribe<Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
            }
            else
            {
                brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
                brain.OnPositioningModeEntered -= HandlePositioningModeEntered;
                brain.OnPlacementsSyncRequested -= HandlePlacementsSyncRequested;

                brain.Unsubscribe<Brain.Events.ModelMovedEvent>(HandleModelMovedEvent);
                brain.Unsubscribe<Brain.Events.ModelSwappedEvent>(HandleModelSwappedEvent);
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
            if (!ValidationHelper.ValidateNotNull(inst, nameof(inst)))
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

            // CRITICAL: Set the flag on the CharacterInstance object itself
            // so that GetSelectedForBattlePlayerTeamUnits() can find it
            inst.IsSelectedForBattle = selected;

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

        private void HandlePlacementsSyncRequested(bool persist, bool forceApplyPlacementsOnLoad)
        {
            // Simplified: no sync needed, placements live only in this object until battle starts
            Brain?.PublishPlacementsInitialized();
        }

        private void HandleModelMovedEvent(Brain.Events.ModelMovedEvent ev)
        {
            if (ev == null || placements == null)
            {
                return;
            }

            var inst = ev.Unit;
            if (inst == null && !string.IsNullOrEmpty(ev.UnitId))
            {
                var allInstances = Brain?.gamewideContextBrain?.GetAllActiveInstances();
                inst = allInstances?.FirstOrDefault(u => u != null && u.Id == ev.UnitId);
            }

            if (inst?.CharacterTemplate == null)
            {
                return;
            }

            placements.Remove(ev.From);
            placements[ev.To] = inst.CharacterTemplate;
            CurrentPlacementState = PlacementState.PlayerPlaced;
        }

        private void HandleModelSwappedEvent(Brain.Events.ModelSwappedEvent ev)
        {
            if (ev == null || placements == null)
            {
                return;
            }

            var all = Brain?.gamewideContextBrain?.GetAllActiveInstances();
            var instA = !string.IsNullOrEmpty(ev.UnitIdA)
                ? all?.FirstOrDefault(u => u != null && u.Id == ev.UnitIdA)
                : null;
            var instB = !string.IsNullOrEmpty(ev.UnitIdB)
                ? all?.FirstOrDefault(u => u != null && u.Id == ev.UnitIdB)
                : null;

            var dataA = instA?.CharacterTemplate;
            var dataB = instB?.CharacterTemplate;

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
    }
}
