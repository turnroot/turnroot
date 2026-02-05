using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Characters.Components.Support;
using Turnroot.Conversations;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.Skills;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Characters.CharacterInstance;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Memory Events

        public event Action<string> OnIllegallyModifiedFileDetected;
        public event Action<int> OnLtmKeyCacheUpdated;

        public void PublishIllegalModification(string message) =>
            OnIllegallyModifiedFileDetected?.Invoke(message);

        public void PublishLtmKeyCacheUpdated(int version) => OnLtmKeyCacheUpdated?.Invoke(version);

        #endregion

        #region Memory Coders
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

        #region Cursor Events

        public event Action<MapGrid, List<Vector2Int>> OnCursorInitializeRequested;
        public event Action<Vector2Int> OnCursorMoveRequested;
        public event Action<List<Vector2Int>> OnCursorRestrictionsRequested;
        public event Action OnCursorRestrictionsClearRequested;
        public event Action OnCursorHideRequested;
        public event Action OnCursorShowRequested;
        public event Action<Vector2Int, MapGrid> OnCursorPositionChanged;

        public void PublishCursorInitializeRequested(
            MapGrid mapGrid,
            List<Vector2Int> allowedPositions = null
        ) => OnCursorInitializeRequested?.Invoke(mapGrid, allowedPositions);

        public void PublishCursorMoveRequested(Vector2Int position) =>
            OnCursorMoveRequested?.Invoke(position);

        public void PublishCursorRestrictionsRequested(List<Vector2Int> allowedPositions) =>
            OnCursorRestrictionsRequested?.Invoke(allowedPositions);

        public void PublishCursorRestrictionsClearRequested() =>
            OnCursorRestrictionsClearRequested?.Invoke();

        public void PublishCursorHideRequested() => OnCursorHideRequested?.Invoke();

        public void PublishCursorShowRequested() => OnCursorShowRequested?.Invoke();

        public void PublishCursorPositionChanged(Vector2Int position, MapGrid mapGrid) =>
            OnCursorPositionChanged?.Invoke(position, mapGrid);

        #endregion

        #region Pre-Battle Map Events

        public event Action<MapGrid> OnPreBattleMapReady;
        public event Action<Vector2Int, CharacterInstance> OnPreBattleSpawnPositionSelected;
        public event Action<CharacterInstance> OnPreBattleSpawnPositionCanceled;

        public event Action OnUiPlayerIsTryingToUnselectLastUnit;

        public void PublishUiPlayerIsTryingToUnselectLastUnit() =>
            OnUiPlayerIsTryingToUnselectLastUnit?.Invoke();

        public void PublishPreBattleMapReady(MapGrid mapGrid) =>
            OnPreBattleMapReady?.Invoke(mapGrid);

        public event Action<
            Dictionary<MapGridPoint, float>,
            Dictionary<MapGridPoint, float>
        > OnValidTilesComputed;

        public void PublishValidTilesComputed(
            Dictionary<MapGridPoint, float> moveTiles,
            Dictionary<MapGridPoint, float> attackTiles
        ) => OnValidTilesComputed?.Invoke(moveTiles, attackTiles);

        public void PublishPreBattleSpawnPositionSelected(
            Vector2Int position,
            CharacterInstance unit
        ) => OnPreBattleSpawnPositionSelected?.Invoke(position, unit);

        public void PublishPreBattleSpawnPositionCanceled(CharacterInstance unit) =>
            OnPreBattleSpawnPositionCanceled?.Invoke(unit);

        #endregion

        #region Character Model Events

        public event Action<CharacterInstance> OnUnitModelSpawnRequested;
        public event Action<CharacterInstance> OnUnitModelChangeRequested;

        public void PublishUnitModelSpawnRequested(CharacterInstance unit) =>
            OnUnitModelSpawnRequested?.Invoke(unit);

        public void PublishUnitModelChangeRequested(CharacterInstance unit) =>
            OnUnitModelChangeRequested?.Invoke(unit);

        #endregion

        #region Placement Events

        public event Action OnPositioningModeEntered;
        public event Action OnPositioningModeExited;
        public event Action OnPlacementsInitialized;
        public event Action<CharacterInstance, bool> OnUnitSelectionChanged;

        public void PublishPositioningModeEntered() => OnPositioningModeEntered?.Invoke();

        public void PublishPositioningModeExited() => OnPositioningModeExited?.Invoke();

        public void PublishPlacementsInitialized() => OnPlacementsInitialized?.Invoke();

        public void PublishUnitSelectionChanged(CharacterInstance unit, bool selected) =>
            OnUnitSelectionChanged?.Invoke(unit, selected);

        #endregion

        #region Character Movement Events

        public event Action<CharacterInstance, MapGridPoint> OnCharacterMoveStarted;
        public event Action<CharacterInstance, MapGridPoint> OnCharacterMoveCompleted;
        public event Action<CharacterInstance> OnPlayerMovePreviewStarted;
        public event Action<CharacterInstance, MapGridPoint> OnPlayerChoseMoveTile;
        public event Action<CharacterInstance, Vector2Int> OnCharacterVisitedTile;

        public void PublishCharacterMoveStarted(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnCharacterMoveStarted?.Invoke(character, targetPoint);

        public void PublishCharacterMoveCompleted(
            CharacterInstance character,
            MapGridPoint targetPoint
        ) => OnCharacterMoveCompleted?.Invoke(character, targetPoint);

        public void PublishCharacterVisitedTile(
            CharacterInstance character,
            Vector2Int tilePosition
        ) => OnCharacterVisitedTile?.Invoke(character, tilePosition);

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

        #region Character Recruitment Events

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

        #endregion

        #region Character Spawn Events

        public event Action<CharacterInstance, Vector2Int> OnCharacterSpawned;
        public event Action<CharacterInstance, Vector2Int> OnCharacterRemovedFromSpawn;

        public void PublishCharacterSpawned(CharacterInstance character, Vector2Int position) =>
            OnCharacterSpawned?.Invoke(character, position);

        public void PublishCharacterRemovedFromSpawn(
            CharacterInstance character,
            Vector2Int position
        ) => OnCharacterRemovedFromSpawn?.Invoke(character, position);

        #endregion

        #region Save/Persistence Events

        public event Action OnSavePlayerRosterRequested;
        public event Action OnSavePlayerSettingsRequested;

        public void PublishSavePlayerRosterRequested() => OnSavePlayerRosterRequested?.Invoke();

        public void PublishSavePlayerSettingsRequested() => OnSavePlayerSettingsRequested?.Invoke();

        public event Action<PlayerSettings.GameplayPlayerSettings.InputControlType> OnInputControlTypeChanged;

        public void PublishInputControlTypeChanged(
            PlayerSettings.GameplayPlayerSettings.InputControlType newType
        ) => OnInputControlTypeChanged?.Invoke(newType);

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
        public event Action<CharacterInstance, ObjectItemInstance> OnItemEquipped;
        public event Action<CharacterInstance, ObjectItemInstance> OnItemUnequipped;

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

        #endregion

        #region Support Relationship Events

        public event Action<
            CharacterInstance,
            SupportRelationshipInstance
        > OnSupportRelationshipAdded;
        public event Action<CharacterInstance, CharacterData> OnSupportRelationshipRemoved;

        public void PublishSupportRelationshipAdded(
            CharacterInstance source,
            SupportRelationshipInstance relationship
        ) => OnSupportRelationshipAdded?.Invoke(source, relationship);

        public void PublishSupportRelationshipRemoved(
            CharacterInstance source,
            CharacterData target
        ) => OnSupportRelationshipRemoved?.Invoke(source, target);

        #endregion

        #region Battle Lifecycle Events

        public event Action OnBattleInputEnabled;
        public event Action OnBattleInputDisabled;

        public void PublishBattleInputEnabled() => OnBattleInputEnabled?.Invoke();

        public void PublishBattleInputDisabled() => OnBattleInputDisabled?.Invoke();

        public event Action OnBattleStarted;
        public event Action<BattleExitType> OnBattleCompleted;
        public event Action OnBattleContextInitialized;
        public event Action OnPreBattlePrepare;
        public event Action OnPreBattleStarted;
        public event Action OnPreBattleCompleted;

        public void PublishBattleStarted() => OnBattleStarted?.Invoke();

        public void PublishPreBattlePrepare() => OnPreBattlePrepare?.Invoke();

        public void PublishPreBattleStarted() => OnPreBattleStarted?.Invoke();

        public void PublishPreBattleCompleted() => OnPreBattleCompleted?.Invoke();

        public event Action OnPrecomputeCompleted;

        public void PublishPrecomputeCompleted() => OnPrecomputeCompleted?.Invoke();

        public event Action<BattleGameObject> OnBattleObjectSet;

        public void PublishBattleObjectSet(BattleGameObject battleObject) =>
            OnBattleObjectSet?.Invoke(battleObject);

        public event Action<MapGrid> OnBattleMapReady;

        public void PublishBattleMapReady(MapGrid mapGrid) => OnBattleMapReady?.Invoke(mapGrid);

        public event Action<BattlePreparationObject> OnBattlePrepObjectInitialized;

        public void PublishBattlePrepObjectInitialized(BattlePreparationObject prep) =>
            OnBattlePrepObjectInitialized?.Invoke(prep);

        #endregion

        #region Battle Turn Events

        public event Action OnTurnBegin;
        public event Action OnTurnEnded;
        public event Action<CharacterInstance> OnPlayerTurnStarted;
        public event Action OnPlayerTurnEnded;
        public event Action<PlayerTurnStates> OnPlayerTurnStateChanged;
        public event Action OnPlayerUndoAction;
        public event Action OnEnemyTurnStarted;
        public event Action OnEnemyTurnEnded;
        public event Action OnThirdPartyTurnStarted;
        public event Action OnThirdPartyTurnEnded;
        public event Action<CharacterInstance> OnUnitTurnEnded;
        public event Action<CharacterInstance> OnWaitActionRequested;
        public event Action<CharacterInstance> OnWaitActionConfirmed;

        public void PublishTurnBegin() => OnTurnBegin?.Invoke();

        public void PublishTurnEnded() => OnTurnEnded?.Invoke();

        public void PublishPlayerTurnStarted(CharacterInstance unit) =>
            OnPlayerTurnStarted?.Invoke(unit);

        public void PublishPlayerTurnEnded() => OnPlayerTurnEnded?.Invoke();

        public void PublishPlayerTurnStateChanged(PlayerTurnStates newState) =>
            OnPlayerTurnStateChanged?.Invoke(newState);

        public void PublishPlayerUndoAction() => OnPlayerUndoAction?.Invoke();

        public void PublishWaitActionRequested(CharacterInstance unit) =>
            OnWaitActionRequested?.Invoke(unit);

        public void PublishWaitActionConfirmed(CharacterInstance unit) =>
            OnWaitActionConfirmed?.Invoke(unit);

        public void PublishEnemyTurnStarted() => OnEnemyTurnStarted?.Invoke();

        public void PublishEnemyTurnEnded() => OnEnemyTurnEnded?.Invoke();

        public void PublishThirdPartyTurnStarted() => OnThirdPartyTurnStarted?.Invoke();

        public void PublishThirdPartyTurnEnded() => OnThirdPartyTurnEnded?.Invoke();

        public void PublishUnitTurnEnded(CharacterInstance unit) => OnUnitTurnEnded?.Invoke(unit);

        #endregion

        #region Battle Cursor Events

        public event Action<Vector2Int> OnBattleCursorMoved;

        public void PublishBattleCursorMoved(Vector2Int cursorPosition) =>
            OnBattleCursorMoved?.Invoke(cursorPosition);

        #endregion

        #region Battle Unit Action Events

        public event Action<CharacterInstance> OnPlayerControlledUnitActivated;
        public event Action<CharacterInstance, int> OnAllyDamaged;
        public event Action<CharacterInstance, int> OnEnemyDamaged;
        public event Action<CharacterInstance> OnUnitDefeated;
        public event Action<CharacterInstance, Vector2Int> OnUnitMoved;
        public event Action<CharacterInstance> OnUnitTakesAnotherTurn;
        public event Action<CharacterInstance> OnUnitFinishedMovingAfterAction;
        public event Action<CharacterInstance> OnCriticalHit;
        public event Action<CharacterInstance, int> OnWeaponUsesChanged;
        public event Action<CharacterInstance, CharacterInstance> OnLastAttackerSet;
        public event Action<CharacterInstance> OnLastAttackerCleared;
        public event Action<CharacterInstance, CharacterInstance> OnItemStolen;
        public event Action<CharacterInstance, BattleEmotion> OnUnitEmotionChanged;

        public void PublishPlayerControlledUnitActivated(CharacterInstance unit)
        {
            var handlers = OnPlayerControlledUnitActivated;
            if (handlers == null)
            {
                return;
            }

            foreach (var handler in handlers.GetInvocationList())
            {
                try
                {
                    ((Action<CharacterInstance>)handler).Invoke(unit);
                }
                catch (Exception ex)
                {
                    TurnrootLogger.Log(
                        $"PublishPlayerControlledUnitActivated: handler {handler.Method.Name} threw: {ex}",
                        TurnrootLogger.LogLevel.Error
                    );
                    throw;
                }
            }
        }

        public void PublishAllyDamaged(CharacterInstance unit, int damage) =>
            OnAllyDamaged?.Invoke(unit, damage);

        public void PublishEnemyDamaged(CharacterInstance unit, int damage) =>
            OnEnemyDamaged?.Invoke(unit, damage);

        public void PublishUnitDefeated(CharacterInstance unit) => OnUnitDefeated?.Invoke(unit);

        public void PublishUnitMoved(CharacterInstance unit, Vector2Int pos) =>
            OnUnitMoved?.Invoke(unit, pos);

        public void PublishUnitTakesAnotherTurn(CharacterInstance unit) =>
            OnUnitTakesAnotherTurn?.Invoke(unit);

        public event Action<CharacterInstance, MapGridPoint> OnMoveStarted; // logical start (includes target)
        public event Action<CharacterInstance, MapGridPoint> OnMoveCompleted; // logical completion (context)
        public event Action<CharacterInstance> OnMoveAnimationCompleted; // visual/animation completion

        public event Action<CharacterInstance> OnAttackStarted; // animation/visual start
        public event Action<CharacterInstance> OnAttackLogicCompleted; // backend logic completion
        public event Action<CharacterInstance> OnAttackAnimationCompleted; // animation completion

        public event Action<CharacterInstance> OnHealStarted;
        public event Action<CharacterInstance> OnHealLogicCompleted;
        public event Action<CharacterInstance> OnHealAnimationCompleted;

        public event Action<CharacterInstance, ObjectItemInstance> OnUseItemStarted;
        public event Action<CharacterInstance, ObjectItemInstance> OnUseItemLogicCompleted;
        public event Action<CharacterInstance, ObjectItemInstance> OnUseItemAnimationCompleted;

        public event Action<CharacterInstance> OnEndTurnCompleted;

        public void PublishMoveStarted(CharacterInstance unit, MapGridPoint targetPoint) =>
            OnMoveStarted?.Invoke(unit, targetPoint);

        public void PublishMoveCompleted(CharacterInstance unit, MapGridPoint targetPoint) =>
            OnMoveCompleted?.Invoke(unit, targetPoint);

        public void PublishMoveAnimationCompleted(CharacterInstance unit) =>
            OnMoveAnimationCompleted?.Invoke(unit);

        public void PublishAttackStarted(CharacterInstance unit) => OnAttackStarted?.Invoke(unit);

        public void PublishAttackLogicCompleted(CharacterInstance unit) =>
            OnAttackLogicCompleted?.Invoke(unit);

        public void PublishAttackAnimationCompleted(CharacterInstance unit) =>
            OnAttackAnimationCompleted?.Invoke(unit);

        public void PublishHealStarted(CharacterInstance unit) => OnHealStarted?.Invoke(unit);

        public void PublishHealLogicCompleted(CharacterInstance unit) =>
            OnHealLogicCompleted?.Invoke(unit);

        public void PublishHealAnimationCompleted(CharacterInstance unit) =>
            OnHealAnimationCompleted?.Invoke(unit);

        public void PublishUseItemStarted(CharacterInstance unit, ObjectItemInstance item) =>
            OnUseItemStarted?.Invoke(unit, item);

        public void PublishUseItemLogicCompleted(CharacterInstance unit, ObjectItemInstance item) =>
            OnUseItemLogicCompleted?.Invoke(unit, item);

        public void PublishUseItemAnimationCompleted(
            CharacterInstance unit,
            ObjectItemInstance item
        ) => OnUseItemAnimationCompleted?.Invoke(unit, item);

        public void PublishEndTurnCompleted(CharacterInstance unit) =>
            OnEndTurnCompleted?.Invoke(unit);

        public void PublishUnitFinishedMovingAfterAction(CharacterInstance unit) =>
            OnUnitFinishedMovingAfterAction?.Invoke(unit);

        public void PublishCriticalHit(CharacterInstance unit) => OnCriticalHit?.Invoke(unit);

        public void PublishWeaponUsesChanged(CharacterInstance unit, int change) =>
            OnWeaponUsesChanged?.Invoke(unit, change);

        public void PublishLastAttackerSet(CharacterInstance target, CharacterInstance attacker) =>
            OnLastAttackerSet?.Invoke(target, attacker);

        public void PublishLastAttackerCleared(CharacterInstance target) =>
            OnLastAttackerCleared?.Invoke(target);

        public void PublishItemStolen(CharacterInstance thief, CharacterInstance target) =>
            OnItemStolen?.Invoke(thief, target);

        public void PublishUnitBattleEmotionChanged(
            CharacterInstance unit,
            BattleEmotion emotion
        ) => OnUnitEmotionChanged?.Invoke(unit, emotion);

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
        public event Action<CharacterInstance> OnAllStatusEffectsCleared;

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
    }
}
