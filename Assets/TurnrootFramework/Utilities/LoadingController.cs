using System;
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

        // guard so we only fire the scene flow once when we first hit 100%
        private bool _completionTriggered = false;

        protected int LoadingPercentage =>
            TotalToLoad == 0 ? 0 : (int)((float)LoadedAmount / TotalToLoad * 100);

        public event Action<float> OnProgressChanged;

        private Utilities.AbstractScripts.DynamicSceneFlow GetDynamicSceneFlow() =>
            Brain?.GetComponentInChildren<Utilities.AbstractScripts.DynamicSceneFlow>(true)
            ?? FindFirstObjectByType<Utilities.AbstractScripts.DynamicSceneFlow>();

        private void ReportProgress()
        {
            var flow = GetDynamicSceneFlow();
            float normalized = TotalToLoad == 0 ? 0f : (float)LoadedAmount / TotalToLoad;
            flow?.ReportLoadingProgress(normalized);
            OnProgressChanged?.Invoke(normalized);
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
            if (TotalToLoad > 0 && LoadedAmount >= TotalToLoad && !_completionTriggered)
            {
                _completionTriggered = true;
                var flow = GetDynamicSceneFlow();
                flow?.Progress();
            }
        }

        public void IncreaseLoadTotal()
        {
            TotalToLoad++;
            if (LoadedAmount < TotalToLoad)
            {
                _completionTriggered = false;
            }
        }

        public void IncreaseLoadTotalBy(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            TotalToLoad += amount;
            if (LoadedAmount < TotalToLoad)
            {
                _completionTriggered = false;
            }
            ReportProgress();
        }

        public void Initialize()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
            _completionTriggered = false;
            ReportProgress();
        }

        public void Clear()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
            _completionTriggered = false;
            ReportProgress();
        }

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }
    }
}
