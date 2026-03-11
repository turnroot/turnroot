using System;
using Turnroot.Characters;
using Turnroot.Maps.Components.Grids;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    /// <summary>
    /// Represents a single grid cell on the map with terrain type, spawn point data, and neighbor connectivity.
    /// </summary>
    public partial class MapGridPoint : MonoBehaviour
    {
        [SerializeField]
        private SpawnPoint _spawnPoint = new();
        public SpawnPoint SpawnPoint
        {
            get => _spawnPoint;
            set => _spawnPoint = value ?? new SpawnPoint();
        }

        private void OnValidate()
        {
            SpawnPoint ??= new SpawnPoint();
            InvalidateTerrainTypeCache();
        }

        /* ---------------------------- Grid point data ---------------------------- */
        private static readonly (string name, int dRow, int dCol)[] Directions = new[]
        {
            ("N", -1, 0),
            ("NE", -1, 1),
            ("E", 0, 1),
            ("SE", 1, 1),
            ("S", 1, 0),
            ("SW", 1, -1),
            ("W", 0, -1),
            ("NW", -1, -1),
        };

        private static readonly (string name, int dRow, int dCol)[] CardinalDirections = new[]
        {
            ("N", -1, 0),
            ("E", 0, 1),
            ("S", 1, 0),
            ("W", 0, -1),
        };

        private const float GizmoRadius = 0.35f;

        // Starting unit tracking (unrelated to the old property system)
        private CharacterInstance _startingUnit = null;

        [HideInInspector, NonSerialized]
        /// <summary>
        /// The character instance currently occupying this grid point.
        /// This should be set when a character enters the grid point and cleared when the character leaves.
        /// Used to track which character, if any, is present at this location.
        /// </summary>
        public CharacterInstance CurrentInstance;

        /// <summary>
        /// Indicates whether a character is currently occupying this grid point.
        /// Returns true if <see cref="CurrentInstance"/> is not null.
        /// </summary>
        public bool IsOccupied => CurrentInstance != null;

        [SerializeField]
        private int _row;
        public int Row => _row;

        [SerializeField]
        private int _col;
        public int Col => _col;

        [SerializeField]
        [Tooltip("Terrain type")]
        private string _terrainTypeId = string.Empty;
        public string TerrainTypeId => _terrainTypeId;

        public void Initialize(int row, int col)
        {
            _row = row;
            _col = col;
        }

        /* ---------------------------- Grid Point Property Accessors ---------------------------- */

        public CharacterInstance GetStartingUnit() => _startingUnit;

        public void SetStartingUnit(CharacterInstance unit) => _startingUnit = unit;
    }
}

