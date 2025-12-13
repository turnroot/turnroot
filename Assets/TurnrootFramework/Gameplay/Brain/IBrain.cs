using System;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.StatusEffects;
using Turnroot.Conversations;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Interface for the Brain system to enable dependency injection and testing.
    /// Use this interface instead of directly referencing the Brain class when possible.
    /// This allows for mocking in unit tests and reduces coupling.
    /// </summary>
    /// <remarks>
    /// To inject IBrain into your classes:
    /// 1. Add a constructor or Initialize method that accepts IBrain
    /// 2. Store the reference in a private field
    /// 3. Use the interface methods for all Brain operations
    ///
    /// Example:
    /// <code>
    /// public class MyClass
    /// {
    ///     private readonly IBrain _brain;
    ///
    ///     public MyClass(IBrain brain)
    ///     {
    ///         _brain = brain;
    ///     }
    ///
    ///     public void DoSomething()
    ///     {
    ///         _brain.PublishGoldGained(100);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface IBrain
    {
        #region Brain Segments

        StateBrain StateBrain { get; }
        ConversationalBrain ConversationalBrain { get; }
        GamewideContextBrain GamewideContextBrain { get; }
        CharactersBrain CharactersBrain { get; }
        BattleBrain BattleBrain { get; }
        InventoryBrain InventoryBrain { get; }
        StorehouseBrain StorehouseBrain { get; }
        LongTermMemory LongTermMemory { get; }

        #endregion

        #region Memory Events

        event Action<string> OnIllegallyModifiedFileDetected;
        event Action<int> OnLtmKeyCacheUpdated;
        void PublishLtmKeyCacheUpdated(int version);
        void NotifyIllegalModification(string message);

        #endregion

        #region Memory Coders

        string EncodeInstanceToString<T>(T instance)
            where T : class;
        T DecodeInstanceFromString<T>(string encodedString)
            where T : class;
        string EncodeString(string value);
        string DecodeString(string encodedString);

        #endregion

        #region State Events

        event Action<BrainState> OnPaused;
        event Action<BrainState> OnResumed;
        event Action<BrainState> OnStateChanged;
        event Action OnGameOver;
        event Action OnHighLevelStatesInitialized;

        void PublishPaused(BrainState prev);
        void PublishResumed(BrainState prev);
        void PublishStateChanged(BrainState newState);
        void PublishGameOver();
        void PublishHighLevelStatesInitialized();

        #endregion

        #region Roster Events

        event Action<RosterInstance> OnRosterReady;
        event Action<Roster, string> OnRosterFailed;

        void PublishRosterReady(RosterInstance instance);
        void PublishRosterFailed(Roster roster, string reason);

        #endregion

        #region Character Events

        event Action<CharacterInstance> OnCharacterLevelUp;
        event Action<CharacterInstance> OnCharacterKill;
        event Action<CharacterInstance, Skill> OnCharacterLearnedSkill;
        event Action<CharacterInstance, Skill> OnCharacterRemovedSkill;
        event Action<CharacterInstance> OnCharacterClassChanged;
        event Action<CharacterInstance, string, int> OnExperienceGained;
        event Action<CharacterInstance, CharacterData, int> OnSupportIncreased;
        event Action<CharacterInstance, Vector2Int> OnCharacterSpawned;
        event Action<CharacterInstance, Vector2Int> OnCharacterRemovedFromSpawn;

        void PublishCharacterLevelUp(CharacterInstance character);
        void PublishCharacterKill(CharacterInstance character);
        void PublishCharacterLearnedSkill(CharacterInstance character, Skill skill);
        void PublishCharacterRemovedSkill(CharacterInstance character, Skill skill);
        void PublishCharacterClassChanged(CharacterInstance character);
        void PublishExperienceGained(CharacterInstance character, string type, int amount);
        void PublishSupportIncreased(CharacterInstance character, CharacterData partner, int level);
        void PublishCharacterSpawned(CharacterInstance character, Vector2Int position);
        void PublishCharacterRemovedFromSpawn(CharacterInstance character, Vector2Int position);

        #endregion

        #region Item Events

        event Action<ObjectItemInstance, int> OnItemUsed;
        event Action<ObjectItemInstance> OnItemBroken;
        event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemTransferred;
        event Action<ObjectItemInstance> OnItemDiscarded;
        event Action<ObjectItemInstance> OnItemSold;
        event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemBought;
        event Action<ObjectItemInstance, int> OnItemRepaired;
        event Action<ObjectItemInstance, ObjectItem> OnItemForged;
        event Action<ObjectItemInstance> OnItemDeposited;
        event Action<ObjectItemInstance, CharacterInventoryInstance> OnItemWithdrawn;

        void PublishItemUsed(ObjectItemInstance item, int usesConsumed);
        void PublishItemBroken(ObjectItemInstance item);
        void PublishItemTransferred(
            ObjectItemInstance item,
            CharacterInventoryInstance destination
        );
        void PublishItemDiscarded(ObjectItemInstance item);
        void PublishItemSold(ObjectItemInstance item);
        void PublishItemBought(ObjectItemInstance item, CharacterInventoryInstance destination);
        void PublishItemRepaired(ObjectItemInstance item, int repairAmount);
        void PublishItemForged(ObjectItemInstance item, ObjectItem forgedItem);
        void PublishItemDeposited(ObjectItemInstance item);
        void PublishItemWithdrawn(ObjectItemInstance item, CharacterInventoryInstance destination);

        #endregion

        #region Gold Events

        event Action<int> OnGoldGained;
        event Action<int> OnGoldSpent;

        void PublishGoldGained(int amount);
        void PublishGoldSpent(int amount);

        #endregion

        #region Battle Events

        // Standardized names (use these for new code)
        event Action OnBattleStarted;
        event Action<BattleExitType> OnBattleCompleted;
        event Action OnBattleContextInitialized;
        event Action OnPreBattleStarted;
        event Action OnPreBattleCompleted;
        event Action OnTurnStarted;
        event Action OnPlayerTurnStarted;
        event Action OnEnemyTurnStarted;
        event Action OnThirdPartyTurnStarted;
        event Action OnPlayerTurnCompleted;
        event Action OnEnemyTurnCompleted;
        event Action OnThirdPartyTurnCompleted;
        event Action OnTurnCompleted;
        event Action<CharacterInstance, int> OnAllyDamaged;
        event Action<CharacterInstance, int> OnEnemyDamaged;
        event Action<CharacterInstance> OnUnitDefeated;
        event Action<CharacterInstance, Vector2Int> OnUnitMoved;

        void PublishBattleStarted();
        void PublishBattleCompleted(BattleExitType exitType);
        void PublishBattleContextInitialized();
        void PublishPreBattleStarted();
        void PublishPreBattleCompleted();
        void PublishTurnStarted();
        void PublishPlayerTurnStarted();
        void PublishEnemyTurnStarted();
        void PublishThirdPartyTurnStarted();
        void PublishPlayerTurnCompleted();
        void PublishEnemyTurnCompleted();
        void PublishThirdPartyTurnCompleted();
        void PublishTurnCompleted();
        void PublishAllyDamaged(CharacterInstance unit, int damage);
        void PublishEnemyDamaged(CharacterInstance unit, int damage);
        void PublishUnitDefeated(CharacterInstance unit);
        void PublishUnitMoved(CharacterInstance unit, Vector2Int position);

        // Additional battle events
        event Action<CharacterInstance> OnUnitTakesAnotherTurn;
        event Action<CharacterInstance> OnCriticalHit;
        event Action<CharacterInstance, int> OnWeaponUsesChanged;
        event Action<CharacterInstance, CharacterInstance> OnItemStolen;

        void PublishUnitTakesAnotherTurn(CharacterInstance unit);
        void PublishCriticalHit(CharacterInstance unit);
        void PublishWeaponUsesChanged(CharacterInstance unit, int usesChange);
        void PublishItemStolen(CharacterInstance thief, CharacterInstance target);

        #endregion

        #region Status Effect Events

        event Action<CharacterInstance, StatusEffectInstance> OnStatusEffectApplied;
        event Action<CharacterInstance, StatusEffectInstance> OnStatusEffectRemoved;
        event Action<CharacterInstance, StatusEffectInstance> OnStatusEffectStacked;
        event Action<CharacterInstance, StatusEffectInstance> OnStatusEffectExpired;

        void PublishStatusEffectApplied(CharacterInstance character, StatusEffectInstance effect);
        void PublishStatusEffectRemoved(CharacterInstance character, StatusEffectInstance effect);
        void PublishStatusEffectStacked(CharacterInstance character, StatusEffectInstance effect);
        void PublishStatusEffectExpired(CharacterInstance character, StatusEffectInstance effect);

        #endregion

        #region Conversation Events

        event Action<SupportRelationshipInstance> OnSupportPointsChanged;
        event Action<SupportRelationshipInstance> OnSupportConversationAvailable;
        event Action<SupportRelationshipInstance> OnSLevelSupportConversationAvailable;
        event Action<Conversation> OnConversationStarted;
        event Action<Conversation> OnConversationEnded;
        event Action<ConversationLayer> OnConversationLayerStarted;
        event Action<ConversationLayer> OnConversationLayerEnded;

        void PublishSupportPointsChanged(SupportRelationshipInstance relationship);
        void PublishSupportConversationAvailable(SupportRelationshipInstance relationship);
        void PublishSLevelSupportConversationAvailable(SupportRelationshipInstance relationship);
        void PublishConversationStarted(Conversation conversation);
        void PublishConversationEnded(Conversation conversation);
        void PublishConversationLayerStarted(ConversationLayer layer);
        void PublishConversationLayerEnded(ConversationLayer layer);

        #endregion

        #region Battle Condition Events

        event Action<BattleCondition> OnBattleConditionMet;
        event Action<BattleCondition> OnBattleConditionFailed;

        void PublishBattleConditionMet(BattleCondition condition);
        void PublishBattleConditionFailed(BattleCondition condition);

        #endregion

        #region Skill Events

        event Action<CharacterInstance, Skill> OnSkillTriggered;
        event Action<CharacterInstance, Skill> OnSkillEquipped;
        event Action<CharacterInstance, Skill> OnSkillUnequipped;

        void PublishSkillTriggered(CharacterInstance character, Skill skill);
        void PublishSkillEquipped(CharacterInstance character, Skill skill);
        void PublishSkillUnequipped(CharacterInstance character, Skill skill);

        #endregion

        #region State Control

        void Pause();
        void Resume();

        #endregion
    }
}
