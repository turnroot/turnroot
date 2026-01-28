using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
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
        private Dictionary<Vector2Int, GameObject> _activeUnitModels = new();

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void Awake()
        {
            base.Awake();
            _settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
        }

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleObjectSet += HandleBattleObjectSet;

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
            }
        }

        private void InitializeClassVisuals(
            CharacterClassDataInstance classInst,
            CharacterInstance unit
        )
        {
            if (classInst == null)
            {
                return;
            }

            var renderer = classInst.MeshRenderer ?? unit.Renderer;
            if (renderer != null)
            {
                classInst.InitializeWithRenderer(renderer);
            }
        }

        private void ApplyVisuals(CharacterInstance unit, GameObject model)
        {
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                unit.SetRenderer(renderer);
                GetUnitOutfitMaterial(unit);
                SetBlendshapes(unit);
            }
        }

        private void PublishDespawnEvent(GameObject model, Vector2Int pos)
        {
            var owner = model.GetComponent<UnitModelOwnership>();
            var unitId = owner?.UnitId;
            var unit = !string.IsNullOrEmpty(unitId)
                ? _brain
                    ?.gamewideContextBrain?.GetAllActiveInstances()
                    ?.FirstOrDefault(u => u?.Id == unitId)
                : null;

            _brain?.Publish(new ModelDespawnedEvent(unit, unitId, pos, model));
        }

        private OperationResult HandleBattleStarted()
        {
            ClearExistingModels();

            var roster =
                _brain?.battleBrain?.PlayerTeamRoster
                ?? _brain?.battleBrain?.BattleObject?.PlayerTeamRoster;
            if (roster == null)
            {
                return OperationResult.Failure("PlayerTeamRoster is null");
            }

            var placements = roster.GetPlacements();

            foreach (var placement in placements)
            {
                var instance = roster.GetInstanceFor(placement.CharacterData);
                if (instance == null)
                {
                    TurnrootLogger.Log(
                        $"UnitAppearanceBrain: No instance for template {placement.CharacterData?.DisplayName}",
                        TurnrootLogger.LogLevel.Warning
                    );
                    continue;
                }

                var res = SpawnUnitModelOnGrid(
                    placement.SpawnPosition,
                    instance,
                    _activeUnitModels,
                    prebattle: false
                );
                if (!res.Success)
                {
                    TurnrootLogger.Log(
                        $"HandleBattleStarted: Failed to spawn model for {instance?.CharacterTemplate?.DisplayName} at {placement.SpawnPosition} - {res.ErrorMessage}",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }
            return OperationResult.Successful();
        }

        private void HandleBattleObjectSet(BattleGameObject battleObject) => HandleBattleStarted();

        private Vector3 GetWorldPosition(Vector2Int pos, bool prebattle)
        {
            return prebattle
                ? _brain.battleBrain.PreparationObject.MapGrid.GetTerrainAdjustedWorldPosition(pos)
                : _brain.battleBrain.BattleObject.MapGrid.GetTerrainAdjustedWorldPosition(pos);
        }
    }
}
