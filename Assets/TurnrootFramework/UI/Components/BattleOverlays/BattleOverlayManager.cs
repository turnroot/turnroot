using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class BattleOverlayManager : MonoBehaviour
    {
        private Brain _brain;
        private GameObject _overlayInstance;
        private PassiveSkillOverlay _overlayComponent;
        private CharacterInstance _currentUnit;

        private void OnDestroy()
        {
            UnsubscribeFromBrain();
        }

        public void Initialize(Brain brain)
        {
            if (brain == null)
            {
                return;
            }

            _brain = brain;
            SubscribeToBrain();
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
            _brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
            _brain.OnPlayerTurnStateChanged += HandlePlayerTurnStateChanged;
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
            _brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
            _brain.OnPlayerTurnStateChanged -= HandlePlayerTurnStateChanged;
        }

        private void HandleBattleStarted()
        {
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

            _currentUnit = null;
        }

        private void HandleCursorPositionChanged(Vector2Int pos, MapGrid grid)
        {
            if (_brain?.cursorBrain != null && _brain.cursorBrain.IsCursorOnUnit(out var unit))
            {
                ShowForUnit(unit);
            }
            else
            {
                HideOverlay();
            }
        }

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected)
        {
            if (selected)
            {
                ShowForUnit(unit);
            }
            else
            {
                HideOverlay();
            }
        }

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
            if (newState == PlayerTurnStates.ExecutingMove)
            {
                HideOverlay();
            }
        }

        private void ShowForUnit(CharacterInstance unit)
        {
            if (unit == null || unit == _currentUnit)
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
                    var icon = skill?.Badge?.RuntimeSprite;
                    _overlayComponent?.AddSkill(skill?.SkillName ?? string.Empty, icon);
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
