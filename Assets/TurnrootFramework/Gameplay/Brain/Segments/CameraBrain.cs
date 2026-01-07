using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class CameraBrain : BrainComponent
    {
        private Camera _battleMapCamera;
        private Vector3 _targetCameraPosition;
        private Vector3 _currentVelocity;
        private bool _inCombat;

        private MapGrid MapGrid => Brain?.battleBrain?.BattleObject?.Context?.mapGrid;
        private GamewideUiSettings UiSettings => Brain?.uiBrain?.uiSettings;
        private BattleGameObject BattleObject => Brain?.battleBrain?.BattleObject;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleCursorMoved += HandleCursorMoved;
            Brain.OnStateChanged += HandleStateChanged;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnBattleCursorMoved -= HandleCursorMoved;
            Brain.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(BrainState newState)
        {
            _inCombat = newState?.Name == BrainStateNames.Battle;
        }

        private void Update()
        {
            if (_inCombat)
            {
                HandleBattleMapCameraPan();
            }
        }
    }
}
