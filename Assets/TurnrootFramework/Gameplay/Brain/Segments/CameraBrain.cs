using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    /// <summary>
    /// Controls the battle map camera positioning and follows cursor movement during gameplay.
    /// </summary>
    public partial class CameraBrain : BrainComponent
    {
        private Camera _battleMapCamera;
        private Vector3 _targetCameraPosition;
        private Vector3 _currentVelocity;
        private bool _shouldMove;
        private MapGrid mapGrid;
        private GamewideUiSettings UiSettings => Brain?.uiBrain?.uiSettings;
        private GameplayPlayerSettings gameplayPlayerSettings => GameplayPlayerSettings.Instance;
        private GameplayPlayerSettings.GameSpeed gameSpeed =>
            gameplayPlayerSettings?.SpeedSetting ?? GameplayPlayerSettings.GameSpeed.Normal;
        private BattleGameObject BattleObject => Brain?.battleBrain?.BattleObject;

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleCursorMoved += HandleCursorMoved;
            Brain.OnStateChanged += HandleStateChanged;
            Brain.OnBattleStarted += InitializeMapGrid;
            Brain.OnBattleStarted += HandleBattleStarted;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            Brain.OnBattleCursorMoved -= HandleCursorMoved;
            Brain.OnStateChanged -= HandleStateChanged;
            Brain.OnBattleStarted -= InitializeMapGrid;
            Brain.OnBattleStarted -= HandleBattleStarted;
        }

        public void InitializeMapGrid()
        {
            var grid = BattleObject?.MapGrid ?? Brain?.battleBrain?.PreparationObject?.MapGrid;
            SetMapGrid(grid);
        }

        public OperationResult SetMapGrid(MapGrid grid)
        {
            var validation = OperationResultGuards.RequireNotNull(grid, nameof(grid));
            if (!validation.Success)
            {
                return validation;
            }

            mapGrid = grid;
            return OperationResult.Successful();
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
