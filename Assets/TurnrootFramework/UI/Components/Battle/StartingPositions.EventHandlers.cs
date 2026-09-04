using System.Collections;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.PreBattle;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public partial class StartingPositions
    {
        private Coroutine _spawnDebounceCoroutine;
        private readonly float _spawnDebounceSeconds = 0.06f;

        private Coroutine _reinitDebounceCoroutine;
        private readonly float _reinitDebounceSeconds = 0.06f;

        private void HandlePlacementsInitialized_Impl()
        {
            // Debounce multiple rapid placements-initialized events
            if (_spawnDebounceCoroutine != null)
            {
                StopCoroutine(_spawnDebounceCoroutine);
            }
            _spawnDebounceCoroutine = StartCoroutine(DebouncedSpawn_Impl());
        }

        private IEnumerator DebouncedSpawn_Impl()
        {
            yield return new WaitForSeconds(_spawnDebounceSeconds);

            // Keep subscription so future placement changes will trigger a re-spawn

            if (_prepObject.placements != null && _prepObject.placements.Count > 0)
            {
                SpawnAllUnitModels();
            }
            _spawnDebounceCoroutine = null;
        }

        private void HandleUnitSelectionChanged_Impl(CharacterInstance unit, bool selected)
        {
            // Debounce unit-selection driven reinitialization so rapid selection changes
            // don't trigger repeated immediate work. The prep object will run a single
            // stable InitializePlacements() when the debounce completes.
            if (_prepObject == null)
            {
                return;
            }

            if (_reinitDebounceCoroutine != null)
            {
                StopCoroutine(_reinitDebounceCoroutine);
            }
            _reinitDebounceCoroutine = StartCoroutine(DebouncedReinit_Impl());
        }

        private IEnumerator DebouncedReinit_Impl()
        {
            yield return new WaitForSeconds(_reinitDebounceSeconds);
            _reinitDebounceCoroutine = null;
            _prepObject.InitializePlacements();
        }

        private void HandleBrainPrepInitialized_Impl(BattlePreparationObject prep)
        {
            if (prep != _prepObject)
            {
                return;
            }

            var brain = prep.Brain;
            if (brain != null)
            {
                // Unsubscribe via the wrapper delegate that was actually passed to the event,
                // not the _Impl method directly (which is a different delegate instance and
                // would silently fail to unsubscribe, leaving the handler firing indefinitely).
                brain.OnBattlePrepObjectInitialized -= HandleBrainPrepInitialized;
            }

            SubscribeToEvents();
            if (_prepObject.placements != null && _prepObject.placements.Count > 0)
            {
                SpawnAllUnitModels();
            }
        }
    }
}
