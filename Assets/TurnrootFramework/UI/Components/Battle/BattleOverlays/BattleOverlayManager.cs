using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components
{
    [RequireComponent(typeof(BattleContext))]
    public class BattleOverlayManager : MonoBehaviour
    {
        private Brain _brain;
        private GameObject _overlayInstance;
        private PassiveSkillOverlay _overlayComponent;
        private CharacterInstance _currentUnit;

        private void Awake()
        {
            var toggle = UIInputActionDefaults.ToggleDetails;
            if (toggle != null)
            {
                toggle.performed += OnToggleDetailsPerformed;
            }
        }

        private bool _initialized = false;
        private bool _unitSelected = false;

        private void OnDestroy()
        {
            UnsubscribeFromBrain();
            var toggle = UIInputActionDefaults.ToggleDetails;
            if (toggle != null)
            {
                toggle.performed -= OnToggleDetailsPerformed;
            }
        }

        private bool _passiveSkillsExpanded = false;

        private void OnToggleDetailsPerformed(InputAction.CallbackContext ctx) => ToggleDetails();

        private void ToggleDetails()
        {
            _passiveSkillsExpanded = !_passiveSkillsExpanded;
            _overlayComponent?.ToggleDetails(_passiveSkillsExpanded);
        }

        public void Initialize()
        {
            // this is called from the PreTurnUi timeline signal, in battlesceneflow
            if (!_initialized)
            {
                var battleContext = GetComponent<BattleContext>();
                _brain = battleContext.Brain;
                SubscribeToBrain();
                _initialized = true;
                "BattleOverlayManager initialized and subscribed to brain events.".LogInfo();
                if (_overlayInstance != null)
                {
                    Destroy(_overlayInstance);
                    _overlayInstance = null;
                    _overlayComponent = null;
                }
                var settings = GamewideUiSettings.Instance;
                if (settings != null && settings.PassiveSkillOverlayPrefab != null)
                {
                    _overlayInstance = Instantiate(settings.PassiveSkillOverlayPrefab);
                    _overlayComponent = _overlayInstance.GetComponent<PassiveSkillOverlay>();
                    _overlayInstance.SetActive(false);
                }
                "BattleOverlayManager: Passive skill overlay instantiated.".LogInfo();
            }
        }

        private void SubscribeToBrain()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnBattleStarted += HandleBattleStarted;
            _brain.OnBattleCompleted += HandleBattleEnded;
            _brain.OnCursorPositionChanged += HandleCursorPositionChanged;
            _brain.OnPlayerControlledUnitActivated += HandlePlayerControlledUnitActivated;
            _brain.OnPlayerTurnStateChanged += HandlePlayerTurnStateChanged;
            _brain.OnPlayerTurnStarted += HandlePlayerTurnStarted;
        }

        private void UnsubscribeFromBrain()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.OnBattleStarted -= HandleBattleStarted;
            _brain.OnBattleCompleted -= HandleBattleEnded;
            _brain.OnCursorPositionChanged -= HandleCursorPositionChanged;
            _brain.OnPlayerControlledUnitActivated -= HandlePlayerControlledUnitActivated;
            _brain.OnPlayerTurnStateChanged -= HandlePlayerTurnStateChanged;
            _brain.OnPlayerTurnStarted -= HandlePlayerTurnStarted;
        }

        private void HandleBattleStarted()
        {
            _currentUnit = null;
            _unitSelected = false;
        }

        private void HandlePlayerTurnStarted(CharacterInstance unit)
        {
            // Skills are evaluated when the player turn starts; if the overlay is already
            // showing a unit (e.g. from the camera-snap cursor move before evaluation ran)
            // force a refresh so the correct skill list appears.
            if (_currentUnit != null && _overlayInstance != null)
            {
                var stale = _currentUnit;
                _currentUnit = null;
                ShowForUnit(stale);
            }
        }

        private void HandleCursorPositionChanged(Vector2Int pos, MapGrid grid)
        {
            if (_unitSelected)
            {
                return;
            }

            if (_brain?.cursorBrain == null)
            {
                return;
            }

            if (_brain.cursorBrain.IsCursorOnUnit(out var unit))
            {
                ShowForUnit(unit);
            }
            else
            {
                HideOverlay();
            }
        }

        private void HandlePlayerControlledUnitActivated(CharacterInstance unit)
        {
            // Force refresh — skills may have been evaluated since the last time this unit was shown.
            _currentUnit = null;
            ShowForUnit(unit);
        }

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
            switch (newState)
            {
                case PlayerTurnStates.UnitSelected:
                case PlayerTurnStates.ChoosingDestination:
                case PlayerTurnStates.DestinationSelected:
                    _unitSelected = true;
                    break;

                case PlayerTurnStates.ExecutingMove:
                    HideOverlay();
                    _unitSelected = false;
                    break;

                case PlayerTurnStates.TurnEnded:
                case PlayerTurnStates.NoUnitSelected:
                    _unitSelected = false;
                    break;
            }
        }

        private void ShowForUnit(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            if (unit == _currentUnit)
            {
                return;
            }

            _currentUnit = unit;

            if (_overlayInstance == null)
            {
                return;
            }

            _overlayComponent?.ClearSkills();
            if (unit.ActivePassiveSkills != null)
            {
                foreach (var skill in unit.ActivePassiveSkills)
                {
                    _overlayComponent?.AddSkill(skill);
                }
            }

            UIFadeHelpers.ShowWithFade(_overlayInstance);
        }

        private void HideOverlay()
        {
            _currentUnit = null;
            if (_overlayInstance != null)
            {
                UIFadeHelpers.HideWithFade(_overlayInstance);
            }
        }

        private void HandleBattleEnded(BattleExitType exitType)
        {
            if (_overlayInstance != null)
            {
                Destroy(_overlayInstance);
                _overlayInstance = null;
                _overlayComponent = null;
            }
            _currentUnit = null;
        }
    }
}
