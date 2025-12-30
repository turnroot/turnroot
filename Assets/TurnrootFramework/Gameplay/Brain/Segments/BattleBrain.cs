using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(TurnRotisserie))]
    /// <summary>
    /// The battle brain manages one battle at a time.
    /// Responsible for initializing battles and managing turn order.
    /// </summary>
    public partial class BattleBrain : BrainComponent
    {
        [SerializeField, HideInInspector]
        private PlayerTeamRoster _playerTeamRoster;
        private TurnRotisserie _turnRotisserie;

        public CharacterInstance ActiveUnit => _turnRotisserie.GetActiveUnit();

        private BattleContextAIHelper _aiHelper;

        public BattleGameObject BattleObject { get; private set; }

        // Roster accessors through BattleGameObject
        public PlayerTeamRosterInstance PlayerTeamRoster =>
            BattleObject != null ? BattleObject.PlayerTeamRoster : null;
        public GenericRosterInstance EnemyTeamRoster =>
            BattleObject != null ? BattleObject.EnemyTeamRoster : null;
        public GenericRosterInstance ThirdPartyTeamRoster =>
            BattleObject != null ? BattleObject.ThirdPartyTeamRoster : null;

        // Roster lifecycle and character persistence are delegated to GamewideContextBrain

        protected override void Awake()
        {
            base.Awake();

            _turnRotisserie = GetComponent<TurnRotisserie>();

#if UNITY_EDITOR
            Debug.Log("BattleBrain: TurnRotisserie ready");
#endif
        }

        private void Start()
        {
            // Ensure the gamewide persistent player roster exists and is recalled
            if (
                _brain?.gamewideContextBrain != null
                && _brain.gamewideContextBrain.GamewidePersistentPlayerRoster == null
            )
            {
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            }

            // Prefer the gamewide roster if available
            _playerTeamRoster =
                _brain?.gamewideContextBrain?.GamewidePersistentPlayerRoster ?? _playerTeamRoster;

            _brain?.gamewideContextBrain?.GetOrCreatePlayerTeamRoster(_playerTeamRoster);
        }

        #region Roster Management API

        public GenericRosterInstance InstantiateGenericRoster(
            GenericRoster roster,
            bool register = false
        ) => _brain?.gamewideContextBrain?.GetOrCreateGenericRoster(roster, register);

        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _brain?.gamewideContextBrain?.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            _brain?.gamewideContextBrain?.GetAllActiveInstances();

        public PlayerTeamRosterInstance InstantiatePlayerTeamRoster() =>
            _brain?.gamewideContextBrain?.GetOrCreatePlayerTeamRoster(_playerTeamRoster);

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            _brain?.gamewideContextBrain?.RecallGenericRosters(rosters);

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _brain?.gamewideContextBrain?.SaveUniqueCharacterProgress(instance);

        #endregion

        #region Status Effect & Last-Attacker API

        public bool RemoveStatusEffect(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        )
        {
            if (character == null || effect == null)
            {
                return false;
            }

            var removed = character.RemoveStatusEffect(effect);
            if (removed)
            {
                _brain?.PublishStatusEffectRemoved(character, effect);
            }
            return removed;
        }

        public int RemoveStatusEffectsByType(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectType effectType
        )
        {
            if (character == null || effectType == null)
            {
                return 0;
            }

            var toRemove = character
                .GetActiveStatusEffects()
                .FindAll(e => e.EffectType == effectType);
            var count = character.RemoveStatusEffectsByType(effectType);
            foreach (var r in toRemove)
            {
                _brain?.PublishStatusEffectRemoved(character, r);
            }
            return count;
        }

        public int RemoveAllBuffs(CharacterInstance character)
        {
            if (character == null)
            {
                return 0;
            }

            var toRemove = character
                .GetActiveStatusEffects()
                .FindAll(e => e.EffectType?.IsBuff == true);
            var count = character.RemoveAllBuffs();
            foreach (var r in toRemove)
            {
                _brain?.PublishStatusEffectRemoved(character, r);
            }
            return count;
        }

        public int RemoveAllDebuffs(CharacterInstance character)
        {
            if (character == null)
            {
                return 0;
            }

            var toRemove = character
                .GetActiveStatusEffects()
                .FindAll(e => e.EffectType?.IsDebuff == true);
            var count = character.RemoveAllDebuffs();
            foreach (var r in toRemove)
            {
                _brain?.PublishStatusEffectRemoved(character, r);
            }
            return count;
        }

        public void ClearAllStatusEffects(CharacterInstance character)
        {
            if (character == null)
            {
                return;
            }

            var toRemove = character.GetActiveStatusEffects();
            character.ClearAllStatusEffects();
            foreach (var r in toRemove)
            {
                _brain?.PublishStatusEffectRemoved(character, r);
            }
            _brain?.PublishAllStatusEffectsCleared(character);
        }

        public void SetLastAttacker(
            BattleContext context,
            CharacterInstance target,
            CharacterInstance attacker
        )
        {
            if (target == null)
            {
                return;
            }

            target.SetLastAttacker(attacker);
            context?.RegisterLastAttacker(target, attacker);
            if (attacker == null)
            {
                _brain?.PublishLastAttackerCleared(target);
            }
            else
            {
                _brain?.PublishLastAttackerSet(target, attacker);
            }
        }

        public void ClearLastAttacker(BattleContext context, CharacterInstance target)
        {
            if (target == null)
            {
                return;
            }

            target.ClearLastAttacker();
            context?.RegisterLastAttacker(target, null);
            _brain?.PublishLastAttackerCleared(target);
        }

        #endregion

        private void HandleTurnEndStatusEffects()
        {
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst == null)
                {
                    continue;
                }

                var expired = inst.TickStatusEffects();
                foreach (var e in expired)
                {
                    _brain?.PublishStatusEffectExpired(inst, e);
                }
            }
        }

        /// <summary>
        /// Apply a status effect to a character via the BattleBrain so events are published consistently.
        /// </summary>
        public Characters.StatusEffects.StatusEffectInstance ApplyStatusEffect(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectType effectType,
            string sourceCharacterId = null,
            string sourceSkillId = null,
            int? duration = null,
            float intensity = 1f
        )
        {
            if (character == null || effectType == null)
            {
                return null;
            }

            var previous = character.GetActiveStatusEffects().Find(e => e.EffectType == effectType);
            var prevStacks = previous?.CurrentStacks ?? 0;

            var result = character.ApplyStatusEffect(
                effectType,
                sourceCharacterId,
                sourceSkillId,
                duration,
                intensity
            );
            if (result == null)
            {
                return null;
            }

            if (previous == null)
            {
                _brain?.PublishStatusEffectApplied(character, result);
            }
            else if (result.CurrentStacks > prevStacks)
            {
                _brain?.PublishStatusEffectStacked(character, result);
            }
            else
            {
                // Refreshed or updated duration without stacking
                _brain?.PublishStatusEffectApplied(character, result);
            }

            return result;
        }

        /// <summary>
        /// Internal helper to move a unit on the grid and publish movement events.
        /// This should only be called by command implementations (e.g., MoveCommand) so that movement is undoable/redoable.
        /// Use <see cref="Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext.MoveUnitToPoint"/> to perform a commanded move.
        /// </summary>
        internal bool MoveUnit(CharacterInstance unit, Vector2Int target, MapGrid mapGrid)
        {
            if (unit == null || mapGrid == null)
            {
                return false;
            }

            var from = unit.MapGridPosition;
            var oldPoint = unit.UnitPositionToMapGridPoint(from, mapGrid);
            var result = unit.MoveToPosition(target, mapGrid);
            if (result.Success)
            {
                var newPoint = unit.UnitPositionToMapGridPoint(target, mapGrid);
                mapGrid.RemoveOccupied(oldPoint);
                mapGrid.SetOccupied(newPoint, unit);
                unit.MapGridPosition = target;
                // publish both simple event and the advanced UnitMovedEvent for subscribers
                _brain?.PublishCharacterMoveCompleted(unit, newPoint);
                _brain?.PublishUnitMoved(unit, target);
                _brain?.Publish(new Events.UnitMovedEvent(unit, from, target));
            }
            return result.Success;
        }

        public void ProgressTurnOrder()
        {
            if (!_turnRotisserie.Progress())
            {
#if UNITY_EDITOR
                Debug.LogError("BattleBrain: Failed to progress turn order!");
#endif
                Debug.Break();
            }
        }

        /// <summary>
        /// Clears AI helper's reusable caches (move/attack tiles) and reachability caches.
        /// </summary>
        public void ClearAICache() => _aiHelper?.InvalidateAllCaches();

        #region Battle Initialization

        public void HandleStartBattle()
        {
#if UNITY_EDITOR
            Debug.Log("BattleBrain: Handling StartBattle event");
#endif

            BattleObject = FindBattleGameObjectInScene();

            if (BattleObject == null)
            {
#if UNITY_EDITOR
                Debug.LogError("BattleBrain: No BattleGameObject found in any loaded scene!");
#endif
                return;
            }

            // Connect systems
            BattleObject.Brain = _brain;
            BattleObject.ConnectToBrainEvents();
            BattleObject.ConnectBattleConditionsToContext();

#if UNITY_EDITOR
            Debug.Log($"BattleBrain: Connected to BattleGameObject");
#endif

            // Initialize battle using roster system
            InitializeBattleRosters();

            // Clear last-attacked per-character so we start fresh for this battle
            var allInstances = GetAllActiveInstances();
            foreach (var inst in allInstances)
            {
                if (inst != null)
                {
                    inst.LastAttackedTarget = null;
                    ClearLastAttacker(BattleObject?.Context, inst);
                }
            }

            // Clear central last-attacker mapping in the context
            BattleObject?.Context?.ClearLastAttackHistory();

            // Initialize advanced systems (commands, snapshots)
            // Clear any previous battle's command history
            _brain.Commands?.Clear();
            // Take initial snapshot of battle state
            _brain.TakeSnapshot();

#if UNITY_EDITOR
            Debug.Log("BattleBrain: Battle initialization complete");
#endif
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
#if UNITY_EDITOR
                        Debug.Log($"BattleBrain: Found BattleGameObject in scene '{scene.name}'");
#endif
                        return battleObj;
                    }
                }
            }

