using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    public partial class BattleBrain : BrainComponent
    {
        #region Battle Lifecycle

        public void HandleStartBattle()
        {
            if (!InitializeBattleObject())
            {
                return;
            }

            turnRotisserie?.ResetForBattle();
            playerTurnFlow?.ResetForBattle();
            ResetTurnNumber();
            BattleObject.ResetTurnCount();

            // Despawn pre-battle positioning models to avoid confusion with battle models
            PreparationObject?.StartingPositionsComponent?.DespawnAllModels();

            // Start in initializing mode (prevents premature snapshots)
            IsInitializing = true;

            InitializeBattleRosters();
            Brain.PublishBattleObjectSet(BattleObject);
            Brain.PublishBattleStarted();
            ClearUnitBattleState();

            InitializeAdvancedSystems();
            InitializePrecomputeLoader();
            StartBattleWithPlayerPhase();
        }

        private bool InitializeBattleObject()
        {
            var battleObject = FindBattleGameObjectInScene();

            if (battleObject == null)
            {
                "BattleBrain: No BattleGameObject found in any loaded scene".LogError();
                return false;
            }

            var result = BindBattleObject(battleObject);
            if (!result.Success)
            {
                $"BattleBrain: Failed to initialize BattleGameObject: {result.ErrorMessage}".LogError();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Binds a battle object to the brain without starting pre-battle or turn flow.
        /// </summary>
        public OperationResult BindBattleObject(
            BattleGameObject battleObject,
            bool initializePresentation = true
        )
        {
            var validation = OperationResultGuards.RequireNotNull(
                battleObject,
                nameof(battleObject)
            );
            if (!validation.Success)
            {
                return validation;
            }

            BattleObject = battleObject;
            BattleObject.Brain = _brain;

            var connectionResult = BattleObject.ConnectToBrainEvents(initializePresentation);
            return !connectionResult.Success ? connectionResult : BattleObject.ConnectBattleConditionsToContext();
        }

        private void ClearUnitBattleState()
        {
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst != null)
                {
                    inst.LastAttackedTarget = null;
                    ClearLastAttacker(BattleObject?.Context, inst);
                }
            }
            BattleObject?.Context?.ClearLastAttackHistory();
        }

        private void InitializeAdvancedSystems() =>
            // Clear any previous battle's command history
            _brain.Commands?.Clear(); // Initial snapshot is taken AFTER precompute completes (see HandlePrecomputeCompleted)

        private void InitializePrecomputeLoader()
        {
            var precomputeLoader =
                FindFirstObjectByType<Combat.Precompute.BattlePrecomputeLoader>();

            if (precomputeLoader != null)
            {
                var initRes = precomputeLoader.Initialize(_brain, BattleObject?.Context);
                if (!initRes.Success)
                {
                    $"BattleBrain: BattlePrecomputeLoader.Initialize failed: {initRes.ErrorMessage}".LogWarning();
                }
                else
                {
                    precomputeLoader.ForceStartPrecomputeIfPossible();
                }
            }
            else
            {
                "BattleBrain: No BattlePrecomputeLoader found in scene; precompute will be skipped if no loader is available".LogWarning();
            }
        }

        private void SaveInitialRosterPlacements()
        {
            var gw = Brain.gamewideContextBrain;
            if (gw != null)
            {
                int lastSaved = gw.GetSavedPlayerRosterLastBattleTurn();
                if (lastSaved <= 1)
                {
                    // Use the brain event to request a save (lastSavedBattleTurn == 1)
                    Brain?.PublishSavePlayerRosterRequested(1);
                }
            }
        }

        private void StartBattleWithPlayerPhase()
        {
            AdvanceBattleUnitOrPhase();

            if (playerTurnFlow != null)
            {
                playerTurnFlow.BeginPlayerPhase();
                $"Battle started. PlayerTurnFlow state: {playerTurnFlow.GetCurrentState()}".LogInfo();
            }
        }

        private void HandleExitBattle(BattleExitType exitType)
        {
            $"BattleBrain: Handling ExitBattle event with type: {exitType}".LogInfo();

            if (exitType != BattleExitType.Bookmark)
            {
                _brain.Commands?.Clear();
                _brain.Snapshots?.Clear();
            }
            _brain.battleBrain.BattleObject.ClearBattleRosters();

            // Clear transient per-battle data on characters
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst != null)
                {
                    inst.LastAttackedTarget = null;
                    ClearLastAttacker(BattleObject.Context, inst);
                }
            }

            Brain.battleBrain.BattleObject.Context.ClearLastAttackHistory();

            var precomputeLoader =
                FindFirstObjectByType<Combat.Precompute.BattlePrecomputeLoader>();
            precomputeLoader?.ResetPrecomputeFlag();

            "BattleBrain: Battle cleanup complete".LogInfo();
        }

        private BattleGameObject FindBattleGameObjectInScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject rootObject in scene.GetRootGameObjects())
                {
                    var battleObj = rootObject.GetComponentInChildren<BattleGameObject>();
                    if (battleObj != null)
                    {
                        $"BattleBrain: Found BattleGameObject in scene '{scene.name}'".LogInfo();
                        return battleObj;
                    }
                }
            }
            "BattleBrain: No BattleGameObject found in loaded scenes".LogError();
            return null;
        }

        #endregion
    }
}