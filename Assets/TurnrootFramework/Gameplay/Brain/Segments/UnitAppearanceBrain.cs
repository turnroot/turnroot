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

            foreach (var model in _unitModels.Values.ToList())
            {
                if (model != null)
                {
                    model.SetActive(false);
                    Destroy(model);
                }
            }

            foreach (var mount in _mountModels.Values.ToList())
            {
                if (mount != null)
                {
                    mount.SetActive(false);
                    Destroy(mount);
                }
            }

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
    }
}
