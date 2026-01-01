using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.UI.Components.RadialMenu;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        [HideInInspector]
        public GameObject PreBattleMenuInstance { get; private set; }

        [HideInInspector]
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
            // Clean up any existing menu first
            if (PreBattleMenuInstance != null)
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: Cleaning up existing PreBattleMenuInstance.");
#endif
                CleanupPreBattleMenu();
            }

            // Instantiate the pre-battle menu prefab
            if (uiSettings.PreBattleMenuPrefab != null)
            {
#if UNITY_EDITOR
                Debug.Log("UiBrain: Instantiating PreBattleMenuPrefab.");
#endif
                PreBattleMenuInstance = Instantiate(uiSettings.PreBattleMenuPrefab);
                var uiFade = PreBattleMenuInstance.AddComponent<UIFade>();
                uiFade.lerpTime = 0.8f;
                var radialMenu = PreBattleMenuInstance.GetComponent<RadialMenu>();
                radialMenu.uiBrain = this;
                radialMenu.OnNavigate += HandlePreBattleMenuNavigate;
                radialMenu.OnItemSelected += HandlePreBattleMenuSelect;
            }
        }

        private void CleanupPreBattleMenu()
        {
            if (PreBattleMenuInstance != null)
            {
                var radialMenu = PreBattleMenuInstance.GetComponent<RadialMenu>();
                if (radialMenu != null)
                {
                    // Unsubscribe from events to prevent orphaned references
                    radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                    radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                }

                Destroy(PreBattleMenuInstance);
                PreBattleMenuInstance = null;
            }
        }
    }
}
