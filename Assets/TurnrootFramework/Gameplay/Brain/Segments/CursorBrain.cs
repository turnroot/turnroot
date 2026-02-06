using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages the player's cursor position and movement on the map grid during battle and pre-battle phases.
    /// </summary>
    public partial class CursorBrain : BrainComponent
    {
        // Core fields required by other partials
        private GameObject _cursorInstance;
        private MapGrid _currentMap;
        private List<Vector2Int> _allowedPositions;
        private int _currentPositionIndex;
        private CursorContext _currentContext = CursorContext.None;

        [HideInInspector]
        public MapGridPoint CursorPosition;

        [HideInInspector]
        public GamewideUiSettings uiSettings;

        private float inputThreshold = 0.5f;

        private enum CursorContext
        {
            None,
            Battle,
            PreBattle,
        }

        public bool IsInitialized { get; private set; } = false;
        public bool IsVisible { get; private set; } = true;

        public OperationResult SetUiSettingsReference(GamewideUiSettings u)
        {
            uiSettings = u;
            return uiSettings != null
                ? OperationResult.Successful()
                : OperationResult.Failure("Invalid gamewideUiSettings");
        }

        protected override void Awake() => base.Awake();

        protected override void OnDestroy()
        {
            CleanupCursor();
            base.OnDestroy();
        }

        public string GetCurrentContext() => _currentContext.ToString();
    }
}
