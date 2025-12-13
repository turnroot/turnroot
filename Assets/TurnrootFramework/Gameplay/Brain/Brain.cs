using System;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public class Brain : MonoBehaviour
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
        public LongTermMemory ltm;

        // Scene-level dependencies
        private ConversationController _sceneConversationController;

        // Module flags - paid add-on modules that self-install (evaluated at compile-time)
#if TURNROOT_HUB_MODULE
        public static bool HubModuleEnabled => true;
#else
        public static bool HubModuleEnabled => false;
#endif

#if TURNROOT_BLOODLINES_MODULE
        public static bool BloodlinesModuleEnabled => true;
#else
        public static bool BloodlinesModuleEnabled => false;
#endif

#if TURNROOT_RETRO_MODULE
        public static bool RetroModuleEnabled => true;
#else
        public static bool RetroModuleEnabled => false;
#endif

#if TURNROOT_UNWIND_MODULE
        public static bool UnwindModuleEnabled => true;
#else
        public static bool UnwindModuleEnabled => false;
#endif

#if TURNROOT_TROOPS_MODULE
        public static bool TroopsModuleEnabled => true;
#else
        public static bool TroopsModuleEnabled => false;
#endif

#if TURNROOT_MONSTERS_MODULE
        public static bool MonstersModuleEnabled => true;
