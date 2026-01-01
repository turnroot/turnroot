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
#if UNITY_EDITOR
                Debug.Log($"UiBrain: Brain state changed to {name}");
#endif
                switch (name)
                {
                    case BrainStateNames.PreBattle:
                        HandlePreBattleUi();
                        break;
                }
            };

            Brain.OnStateChanged += _onStateChangedHandler;
            // If the Brain already has an active state, invoke handler immediately so UI can react to the current state
            var current = Brain?.stateBrain?.CurrentState;
            if (current != null)
            {
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
#if UNITY_EDITOR
            Debug.Log("UiBrain: Handling PreBattle UI setup.");
#endif
            // Instantiate the pre-battle menu prefab
            if (uiSettings.PreBattleMenuPrefab != null)
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: Instantiating PreBattleMenuPrefab.");
#endif
                Instantiate(uiSettings.PreBattleMenuPrefab);
            }
        }
    }
}
