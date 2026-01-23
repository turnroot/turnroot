using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.AbstractScripts
{
    [RequireComponent(typeof(DynamicSceneFlow))]
    public class LoadingController : MonoBehaviour
    {
        private void Awake()
        {
            OnLoadedAmountChanged.AddListener(
                (percentage) => OnLoadedAmountChangedAction?.Invoke(percentage)
            );
        }

        private int LoadedAmount { get; set; } = 0;
        private int TotalToLoad { get; set; } = 1;
        protected int LoadingPercentage => (int)((float)LoadedAmount / TotalToLoad * 100);

        [InfoBox(
            "Loading progress will be reported through this event- used for UI updates. When loading is complete, the DynamicSceneFlow will progress to the next segment. Make sure your DynamicSceneFlow segments are ordered correctly"
        )]
        public UnityEvent<int> OnLoadedAmountChanged = new();
        public event Action<int> OnLoadedAmountChangedAction;

        public void IncrementLoadedAmount()
        {
            LoadedAmount++;
            OnLoadedAmountChanged.Invoke(LoadingPercentage);
            if (LoadedAmount >= TotalToLoad)
            {
                DynamicSceneFlow flow = GetComponent<DynamicSceneFlow>();
                flow.Progress();
            }
        }

        public void IncreaseLoadTotal() => TotalToLoad++;

        public void Initialize()
        {
            LoadedAmount = 0;
            TotalToLoad = 1;
            OnLoadedAmountChanged.Invoke(LoadingPercentage);
        }

        public void Clear()
        {
            LoadedAmount = 0;
            TotalToLoad = 0;
        }
    }
}
