using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Tracks loading/precompute tasks across brain states and reports progress
    /// </summary>
    public class LoadingController : BrainComponent
    {
        protected override void Awake() => base.Awake();

        private int LoadedAmount { get; set; } = 0;
        private int TotalToLoad { get; set; } = 0;
        protected int LoadingPercentage =>
            TotalToLoad == 0 ? 100 : (int)((float)LoadedAmount / TotalToLoad * 100);

        /// <summary>
        /// Increments the loaded amount and notifies listeners. When complete, attempts to advance the scene flow.
        /// </summary>
        private Utilities.AbstractScripts.DynamicSceneFlow GetDynamicSceneFlow()
        {
            var flow = Brain?.GetComponentInChildren<Utilities.AbstractScripts.DynamicSceneFlow>(
                true
            );
            if (flow != null)
            {
                TurnrootLogger.Log(
                    "LoadingController.GetDynamicSceneFlow: using Brain child DynamicSceneFlow"
                );
                return flow;
            }

            flow = FindFirstObjectByType<Utilities.AbstractScripts.DynamicSceneFlow>();
            TurnrootLogger.Log(
                $"LoadingController.GetDynamicSceneFlow: FindFirstObjectByType returned {(flow != null)}"
            );
            return flow;
        }

        public void IncrementLoadedAmount()
        {
            LoadedAmount++;
            TurnrootLogger.Log($"LoadingController: Reporting progress {LoadingPercentage}%");
            var flowProgress = GetDynamicSceneFlow();
            flowProgress?.ReportLoadingProgress(LoadingPercentage);
            if (TotalToLoad > 0 && LoadedAmount >= TotalToLoad)
            {
                var flow = GetDynamicSceneFlow();
                TurnrootLogger.Log(
                    $"LoadingController: Completed loading (Loaded:{LoadedAmount}, Total:{TotalToLoad})"
                );
                if (flow == null)
                {
                    TurnrootLogger.Log(
                        "LoadingController: No DynamicSceneFlow found to progress",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
                flow?.Progress();
            }
        }

        public void IncreaseLoadTotal() => TotalToLoad++;

        public void IncreaseLoadTotalBy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalToLoad += amount;
            GetDynamicSceneFlow()?.ReportLoadingProgress(LoadingPercentage);
        }

        public void IncrementLoadedAmountBy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            LoadedAmount += amount;
            TurnrootLogger.Log(
                $"LoadingController: Reporting progress {LoadingPercentage}% (by {amount})"
            );
            var flowProgress = GetDynamicSceneFlow();
            flowProgress?.ReportLoadingProgress(LoadingPercentage);
            if (TotalToLoad > 0 && LoadedAmount >= TotalToLoad)
            {
                var flow = GetDynamicSceneFlow();
                TurnrootLogger.Log(
                    $"LoadingController: Completed loading (Loaded:{LoadedAmount}, Total:{TotalToLoad})"
                );
                if (flow == null)
                {
                    TurnrootLogger.Log(
                        "LoadingController: No DynamicSceneFlow found to progress",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
                flow?.Progress();
            }
        }

        public void Initialize()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
            GetDynamicSceneFlow()?.ReportLoadingProgress(LoadingPercentage);
        }

        public void Clear()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
            GetDynamicSceneFlow()?.ReportLoadingProgress(LoadingPercentage);
        }

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }
    }
}