#else
        public static bool MonstersModuleEnabled => false;
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
            where T : class => gamewideContextBrain.EncodeInstanceToString<T>(instance);

        public T DecodeInstanceFromString<T>(string encodedString)
            where T : class => gamewideContextBrain.DecodeInstanceFromString<T>(encodedString);

        public string EncodeString(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        public string DecodeString(string encodedString)
        {
            var bytes = Convert.FromBase64String(encodedString);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        #endregion

        #region State Events

        public event Action<BrainState> OnPaused;
        public event Action<BrainState> OnResumed;
        public event Action<BrainState> OnStateChanged;
        public event Action OnGameOver;
        public event Action OnHighLevelStatesInitialized;

        public void PublishPaused(BrainState prev)
        {
            Debug.Log($"EventsBrain: State paused -> {prev?.Name ?? "(null)"}");
            OnPaused?.Invoke(prev);
        }

        public void PublishResumed(BrainState prev)
        {
            Debug.Log($"EventsBrain: State resumed -> {prev?.Name ?? "(null)"}");
            OnResumed?.Invoke(prev);
        }

        public void PublishStateChanged(BrainState newState)
        {
            Debug.Log($"EventsBrain: State changed -> {newState?.Name ?? "(null)"}");
            OnStateChanged?.Invoke(newState);
        }

        public void PublishGameOver()
        {
            Debug.Log("EventsBrain: GameOver event received");
            OnGameOver?.Invoke();
        }

        public void PublishHighLevelStatesInitialized()
        {
            Debug.Log("EventsBrain: High-level states initialized");
            OnHighLevelStatesInitialized?.Invoke();
        }

        #endregion

        #region Roster Lifecycle Events

        public event Action<Characters.RosterInstance> OnRosterReady;
        public event Action<Characters.Roster, string> OnRosterFailed;

        public void PublishRosterReady(Characters.RosterInstance instance) =>
            OnRosterReady?.Invoke(instance);

        public void PublishRosterFailed(Characters.Roster roster, string reason) =>
            OnRosterFailed?.Invoke(roster, reason);

        #endregion

        #region Character Progression Events

        public event Action<CharacterInstance> OnCharacterLevelUp;
        public event Action<CharacterInstance> OnCharacterKill;
        public event Action<CharacterInstance, Skill> OnCharacterLearnedSkill;
        public event Action<CharacterInstance, Skill> OnCharacterRemovedSkill;
        public event Action<CharacterInstance> OnCharacterClassChanged;
        public event Action<CharacterInstance, string, int> OnExperienceGained;
        public event Action<CharacterInstance, CharacterData, int> OnSupportIncreased;

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

        #endregion

        #region Battle Events
        public event Action OnStartBattle;
        public event Action<BattleExitType> OnExitBattle;
        public event Action OnBattleContextInitialized;
        public event Action OnPreBattleStarted;
        public event Action OnPreBattleEnded;
        public event Action OnTurnBegin;
        public event Action OnPlayerTurnStarted;
        public event Action OnEnemyTurnStarted;
        public event Action OnThirdPartyTurnStarted;
        public event Action OnPlayerTurnEnded;
        public event Action OnEnemyTurnEnded;
        public event Action OnThirdPartyTurnEnded;
        public event Action OnTurnEnded;
        public event Action<CharacterInstance, int> OnAllyDamaged;
        public event Action<CharacterInstance, int> OnEnemyDamaged;
        public event Action<CharacterInstance> OnUnitDefeated;
        public event Action<CharacterInstance, Vector2Int> OnUnitMoved;

        public void PublishStartBattle() => OnStartBattle?.Invoke();

        public void PublishPreBattleStarted() => OnPreBattleStarted?.Invoke();

        public void PublishPreBattleEnded() => OnPreBattleEnded?.Invoke();

        public void PublishExitBattle(BattleExitType exitType) => OnExitBattle?.Invoke(exitType);

        public void PublishBattleContextInitialized() => OnBattleContextInitialized?.Invoke();

        public void PublishTurnBegin() => OnTurnBegin?.Invoke();

        public void PublishPlayerTurnStarted() => OnPlayerTurnStarted?.Invoke();

        public void PublishEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

        public void PublishThirdPartyTurnStarted() => OnThirdPartyTurnStarted?.Invoke();

        public void PublishPlayerTurnEnded() => OnPlayerTurnEnded?.Invoke();

        public void PublishEnemyTurnEnded() => OnEnemyTurnEnded?.Invoke();

        public void PublishThirdPartyTurnEnded() => OnThirdPartyTurnEnded?.Invoke();

        public void PublishTurnEnded() => OnTurnEnded?.Invoke();

        public void PublishAllyDamaged(CharacterInstance unit, int damageAmount) =>
            OnAllyDamaged?.Invoke(unit, damageAmount);

        public void PublishEnemyDamaged(CharacterInstance unit, int damageAmount) =>
            OnEnemyDamaged?.Invoke(unit, damageAmount);

        public void PublishUnitDefeated(CharacterInstance unit) => OnUnitDefeated?.Invoke(unit);

        public void PublishUnitMoved(CharacterInstance unit, Vector2Int newPosition) =>
            OnUnitMoved?.Invoke(unit, newPosition);

        public event Action<CharacterInstance> OnUnitTakesAnotherTurn;
        public event Action<CharacterInstance> OnCriticalHit;
        public event Action<CharacterInstance, int> OnWeaponUsesChanged;
        public event Action<CharacterInstance, CharacterInstance> OnItemStolen;

        public void PublishUnitTakesAnotherTurn(CharacterInstance unit) =>
            OnUnitTakesAnotherTurn?.Invoke(unit);

        public void PublishCriticalHit(CharacterInstance unit) => OnCriticalHit?.Invoke(unit);

        public void PublishWeaponUsesChanged(CharacterInstance unit, int usesChange) =>
            OnWeaponUsesChanged?.Invoke(unit, usesChange);

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
            Debug.Log("Brain Awake!");

            InitializeLongTermMemory();
            InitializeModules();
            TryLinkConversationController();

            SceneManager.sceneLoaded += OnSceneLoaded_LinkControllers;

            // populate remaining core components
            stateBrain = GetComponent<StateBrain>();
            conversationalBrain = GetComponent<ConversationalBrain>();
            gamewideContextBrain = GetComponent<GamewideContextBrain>();
            battleBrain = GetComponent<BattleBrain>();
            charactersBrain = GetComponent<CharactersBrain>();
        }

        public void InitializeLongTermMemory()
        {
            ltm =
                gameObject.GetComponent<LongTermMemory>()
                ?? gameObject.AddComponent<LongTermMemory>();

            if (ltm == null)
            {
                Debug.LogError("Brain failed to initialize LongTermMemory.");
                Debug.Break();
            }
            else
            {
                Debug.Log("Brain initialized LongTermMemory.");
            }
        }

        public void InitializeModules()
        {
            var enabledModules = GetEnabledModulesString();

            Debug.Log($"Turnroot add-on modules you have access to: {enabledModules}");
            Debug.Log(
                "You can find more info about Turnroot add-on modules on the Unity Asset Store."
            );
            Debug.Log("All available Turnroot modules initialized.");
        }

        private static string GetEnabledModulesString()
        {
            var modules = new System.Collections.Generic.List<string>();

            if (HubModuleEnabled)
            {
                modules.Add("Hub");
            }

            if (BloodlinesModuleEnabled)
            {
                modules.Add("Bloodlines");
            }

            if (UnwindModuleEnabled)
            {
                modules.Add("Unwind");
            }

            if (TroopsModuleEnabled)
            {
                modules.Add("Troops");
            }

            if (MonstersModuleEnabled)
            {
                modules.Add("Monsters");
            }

            if (RetroModuleEnabled)
            {
                modules.Add("Retro");
            }

            return modules.Count > 0 ? string.Join(", ", modules) : "None";
        }

        #region Conversation Controller Management

        private readonly SingleValueCache<ConversationController> _conversationControllerCache =
            new();

        public void PopulateSceneConversationController(ConversationController controller)
        {
            _sceneConversationController = controller;
            _conversationControllerCache.Invalidate(); // Invalidate cache when manually set
            Debug.Log("Brain populated scene ConversationController.");
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

        private void OnSceneLoaded_LinkControllers(Scene scene, LoadSceneMode mode) =>
            TryLinkConversationController();

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

        private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded_LinkControllers;

        #endregion
    }
}
