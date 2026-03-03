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
        internal void LogWarning(string message) => $"UnitAppearanceBrain: {message}".LogWarning();

        internal void LogError(string message) => $"UnitAppearanceBrain: {message}".LogError();

        private GameplayGeneralSettings _settings;
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
        /// Keeps model tracking in sync after a pre-battle unit swap.
        /// </summary>
        private void HandleModelSwappedEvent(ModelSwappedEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            // Delegate to BattlePreparationObject for all model tracking
            var prep = Brain.battleBrain.PreparationObject;
            if (prep != null)
            {
                var result = prep.SwapModelPositions(ev.PosA, ev.PosB);
                if (!result.Success)
                {
                    $"HandleModelSwappedEvent: Failed to swap model positions: {result.ErrorMessage}".LogWarning();
                }
            }
        }

        private OperationResult HandleBattleStarted()
        {
            // ARCHITECTURAL BOUNDARY: This method no longer needs to clear models.
            // Pre-battle models are despawned by BattleBrain.HandleStartBattle() line 137 BEFORE battle models spawn.
            // By the time this method runs, SpawnRosterUnitsOntoGrid() has already spawned battle models.
            // Any models in dictionaries at this point are the BATTLE models - do NOT clear them!
            //
            // SINGLE SOURCE OF TRUTH for model spawning:
            // BattleBrain.SpawnRosterUnitsOntoGrid() → SpawnCommand → UnitSpawnedEvent → HandleUnitSpawnedEvent → SpawnUnitAtPosition

            $"HandleBattleStarted: Battle models already spawned by SpawnRosterUnitsOntoGrid()".LogInfo();
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

            var prep = Brain.battleBrain.PreparationObject;
            if (prep != null)
            {
                foreach (var (position, model, unitId) in prep.GetAllModels())
                {
                    if (model != null)
                    {
                        model.SetActive(false);
                        Destroy(model);
                    }
                }
                // Clear all tracking in BattlePreparationObject
                prep.ClearAllModelTracking();
            }

            // Clear mount models
            _mountModels.Clear();
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

            // Create or move model for the spawned unit
            SpawnUnitAtPosition(evt.Unit, evt.SpawnPosition, prebattle: false);
        }
    }
}
