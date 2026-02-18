using System;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
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

        #endregion
    }
}
