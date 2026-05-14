using System;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Gameplay.Maps;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
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

        /// <summary>
        /// Fires once at the start of a combat exchange (before the first strike).
        /// Both attacker and defender are provided so both sides can fire their CombatStartsNode skills.
        /// </summary>
        public event Action<
            Characters.CharacterInstance,
            Characters.CharacterInstance
        > OnCombatStarted;

        /// <summary>
        /// Fires once after all strikes in a combat exchange have resolved.
        /// Both attacker and defender are provided so both sides can fire their PostCombatNode skills.
        /// </summary>
        public event Action<
            Characters.CharacterInstance,
            Characters.CharacterInstance
        > OnCombatEnded;

        public void PublishCombatStarted(
            Characters.CharacterInstance attacker,
            Characters.CharacterInstance defender
        ) => OnCombatStarted?.Invoke(attacker, defender);

        public void PublishCombatEnded(
            Characters.CharacterInstance attacker,
            Characters.CharacterInstance defender
        ) => OnCombatEnded?.Invoke(attacker, defender);

        #endregion
    }
}
