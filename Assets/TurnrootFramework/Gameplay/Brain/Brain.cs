using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Characters.CharacterInstance;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// The universal brain for managing and propagating events and data throughout the brain system.
    /// All brain events come from here.
    /// It's like a farfalle, lots of stuff in, one central point, lots of stuff out.
    /// </summary>
    /// <remarks>
    /// If you want to extend brain functionality, create new scripts that interact with the brain components.
    /// Hook into the brain, don't alter it.
    /// If you're looking for hooks, there are lots of events you can subscribe to,
    /// search for `public event Action`.
    ///
    /// Advanced Systems (see Brain.Advanced.cs):
    /// - Priority Event System: Subscribe with priorities for ordered event handling
    /// - Command Pattern: Undoable, serializable battle actions
    /// - State Snapshot System: Save/restore battle state for preview and replay
    ///
    /// Note: if you are going to make changes here, make sure you fork the Turnroot Framework repository
    /// on GitHub and make your changes there. If you go in willy-nily without git history,
    /// I can't help you if you mess up your game!
    /// </remarks>
    [RequireComponent(typeof(StateBrain))]
    [RequireComponent(typeof(ConversationalBrain))]
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(GamewideContextBrain))]
    [RequireComponent(typeof(CharactersBrain))]
    [RequireComponent(typeof(BattleBrain))]
    [RequireComponent(typeof(InventoryBrain))]
    [RequireComponent(typeof(StorehouseBrain))]
    [RequireComponent(typeof(PlayerInputBrain))]
    public partial class Brain : MonoBehaviour
    {
        // Core components
        [HideInInspector]
        public StateBrain stateBrain;

        [HideInInspector]
        public ConversationalBrain conversationalBrain;

        [HideInInspector]
        public GamewideContextBrain gamewideContextBrain;

        [HideInInspector]
        public CharactersBrain charactersBrain;

        [HideInInspector]
        public BattleBrain battleBrain;

        [HideInInspector]
        public InventoryBrain inventoryBrain;

        [HideInInspector]
        public StorehouseBrain storehouseBrain;

        [HideInInspector]
        public PlayerInputBrain playerInputBrain;

        [HideInInspector]
        public LongTermMemory ltm;

        // Scene-level dependencies
        private ConversationController _sceneConversationController;

        // Module flags - paid add-on modules that self-install (evaluated at compile-time)
        public static bool HubModuleEnabled =>
#if TURNROOT_HUB_MODULE
            true;
#else
            false;
#endif
        public static bool BloodlinesModuleEnabled =>
#if TURNROOT_BLOODLINES_MODULE
            true;
#else
            false;
#endif
        public static bool RetroModuleEnabled =>
#if TURNROOT_RETRO_MODULE
            true;
#else
            false;
#endif
        public static bool UnwindModuleEnabled =>
#if TURNROOT_UNWIND_MODULE
            true;
#else
            false;
#endif
        public static bool TroopsModuleEnabled =>
#if TURNROOT_TROOPS_MODULE
            true;
#else
            false;
#endif
        public static bool MonstersModuleEnabled =>
#if TURNROOT_MONSTERS_MODULE
            true;
#else
            false;
#endif

        #region Memory Events

        public event Action<string> OnIllegallyModifiedFileDetected;
        public event Action<int> OnLtmKeyCacheUpdated;

        public void PublishLtmKeyCacheUpdated(int version) => OnLtmKeyCacheUpdated?.Invoke(version);

        public void NotifyIllegalModification(string message) =>
            OnIllegallyModifiedFileDetected?.Invoke(message);

        #endregion

        #region Memory Coders

        public string EncodeInstanceToString<T>(T instance)
            where T : class
        {
            var result = GamewideContextBrainHelpers.EncodeInstanceToString(
                gamewideContextBrain,
                instance
            );
            return result.Success ? result.Value : string.Empty;
        }

        public T DecodeInstanceFromString<T>(string encodedString)
            where T : class
        {
            var result = GamewideContextBrainHelpers.DecodeInstanceFromString<T>(
                gamewideContextBrain,
                encodedString
            );
            return result.Success ? result.Value : null;
        }

        public string EncodeString(string value) => DeviceDataCipher.EncryptToBase64(value);

        public string DecodeString(string encodedString) =>
            DeviceDataCipher.DecryptFromBase64(encodedString);

        #endregion

        #region State Events

        public event Action<BrainState> OnPaused;
        public event Action<BrainState> OnResumed;
        public event Action<BrainState> OnStateChanged;
        public event Action OnGameOver;
        public event Action OnHighLevelStatesInitialized;

        public void PublishPaused(BrainState prev) => OnPaused?.Invoke(prev);

        public void PublishResumed(BrainState prev) => OnResumed?.Invoke(prev);

        public void PublishStateChanged(BrainState newState) => OnStateChanged?.Invoke(newState);

        public void PublishGameOver() => OnGameOver?.Invoke();

        public void PublishHighLevelStatesInitialized() => OnHighLevelStatesInitialized?.Invoke();

        #endregion

        #region Roster Lifecycle Events

        public event Action OnRostersReady;
        public event Action OnRostersFailed;

        public void PublishRostersReady() => OnRostersReady?.Invoke();

        public void PublishRostersFailed() => OnRostersFailed?.Invoke();

        #endregion

        #region Character Movement Events
        public event Action<CharacterInstance, MapGridPoint> OnCharacterMoveStarted;

        public event Action<CharacterInstance, MapGridPoint> OnCharacterMoveCompleted;

        public event Action<CharacterInstance> OnPlayerMovePreviewStarted;

        public event Action<CharacterInstance, MapGridPoint> OnPlayerChoseMoveTile;

        public void PublishCharacterMoveStarted(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnCharacterMoveStarted?.Invoke(character, targetPoint);

        public void PublishCharacterMoveCompleted(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnCharacterMoveCompleted?.Invoke(character, targetPoint);

        public void PublishPlayerMovePreviewStarted(CharacterInstance character) =>
            OnPlayerMovePreviewStarted?.Invoke(character);

        public void PublishPlayerChoseMoveTile(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnPlayerChoseMoveTile?.Invoke(character, targetPoint);

        #endregion

        #region Character Progression Events

        public event Action<CharacterInstance> OnCharacterLevelUp;
        public event Action<CharacterInstance> OnCharacterKill;
        public event Action<CharacterInstance, Skill> OnCharacterLearnedSkill;
        public event Action<CharacterInstance, Skill> OnCharacterRemovedSkill;
        public event Action<CharacterInstance> OnCharacterClassChanged;
        public event Action<CharacterInstance, string, int> OnExperienceGained;
        public event Action<CharacterInstance, CharacterData, int> OnSupportIncreased;

        // Recruitment-related events (published when runtime recruitment overrides change)
        public event Action<CharacterInstance, CharacterData, bool> OnCharacterRecruitableChanged;
        public event Action<
            CharacterInstance,
            CharacterData,
            float
        > OnCharacterRecruitmentChanceChanged;
        public event Action<
            CharacterInstance,
            CharacterData,
            float
        > OnCharacterRecruitmentChanceIncreaseChanged;
        public event Action<
            CharacterInstance,
            CharacterData,
            bool
        > OnCharacterRequiresMinSupportLevelChanged;
        public event Action<
            CharacterInstance,
            CharacterData
        > OnCharacterRecruitmentOverridesCleared;

        public void PublishCharacterLevelUp(CharacterInstance character) =>
            OnCharacterLevelUp?.Invoke(character);

        public void PublishCharacterKill(CharacterInstance character) =>
            OnCharacterKill?.Invoke(character);

        public void PublishCharacterLearnedSkill(CharacterInstance character, Skill skill) =>
            OnCharacterLearnedSkill?.Invoke(character, skill);

        public void PublishCharacterRemovedSkill(CharacterInstance character, Skill skill) =>
            OnCharacterRemovedSkill?.Invoke(character, skill);

        public void PublishCharacterClassChanged(CharacterInstance character) =>
            OnCharacterClassChanged?.Invoke(character);

        public void PublishExperienceGained(
            CharacterInstance character,
            string experienceTypeId,
            int amount
        ) => OnExperienceGained?.Invoke(character, experienceTypeId, amount);

        public void PublishSupportIncreased(
            CharacterInstance character,
            CharacterData targetCharacter,
            int amount
        ) => OnSupportIncreased?.Invoke(character, targetCharacter, amount);

        // Publication helpers for recruitment-related events
        public void PublishCharacterRecruitableChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            bool isRecruitable
        ) => OnCharacterRecruitableChanged?.Invoke(sourceCharacter, targetCharacter, isRecruitable);

        public void PublishCharacterRecruitmentChanceChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            float chance
        ) => OnCharacterRecruitmentChanceChanged?.Invoke(sourceCharacter, targetCharacter, chance);

        public void PublishCharacterRecruitmentChanceIncreaseChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            float increase
        ) =>
            OnCharacterRecruitmentChanceIncreaseChanged?.Invoke(
                sourceCharacter,
                targetCharacter,
                increase
            );

        public void PublishCharacterRequiresMinSupportLevelChanged(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter,
            bool requiresMinSupportLevel
        ) =>
            OnCharacterRequiresMinSupportLevelChanged?.Invoke(
                sourceCharacter,
                targetCharacter,
                requiresMinSupportLevel
            );

        public void PublishCharacterRecruitmentOverridesCleared(
            CharacterInstance sourceCharacter,
            CharacterData targetCharacter
        ) => OnCharacterRecruitmentOverridesCleared?.Invoke(sourceCharacter, targetCharacter);

        /// <summary>
        /// Request that all unique player roster characters be saved.
        /// This is an event-based request; subscribers should perform the save.
        /// </summary>
        public event System.Action OnSavePlayerRosterRequested;

        public void PublishSavePlayerRosterRequested() => OnSavePlayerRosterRequested?.Invoke();

        #endregion

        #region Spawn Events

        public event Action<CharacterInstance, Vector2Int> OnCharacterSpawned;
        public event Action<CharacterInstance, Vector2Int> OnCharacterRemovedFromSpawn;

        public void PublishCharacterSpawned(CharacterInstance character, Vector2Int position) =>
            OnCharacterSpawned?.Invoke(character, position);

        public void PublishCharacterRemovedFromSpawn(
            CharacterInstance character,
            Vector2Int position
        ) => OnCharacterRemovedFromSpawn?.Invoke(character, position);

        #endregion

        #region Item Events

        public event Action<ObjectItemInstance, int> OnItemUsed;
        public event Action<ObjectItemInstance> OnItemBroken;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemTransferred;
        public event Action<ObjectItemInstance> OnItemDiscarded;
        public event Action<ObjectItemInstance> OnItemSold;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemBought;
        public event Action<ObjectItemInstance, int> OnItemRepaired;
        public event Action<ObjectItemInstance, ObjectItem> OnItemForged;
        public event Action<ObjectItemInstance> OnItemDeposited;
        public event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemWithdrawn;

        public void PublishItemUsed(ObjectItemInstance item, int remainingUses) =>
            OnItemUsed?.Invoke(item, remainingUses);

        public void PublishItemBroken(ObjectItemInstance item) => OnItemBroken?.Invoke(item);

        public void PublishItemTransferred(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        ) => OnItemTransferred?.Invoke(item, targetInventory);

        public void PublishItemDiscarded(ObjectItemInstance item) => OnItemDiscarded?.Invoke(item);

        public void PublishItemSold(ObjectItemInstance item) => OnItemSold?.Invoke(item);

        public void PublishItemBought(
            ObjectItemInstance item,
            CharacterInventoryInstance buyerInventory
        ) => OnItemBought?.Invoke(item, buyerInventory);

        public void PublishItemRepaired(ObjectItemInstance item, int repairUses) =>
            OnItemRepaired?.Invoke(item, repairUses);

        public void PublishItemForged(ObjectItemInstance item, ObjectItem targetItem) =>
            OnItemForged?.Invoke(item, targetItem);

        public void PublishItemDeposited(ObjectItemInstance item) => OnItemDeposited?.Invoke(item);

        public void PublishItemWithdrawn(
            ObjectItemInstance item,
            CharacterInventoryInstance targetInventory
        ) => OnItemWithdrawn?.Invoke(item, targetInventory);

        // Equip/Unequip events for inventory items
        public event Action<CharacterInstance, ObjectItemInstance> OnItemEquipped;
        public event Action<CharacterInstance, ObjectItemInstance> OnItemUnequipped;

        public void PublishItemEquipped(CharacterInstance character, ObjectItemInstance item) =>
            OnItemEquipped?.Invoke(character, item);

        public void PublishItemUnequipped(CharacterInstance character, ObjectItemInstance item) =>
            OnItemUnequipped?.Invoke(character, item);

        #endregion

        #region Gold Events

        public event Action<int> OnGoldGained;
        public event Action<int> OnGoldSpent;

        public void PublishGoldGained(int amount) => OnGoldGained?.Invoke(amount);

        public void PublishGoldSpent(int amount) => OnGoldSpent?.Invoke(amount);

        #endregion

        #region Conversation Events

        public event Action<SupportRelationshipInstance> OnSupportPointsChanged;
        public event Action<SupportRelationshipInstance> OnSupportConversationAvailable;
        public event Action<SupportRelationshipInstance> OnSLevelSupportConversationAvailable;
        public event Action<Conversation> OnConversationStarted;
        public event Action<Conversation> OnConversationEnded;
        public event Action<ConversationLayer> OnConversationLayerStarted;
        public event Action<ConversationLayer> OnConversationLayerEnded;

        public void PublishSupportPointsChanged(SupportRelationshipInstance relationship) =>
            OnSupportPointsChanged?.Invoke(relationship);

        public void PublishSupportConversationAvailable(SupportRelationshipInstance relationship) =>
            OnSupportConversationAvailable?.Invoke(relationship);

        public void PublishSLevelSupportConversationAvailable(
            SupportRelationshipInstance relationship
        ) => OnSLevelSupportConversationAvailable?.Invoke(relationship);

        public void PublishConversationStarted(Conversation conversation) =>
            OnConversationStarted?.Invoke(conversation);

        public void PublishConversationEnded(Conversation conversation) =>
            OnConversationEnded?.Invoke(conversation);

        public void PublishConversationLayerStarted(ConversationLayer layer) =>
            OnConversationLayerStarted?.Invoke(layer);

        public void PublishConversationLayerEnded(ConversationLayer layer) =>
            OnConversationLayerEnded?.Invoke(layer);

        // Support relationship added/removed events
        public event Action<
            CharacterInstance,
            Turnroot.Characters.Components.Support.SupportRelationshipInstance
        > OnSupportRelationshipAdded;
        public event Action<CharacterInstance, CharacterData> OnSupportRelationshipRemoved;

        public void PublishSupportRelationshipAdded(
            CharacterInstance source,
            Turnroot.Characters.Components.Support.SupportRelationshipInstance relationship
        ) => OnSupportRelationshipAdded?.Invoke(source, relationship);

        public void PublishSupportRelationshipRemoved(
            CharacterInstance source,
            CharacterData target
        ) => OnSupportRelationshipRemoved?.Invoke(source, target);

        #endregion

        #region Battle Events

        public event Action OnBattleStarted;
        public event Action<BattleExitType> OnBattleCompleted;
        public event Action OnBattleContextInitialized;
        public event Action OnPreBattleStarted;
        public event Action OnPreBattleCompleted;
        public event Action OnTurnBegin;
        public event Action OnTurnEnded;
        public event Action<CharacterInstance> OnPlayerTurnStarted;
        public event Action OnPlayerTurnEnded;
        public event Action OnEnemyTurnStarted;
        public event Action OnEnemyTurnEnded;
        public event Action OnThirdPartyTurnStarted;
        public event Action OnThirdPartyTurnEnded;

        public event Action<CharacterInstance> OnPlayerControlledUnitActivated;
        public event Action<CharacterInstance, int> OnAllyDamaged;
        public event Action<CharacterInstance, int> OnEnemyDamaged;
        public event Action<CharacterInstance> OnUnitDefeated;
        public event Action<CharacterInstance, Vector2Int> OnUnitMoved;
        public event Action<CharacterInstance> OnUnitTakesAnotherTurn;

        public event Action<CharacterInstance> OnUnitFinishedMovingAfterAction;
        public event Action<CharacterInstance> OnCriticalHit;
        public event Action<CharacterInstance, int> OnWeaponUsesChanged;

        // Last-attacker change events (per-target)
        public event Action<CharacterInstance, CharacterInstance> OnLastAttackerSet;
        public event Action<CharacterInstance> OnLastAttackerCleared;

        public void PublishLastAttackerSet(CharacterInstance target, CharacterInstance attacker) =>
            OnLastAttackerSet?.Invoke(target, attacker);

        public void PublishLastAttackerCleared(CharacterInstance target) =>
            OnLastAttackerCleared?.Invoke(target);

        public event Action<CharacterInstance, CharacterInstance> OnItemStolen;
        public event Action<CharacterInstance, BattleEmotion> OnUnitEmotionChanged;

        public void PublishBattleStarted() => OnBattleStarted?.Invoke();

        public void PublishBattleCompleted(BattleExitType exitType) =>
            OnBattleCompleted?.Invoke(exitType);

        public void PublishBattleContextInitialized() => OnBattleContextInitialized?.Invoke();

        public void PublishPreBattleStarted() => OnPreBattleStarted?.Invoke();

        public void PublishPreBattleCompleted() => OnPreBattleCompleted?.Invoke();

        public void PublishTurnBegin() => OnTurnBegin?.Invoke();

        public void PublishTurnEnded() => OnTurnEnded?.Invoke();

        public void PublishPlayerTurnStarted(CharacterInstance unit) =>
            OnPlayerTurnStarted?.Invoke(unit);

        public void PublishPlayerTurnEnded() => OnPlayerTurnEnded?.Invoke();

        public void PublishEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

        public void PublishEnemyTurnEnded() => OnEnemyTurnEnded?.Invoke();

        public void PublishThirdPartyTurnStarted() => OnThirdPartyTurnStarted?.Invoke();

        public void PublishThirdPartyTurnEnded() => OnThirdPartyTurnEnded?.Invoke();

        public void PublishPlayerControlledUnitActivated(CharacterInstance unit) =>
            OnPlayerControlledUnitActivated?.Invoke(unit);

        public void PublishAllyDamaged(CharacterInstance unit, int damage) =>
            OnAllyDamaged?.Invoke(unit, damage);

        public void PublishEnemyDamaged(CharacterInstance unit, int damage) =>
            OnEnemyDamaged?.Invoke(unit, damage);

        public void PublishUnitDefeated(CharacterInstance unit) => OnUnitDefeated?.Invoke(unit);

        public void PublishUnitBattleEmotionChanged(
            CharacterInstance unit,
            BattleEmotion emotion
        ) => OnUnitEmotionChanged?.Invoke(unit, emotion);

        public void PublishUnitMoved(CharacterInstance unit, Vector2Int pos) =>
            OnUnitMoved?.Invoke(unit, pos);

        public void PublishUnitTakesAnotherTurn(CharacterInstance unit) =>
            OnUnitTakesAnotherTurn?.Invoke(unit);

        // Published when an individual unit completes its turn (end of that unit's turn)
        public event System.Action<CharacterInstance> OnUnitTurnEnded;

        public void PublishUnitTurnEnded(CharacterInstance unit) => OnUnitTurnEnded?.Invoke(unit);

        public void PublishCriticalHit(CharacterInstance unit) => OnCriticalHit?.Invoke(unit);

        public void PublishUnitFinishedMovingAfterAction(CharacterInstance unit) =>
            OnUnitFinishedMovingAfterAction?.Invoke(unit);

        public void PublishWeaponUsesChanged(CharacterInstance unit, int change) =>
            OnWeaponUsesChanged?.Invoke(unit, change);

        public void PublishItemStolen(CharacterInstance thief, CharacterInstance target) =>
            OnItemStolen?.Invoke(thief, target);

        #endregion

        #region Status Effect Events

        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectApplied;
        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectRemoved;
        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectStacked;
        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectExpired;

        public void PublishStatusEffectApplied(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectApplied?.Invoke(character, effect);

        public void PublishStatusEffectRemoved(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectRemoved?.Invoke(character, effect);

        public void PublishStatusEffectStacked(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectStacked?.Invoke(character, effect);

        public void PublishStatusEffectExpired(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectExpired?.Invoke(character, effect);

        // Notification that all status effects were cleared from a character (explicit clear)
        public event Action<CharacterInstance> OnAllStatusEffectsCleared;

        public void PublishAllStatusEffectsCleared(CharacterInstance character) =>
            OnAllStatusEffectsCleared?.Invoke(character);

        #endregion

        #region Battle Condition Events

        public event Action<BattleCondition> OnBattleConditionMet;
        public event Action<BattleCondition> OnBattleConditionFailed;

        public void PublishBattleConditionMet(BattleCondition condition) =>
            OnBattleConditionMet?.Invoke(condition);

        public void PublishBattleConditionFailed(BattleCondition condition) =>
            OnBattleConditionFailed?.Invoke(condition);

        #endregion

        #region Skill Events

        public event Action<CharacterInstance, Skill> OnSkillTriggered;
        public event Action<CharacterInstance, Skill> OnSkillEquipped;
        public event Action<CharacterInstance, Skill> OnSkillUnequipped;

        public void PublishSkillTriggered(CharacterInstance character, Skill skill) =>
            OnSkillTriggered?.Invoke(character, skill);

        public void PublishSkillEquipped(CharacterInstance character, Skill skill) =>
            OnSkillEquipped?.Invoke(character, skill);

        public void PublishSkillUnequipped(CharacterInstance character, Skill skill) =>
            OnSkillUnequipped?.Invoke(character, skill);

        #endregion

        public void Awake()
        {
            InitializeLongTermMemory();
            InitializeModules();
            InitializeAdvancedSystems();
            TryLinkConversationController();

            // populate remaining core components
            stateBrain = GetComponent<StateBrain>();
            conversationalBrain = GetComponent<ConversationalBrain>();
            gamewideContextBrain = GetComponent<GamewideContextBrain>();
            battleBrain = GetComponent<BattleBrain>();
            charactersBrain = GetComponent<CharactersBrain>();
            inventoryBrain = GetComponent<InventoryBrain>();
            storehouseBrain = GetComponent<StorehouseBrain>();
            playerInputBrain = GetComponent<PlayerInputBrain>();
        }

        public void InitializeLongTermMemory()
        {
            ltm =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();

            if (ltm == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Brain failed to initialize LongTermMemory.");
#endif
                Debug.Break();
            }
            else
            {
#if UNITY_EDITOR
                Debug.Log("Brain initialized LongTermMemory.");
#endif
            }
        }

        public void InitializeModules()
        {
            var modules = new[]
            {
                (HubModuleEnabled, "Hub"),
                (BloodlinesModuleEnabled, "Bloodlines"),
                (UnwindModuleEnabled, "Unwind"),
                (TroopsModuleEnabled, "Troops"),
                (MonstersModuleEnabled, "Monsters"),
                (RetroModuleEnabled, "Retro"),
            };
            var enabled = string.Join(", ", modules.Where(m => m.Item1).Select(m => m.Item2));
#if UNITY_EDITOR
            Debug.Log($"Turnroot modules: {(string.IsNullOrEmpty(enabled) ? "None" : enabled)}");
#endif
        }

        #region Conversation Controller Management

        private readonly SingleValueCache<ConversationController> _conversationControllerCache =
            new();

        public void PopulateSceneConversationController(ConversationController controller)
        {
            _sceneConversationController = controller;
            _conversationControllerCache.Invalidate(); // Invalidate cache when manually set
#if UNITY_EDITOR
            Debug.Log("Brain populated scene ConversationController.");
#endif
        }

        private void TryLinkConversationController()
        {
            var controller = _conversationControllerCache.GetOrCompute(() =>
            {
                var controllers = FindObjectsByType<ConversationController>(
                    FindObjectsSortMode.None
                );
                return (controllers != null && controllers.Length > 0) ? controllers[0] : null;
            });

            if (controller != null)
            {
                _sceneConversationController = controller;
            }
        }

        #endregion

        #region State Control

        public void Pause()
        {
            var stateBrain = GetComponent<StateBrain>();
            stateBrain?.Pause();
        }

        public void Resume()
        {
            var stateBrain = GetComponent<StateBrain>();
            stateBrain?.Resume();
        }

        #endregion

        #region Cleanup

        private void OnDestroy() => CleanupAdvancedSystems();

        #endregion
    }
}
