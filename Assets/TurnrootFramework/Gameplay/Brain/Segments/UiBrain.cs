using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public class UiBrain : BrainComponent
    {
        public GamewideUiSettings uiSettings;

        protected override void Awake()
        {
            base.Awake();
            uiSettings = GameSettingsLoader.LoadFirst<GamewideUiSettings>();
#if UNITY_EDITOR
            Debug.Log($"UiBrain Awake - Brain present: {Brain != null}");
#endif
        }

        private System.Action<BrainState> _onStateChangedHandler;

        protected override void SubscribeToBrainEvents()
        {
            _onStateChangedHandler = (state) =>
            {
                var name = state?.Name ?? string.Empty;
                Debug.Log($"UiBrain: Brain state changed to {name}");
                switch (name)
                {
                    case BrainStateNames.PreBattle:
                        HandlePreBattleUi();
                        break;
                }
            };

            Brain.OnStateChanged += _onStateChangedHandler;
#if UNITY_EDITOR
            Debug.Log("UiBrain: Subscribed to Brain.OnStateChanged");
#endif

            // If the Brain already has an active state, invoke handler immediately so UI can react to the current state
            var current = Brain?.stateBrain?.CurrentState;
            if (current != null)
            {
#if UNITY_EDITOR
                Debug.Log(
                    $"UiBrain: Invoking state handler immediately for current state: {current.Name}"
                );
#endif
                _onStateChangedHandler(current);
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (_onStateChangedHandler != null)
            {
                Brain.OnStateChanged -= _onStateChangedHandler;
                _onStateChangedHandler = null;
            }
        }

        public void HandlePreBattleUi()
        {
            Debug.Log("UiBrain: Handling PreBattle UI setup.");
            // Instantiate the pre-battle menu prefab
            if (uiSettings.PreBattleMenuPrefab != null)
            {
                Debug.Log("UiBrain: Instantiating PreBattleMenuPrefab.");
                Instantiate(uiSettings.PreBattleMenuPrefab);
            }
        }
    }
}
