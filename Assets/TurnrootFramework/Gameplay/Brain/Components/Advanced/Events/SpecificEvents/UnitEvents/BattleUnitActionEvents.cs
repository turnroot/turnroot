using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Characters.CharacterInstance;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
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
                    // Log which handler failed, then rethrow to expose the bug
                    $"PublishPlayerControlledUnitActivated: handler {handler.Method.Name} in {handler.Method.DeclaringType.Name} threw exception: {ex}".LogError();
                    throw; // Don't hide the exception - this is a real bug
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
    }
}
