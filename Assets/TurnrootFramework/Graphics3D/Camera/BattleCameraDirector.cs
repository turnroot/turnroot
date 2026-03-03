using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components.Battle;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Graphics3D.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class BattleCameraDirector : MonoBehaviour
    {
        public bool IsInTopdownBattleView { get; private set; }
        public UnityEngine.Camera ThisCamera => GetComponent<UnityEngine.Camera>();
        private Brain _brain;

        private BattleSceneFlow _battleSceneFlow;

        public void Initialize(Brain brain)
        {
            _brain = brain;
            _battleSceneFlow = brain.battleBrain.turnRotisserie._sceneFlow;
            _brain.OnPlayerTurnStateChanged += HandlePlayerTurnStateChanged;
        }

        private void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnPlayerTurnStateChanged -= HandlePlayerTurnStateChanged;
            }
        }

        private void HandlePlayerTurnStateChanged(PlayerTurnStates newState)
        {
            IsInTopdownBattleView = newState != PlayerTurnStates.ExecutingAction;
            _battleSceneFlow.IsInTopdownBattleView = IsInTopdownBattleView;
        }
    }
}
