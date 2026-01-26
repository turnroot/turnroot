using System;
using Turnroot.Utilities;
using UnityEngine;

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

        public event Action<float> OnProgressChanged;

        private Utilities.AbstractScripts.DynamicSceneFlow GetDynamicSceneFlow() =>
            Brain?.GetComponentInChildren<Utilities.AbstractScripts.DynamicSceneFlow>(true)
            ?? FindFirstObjectByType<Utilities.AbstractScripts.DynamicSceneFlow>();

        private void ReportProgress()
        {
            var flow = GetDynamicSceneFlow();
            flow?.ReportLoadingProgress(LoadingPercentage);
            OnProgressChanged?.Invoke(LoadingPercentage / 100f);

            // Force canvas update to ensure visual feedback
            Canvas.ForceUpdateCanvases();
        }

        public void IncrementLoadedAmount()
        {
            LoadedAmount++;
            ReportProgress();
            CheckCompletion();
        }

        public void IncrementLoadedAmountBy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            LoadedAmount += amount;
            ReportProgress();
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            if (TotalToLoad > 0 && LoadedAmount >= TotalToLoad)
            {
                var flow = GetDynamicSceneFlow();
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
            ReportProgress();
        }

        public void Initialize()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
            ReportProgress();
        }

        public void Clear()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
            ReportProgress();
        }

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }
    }
}
