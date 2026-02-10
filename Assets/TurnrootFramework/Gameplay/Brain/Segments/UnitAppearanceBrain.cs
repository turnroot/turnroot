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
            TurnrootLogger.Log($"UnitAppearanceBrain: {message}", TurnrootLogger.LogLevel.Warning);

        internal void LogError(string message) =>
            TurnrootLogger.Log($"UnitAppearanceBrain: {message}", TurnrootLogger.LogLevel.Error);

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
            Brain.Subscribe<Events.UnitSpawnedEvent>(HandleUnitSpawnedEvent, EventPriority.Normal);

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
                Brain.Unsubscribe<Events.UnitSpawnedEvent>(HandleUnitSpawnedEvent);
            }
        }

        private void HandleBattleObjectSet(BattleGameObject battleObject) => HandleBattleStarted();

        private OperationResult HandleBattleStarted()
        {
            ClearAllModels();

            var roster =
                Brain.battleBrain?.PlayerTeamRoster
                ?? Brain.battleBrain?.BattleObject?.PlayerTeamRoster;

            var validation = OperationResultGuards.RequireNotNull(roster, nameof(roster));
            if (!validation.Success)
            {
                return validation;
            }

            var placements = roster.GetPlacements();

            TurnrootLogger.Log($"HandleBattleStarted: roster has {placements.Length} placements:");
            foreach (var p in placements)
            {
                TurnrootLogger.Log($"  - {p.CharacterData?.DisplayName} at {p.SpawnPosition}");
            }

            foreach (var placement in placements)
            {
                var instance = roster.GetInstanceFor(placement.CharacterData);
                if (instance == null)
                {
                    TurnrootLogger.Log(
                        $"No instance for template {placement.CharacterData?.DisplayName}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                // Try to use the authoritative BattleContext spawn so map occupancy and
                // MapGridPosition are consistently set via the SpawnCommand. If that succeeds
                // we will create visuals via SpawnUnitAtPosition without overwriting positions later.
                var spawnedByContext =
                    Brain.battleBrain?.BattleObject?.Context?.SpawnAtPosition(
                        instance,
                        placement.SpawnPosition
                    ) ?? false;
                if (!spawnedByContext)
                {
                    // Fallback: try to set occupancy directly on the MapGrid so the authoritative grid state & instance position remain consistent.
                    var map = Brain.battleBrain?.BattleObject?.MapGrid;
                    var mgp = map?.GetGridPoint(
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

                            // Publish the authoritative UnitSpawnedEvent so visual systems react consistently.
                            Brain?.Publish(
                                new Events.UnitSpawnedEvent(instance, placement.SpawnPosition)
                            );
                        }
                        else
                        {
                            TurnrootLogger.Log(
                                $"HandleBattleStarted: MapGrid.SetOccupied failed for {instance.CharacterTemplate.DisplayName} at {placement.SpawnPosition}: {setRes.ErrorMessage}",
                                TurnrootLogger.LogLevel.Warning
                            );
                        }
                    }
                    else
                    {
                        TurnrootLogger.Log(
                            $"HandleBattleStarted: Context spawn failed for {instance.CharacterTemplate.DisplayName} at {placement.SpawnPosition}; MapGrid missing grid point, skipping visual spawn.",
                            TurnrootLogger.LogLevel.Warning
                        );
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
                ?? _brain.battleBrain.BattleObject?.MapGrid;
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
                        TurnrootLogger.Log(
                            $"Failed to update weapon for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }
                else if (item.Template.Subtype == Objects.Components.ObjectSubtype.Shield)
                {
                    var result = UpdateUnitShield(character);
                    if (!result.Success)
                    {
                        TurnrootLogger.Log(
                            $"Failed to update shield for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
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
                        TurnrootLogger.Log(
                            $"Failed to update weapon for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }
                else if (item.Template.Subtype == Objects.Components.ObjectSubtype.Shield)
                {
                    var result = UpdateUnitShield(character);
                    if (!result.Success)
                    {
                        TurnrootLogger.Log(
                            $"Failed to update shield for {character?.CharacterTemplate?.DisplayName}: {result.ErrorMessage}",
                            TurnrootLogger.LogLevel.Warning
                        );
                    }
                }
            }
        }

        private void HandleUnitSpawnedEvent(Events.UnitSpawnedEvent evt)
        {
            // When an authoritative spawn occurs (SpawnCommand), create or move visuals to match.
            if (evt == null || evt.Unit == null)
            {
                return;
            }

            // Create or move model for the spawned unit. This will use existing model if present.
            var res = SpawnUnitAtPosition(evt.Unit, evt.SpawnPosition, prebattle: false);
            if (!res.Success)
            {
                TurnrootLogger.Log(
                    $"HandleUnitSpawnedEvent: Failed to create/move visuals for {evt.Unit?.CharacterTemplate?.DisplayName}: {res.ErrorMessage}",
                    TurnrootLogger.LogLevel.Warning
                );
            }
        }
    }
}
