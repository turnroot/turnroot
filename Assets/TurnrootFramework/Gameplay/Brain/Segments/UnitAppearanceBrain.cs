using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages visual representations and 3D models of units in battle, including movement animations and equipment changes.
    /// </summary>
    public partial class UnitAppearanceBrain : BrainComponent
    {
        internal void LogWarning(string message) =>
            $"UnitAppearanceBrain: {message}".LogWarning();

        internal void LogError(string message) =>
            $"UnitAppearanceBrain: {message}".LogError();

        private GameplayGeneralSettings _settings;
        private Dictionary<string, GameObject> _unitModels = new();
        private Dictionary<Vector2Int, string> _modelPositions = new();
        private Dictionary<string, GameObject> _mountModels = new();

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void Awake()
        {
            base.Awake();
            _settings = GameplayGeneralSettings.Instance;
        }

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleObjectSet += HandleBattleObjectSet;
            Brain.OnCharacterMoveStarted += HandleCharacterMoveStarted;
            Brain.OnItemEquipped += HandleItemEquipped;
            Brain.OnItemUnequipped += HandleItemUnequipped;

            // Subscribe to unit spawn events so visuals are created reactively when the authoritative
            // spawn (SpawnCommand) publishes a UnitSpawnedEvent.
            Brain.Subscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent, EventPriority.Normal);

            // Keep _modelPositions in sync when units are swapped during pre-battle positioning.
            Brain.Subscribe<ModelSwappedEvent>(HandleModelSwappedEvent, EventPriority.Normal);

            if (Brain.battleBrain.BattleObject != null)
            {
                HandleBattleObjectSet(Brain.battleBrain.BattleObject);
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (Brain != null)
            {
                Brain.OnBattleObjectSet -= HandleBattleObjectSet;
                Brain.OnCharacterMoveStarted -= HandleCharacterMoveStarted;
                Brain.OnItemEquipped -= HandleItemEquipped;
                Brain.OnItemUnequipped -= HandleItemUnequipped;
                Brain.Unsubscribe<UnitSpawnedEvent>(HandleUnitSpawnedEvent);
                Brain.Unsubscribe<ModelSwappedEvent>(HandleModelSwappedEvent);
            }
        }

        private void HandleBattleObjectSet(BattleGameObject battleObject) => HandleBattleStarted();

        /// <summary>
        /// Keeps <see cref="_modelPositions"/> in sync after a pre-battle unit swap so that
        /// position-based operations (e.g. <see cref="DespawnUnitAtPosition"/>) resolve to the
        /// correct unit after positions have been exchanged.
        /// </summary>
        private void HandleModelSwappedEvent(ModelSwappedEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            // Swap the two position→id entries so _modelPositions reflects the new layout.
            var hasA = _modelPositions.TryGetValue(ev.PosA, out var idAtA);
            var hasB = _modelPositions.TryGetValue(ev.PosB, out var idAtB);

            if (hasA) _modelPositions[ev.PosB] = idAtA;
            else      _modelPositions.Remove(ev.PosB);

            if (hasB) _modelPositions[ev.PosA] = idAtB;
            else      _modelPositions.Remove(ev.PosA);
        }

        private OperationResult HandleBattleStarted()
        {
            ClearAllModels();

            var roster =
                Brain.battleBrain.PlayerTeamRoster
                ?? Brain.battleBrain.BattleObject.PlayerTeamRoster;

            var validation = OperationResultGuards.RequireNotNull(roster, nameof(roster));
            if (!validation.Success)
            {
                return validation;
            }

            var placements = roster.GetPlacements();

            if (placements == null || placements.Length == 0)
            {
                "HandleBattleStarted: roster has no placements".LogInfo();
            }

            foreach (var placement in placements)
            {
                var instance = roster.GetInstanceFor(placement.CharacterData);
                if (instance == null)
                {
                    // Instances may be missing in some initialization scenarios; this is informational.
                    $"No instance for template {placement.CharacterData.DisplayName}".LogInfo();
                    continue;
                }

                // Try to use the authoritative BattleContext spawn so map occupancy and
                // MapGridPosition are consistently set via the SpawnCommand. If that succeeds
                // we will create visuals via SpawnUnitAtPosition without overwriting positions later.
                var spawnedByContext =
                    Brain.battleBrain.BattleObject.Context != null
                    && Brain.battleBrain.BattleObject.Context.SpawnAtPosition(
                        instance,
                        placement.SpawnPosition
                    );
                if (!spawnedByContext)
                {
                    // Fallback: try to set occupancy directly on the MapGrid so the authoritative grid state & instance position remain consistent.
                    var map = Brain.battleBrain.BattleObject.MapGrid;
                    var mgp = map.GetGridPoint(
                        placement.SpawnPosition.x,
                        placement.SpawnPosition.y
                    );
                    if (mgp != null)
                    {
                        var setRes = map.SetOccupied(mgp, instance);
                        if (setRes.Success)
                        {
                            // Mirror SpawnCommand semantics for spawned units.
                            instance.WasSpawnedDuringBattle = true;

                            // ensure the unit is registered in participants so that
                            // subsequent command lookups (and targeting logic) work.
                            Brain.battleBrain?.BattleObject?.Context?.EnsureUnitIsParticipant(
                                instance
                            );

                            // Publish the authoritative UnitSpawnedEvent so visual systems react consistently.
                            Brain.Publish(new UnitSpawnedEvent(instance, placement.SpawnPosition));
                        }
                        else
                        {
                            $"HandleBattleStarted: MapGrid.SetOccupied failed for {instance.CharacterTemplate.DisplayName} at {placement.SpawnPosition}: {setRes.ErrorMessage}".LogInfo();
                        }
                    }
                    else
                    {
                        // Missing map grid point can happen during early initialization; log as informational.

                        $"HandleBattleStarted: Context spawn failed for {instance.CharacterTemplate.DisplayName} at {placement.SpawnPosition}; MapGrid missing grid point, skipping visual spawn.".LogInfo();
                    }
                }
                else
                {
                    // Visuals will be created by the UnitSpawnedEvent handler (reactive to authoritative spawn).
                }
            }

            return OperationResult.Successful();
        }

        private Vector3 GetWorldPosition(Vector2Int pos, bool prebattle)
        {
            // Always use PreparationObject.MapGrid since it works correctly
            // BattleObject.MapGrid might not be initialized the same way
            var mapGrid =
                _brain.battleBrain.PreparationObject?.MapGrid
                ?? _brain.battleBrain.BattleObject.MapGrid;
            return mapGrid.GetTerrainAdjustedWorldPosition(pos);
        }

        private void ClearAllModels()
        {
            var allUnits = Brain.gamewideContextBrain.GetAllActiveInstances();
            if (allUnits != null)
            {
                foreach (var unit in allUnits)
                {
                    if (unit != null)
                    {
                        ClearWeaponFromUnit(unit);
                        ClearMountFromUnit(unit);
                    }
                }
            }
            // Note: ClearMountFromUnit already destroys mounts and removes them from _mountModels

            foreach (var model in _unitModels.Values.ToList())
            {
                if (model != null)
                {
                    model.SetActive(false);
                    Destroy(model);
                }
            }

            // Clear dictionaries
            _unitModels.Clear();
            _mountModels.Clear();
            _modelPositions.Clear();
        }

        private void HandleItemEquipped(
            CharacterInstance character,
            Objects.ObjectItemInstance item
        )
        {
            if (item.Template.IsEquippable == true)
            {
                if (item.Template.Subtype == Objects.Components.ObjectSubtype.Weapon)
                {
                    var result = UpdateUnitWeapon(character);
                    if (!result.Success)
                    {
                        // Non-critical failure to update visuals — informational only.
                        $"Failed to update weapon for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}".LogInfo();
                    }
                }
                else if (item.Template.Subtype == Objects.Components.ObjectSubtype.Shield)
                {
                    var result = UpdateUnitShield(character);
                    if (!result.Success)
                    {
                        // Non-critical failure to update visuals — informational only.
                        $"Failed to update shield for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}".LogInfo();
                    }
                }
            }
        }

        private void HandleItemUnequipped(
            CharacterInstance character,
            Objects.ObjectItemInstance item
        )
        {
            if (item.Template.IsEquippable == true)
            {
                if (item.Template.Subtype == Objects.Components.ObjectSubtype.Weapon)
                {
                    var result = UpdateUnitWeapon(character);
                    if (!result.Success)
                    {
                        // Non-critical failure to update visuals — informational only.
                        $"Failed to update weapon for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}".LogInfo();
                    }
                }
                else if (item.Template.Subtype == Objects.Components.ObjectSubtype.Shield)
                {
                    var result = UpdateUnitShield(character);
                    if (!result.Success)
                    {
                        // Non-critical failure to update visuals — informational only.
                        $"Failed to update shield for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}".LogInfo();
                    }
                }
            }
        }

        private void HandleUnitSpawnedEvent(UnitSpawnedEvent evt)
        {
            // When an authoritative spawn occurs (SpawnCommand), create or move visuals to match.
            if (evt == null || evt.Unit == null)
            {
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD

            $"HandleUnitSpawnedEvent: unit={evt.Unit?.Id}, char={evt.Unit?.CharacterTemplate?.DisplayName}, pos={evt.SpawnPosition}".LogInfo();
#endif

            // Create or move model for the spawned unit. This will use existing model if present.
            var res = SpawnUnitAtPosition(evt.Unit, evt.SpawnPosition, prebattle: false);
            if (!res.Success)
            {
                // Visual update failed; this is typically non-fatal and can be informational.
                $"HandleUnitSpawnedEvent: Failed to create/move visuals for {evt.Unit?.CharacterTemplate?.DisplayName}: {res.ErrorMessage}".LogInfo();
            }
        }
    }
}



