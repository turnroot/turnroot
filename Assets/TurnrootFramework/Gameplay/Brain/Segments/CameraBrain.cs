using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.PlayerSettings;
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
        private bool _shouldMove;

        private MapGrid mapGrid;
        private GamewideUiSettings UiSettings => Brain?.uiBrain?.uiSettings;

        private GameplayPlayerSettings gameplayPlayerSettings =>
            GameSettingsLoader.LoadFirst<GameplayPlayerSettings>();

        private GameplayPlayerSettings.GameSpeed gameSpeed =>
            gameplayPlayerSettings?.SpeedSetting ?? GameplayPlayerSettings.GameSpeed.Normal;
        private BattleGameObject BattleObject => Brain?.battleBrain?.BattleObject;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleCursorMoved += HandleCursorMoved;
            Brain.OnStateChanged += HandleStateChanged;
            Brain.OnBattleStarted += InitializeMapGrid;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnBattleCursorMoved -= HandleCursorMoved;
            Brain.OnStateChanged -= HandleStateChanged;
            Brain.OnBattleStarted -= InitializeMapGrid;
        }

        public void InitializeMapGrid() =>
            SetMapGrid(BattleObject != null ? BattleObject.MapGrid : null);

        public OperationResult SetMapGrid(MapGrid grid)
        {
            if (grid == null)
            {
                return OperationResult.Failure("MapGrid is null");
            }

            mapGrid = grid;
            return OperationResult.SuccessResult();
        }

        private void HandleStateChanged(BrainState newState) =>
            _shouldMove = newState?.Name == BrainStateNames.Battle;

        private void Update()
        {
            if (_shouldMove)
            {
                HandleBattleMapCameraPan();
            }
        }
    }
}
