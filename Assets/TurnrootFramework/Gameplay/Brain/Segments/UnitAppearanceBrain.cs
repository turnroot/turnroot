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
    public partial class UnitAppearanceBrain : BrainComponent
    {
        private GameplayGeneralSettings _settings;

        // Core model tracking - models are owned by units, not positions
        private Dictionary<string, GameObject> _unitModels = new();
        private Dictionary<Vector2Int, string> _modelPositions = new();

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

            if (Brain.battleBrain?.BattleObject != null)
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

                var res = SpawnUnitAtPosition(instance, placement.SpawnPosition, prebattle: false);
                if (!res.Success)
                {
                    TurnrootLogger.Log(
                        $"Failed to spawn {instance?.CharacterTemplate?.DisplayName}: {res.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

            return OperationResult.Successful();
        }

        private Vector3 GetWorldPosition(Vector2Int pos, bool prebattle)
        {
            return prebattle
                ? _brain.battleBrain.PreparationObject.MapGrid.GetTerrainAdjustedWorldPosition(pos)
                : _brain.battleBrain.BattleObject.MapGrid.GetTerrainAdjustedWorldPosition(pos);
        }

        private void ClearAllModels()
        {
            // Get all units to clear their weapon references
            var allUnits = Brain.gamewideContextBrain?.GetAllActiveInstances();
            if (allUnits != null)
            {
                foreach (var unit in allUnits)
                {
                    if (unit != null)
                    {
                        ClearWeaponFromUnit(unit);
                    }
                }
            }

            foreach (var model in _unitModels.Values.ToList())
            {
                if (model != null)
                {
                    model.SetActive(false);
                    Destroy(model);
                }
            }

            _unitModels.Clear();
            _modelPositions.Clear();
        }

        private void HandleItemEquipped(
            CharacterInstance character,
            Objects.ObjectItemInstance item
        )
        {
            // Only update weapon models for equipped weapons
            if (item?.Template?.IsEquippable == true)
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
        }

        private void HandleItemUnequipped(
            CharacterInstance character,
            Objects.ObjectItemInstance item
        )
        {
            // Only update weapon models for unequipped weapons
            if (item?.Template?.IsEquippable == true)
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
        }
    }
}