#if UNITY_EDITOR
            Debug.LogWarning("BattleBrain: No BattleGameObject found in loaded scenes");
#endif
            return null;
        }

        private void InitializeBattleRosters()
        {
            // 1. Create empty runtime roster instances
            BattleObject.InitializeBattleRosters();

            // 2. Populate rosters from templates and persistent data
            var result = BattleObject.PopulateBattleRostersFromTemplates();
            if (!result.Success)
            {
#if UNITY_EDITOR
                Debug.LogError($"Failed to populate battle rosters: {result.ErrorMessage}");
#endif
            }

            SpawnRosterUnitsOntoGrid();

            _aiHelper = new BattleContextAIHelper(BattleObject.Context);
        }

        private void SpawnRosterUnitsOntoGrid()
        {
            var enemyRoster = BattleObject.EnemyTeamRoster;
            if (BattleObject.HasThirdParty)
            {
                var thirdPartyRoster = BattleObject.ThirdPartyTeamRoster;
            }
            var playerTeamRoster = BattleObject.PlayerTeamRoster;
            // 1. Spawn enemy units (iterate runtime placements)
            foreach (var p in enemyRoster.GetPlacements())
            {
                var characterData = p.CharacterData;
                var characterInstance = enemyRoster.GetInstanceFor(characterData);
                var placement = p;
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                enemyRoster.SetOrder(characterData, placement.Order);
            }
            // 2. Spawn third-party units, if needed
            if (BattleObject.HasThirdParty)
            {
                var thirdPartyRoster = BattleObject.ThirdPartyTeamRoster;
                foreach (var p in thirdPartyRoster.GetPlacements())
                {
                    var characterData = p.CharacterData;
                    var characterInstance = thirdPartyRoster.GetInstanceFor(characterData);
                    var placement = p;
                    BattleObject.Context.SpawnAtPosition(
                        characterInstance,
                        placement.SpawnPosition
                    );
                    thirdPartyRoster.SetOrder(characterData, placement.Order);
                }
            }
            // 3. Spawn player team units
            foreach (var p in playerTeamRoster.GetPlacements())
            {
                var characterData = p.CharacterData;
                var characterInstance = playerTeamRoster.GetInstanceFor(characterData);
                var placement = p;
                BattleObject.Context.SpawnAtPosition(characterInstance, placement.SpawnPosition);
                playerTeamRoster.SetOrder(characterData, placement.Order);
            }
        }

        #endregion

        #region Battle Cleanup

        private void HandleExitBattle(BattleExitType exitType)
        {
#if UNITY_EDITOR
            Debug.Log($"BattleBrain: Handling ExitBattle event with type: {exitType}");
#endif
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
                    ClearLastAttacker(BattleObject?.Context, inst);
                }
            }

            // Clear central last-attacker mapping in the context
            _brain?.battleBrain?.BattleObject?.Context?.ClearLastAttackHistory();
#if UNITY_EDITOR
            Debug.Log("BattleBrain: Battle cleanup complete");
#endif
        }

        #endregion
    }
}
