using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Maps;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Runtime context for the entire battle.
    /// Contains all the dynamic data that skills and other systems need at runtime.
    /// </summary>
    [RequireComponent(typeof(UI.Components.BattleOverlayManager))]
    public partial class BattleContext : MonoBehaviour
    {
        #region Core Properties and Initialization

        /// <summary>
        /// Reference to the Brain for publishing events.
        /// Set this when creating the BattleContext. Use Initialize() to assign.
        /// </summary>
        public Brain.Brain Brain { get; private set; }

        /// <summary>
        /// Active map graph for this battle.
        /// </summary>
        public MapGrid MapGrid { get; private set; }

        public BattleContextAIHelper AIHelper { get; private set; }
        public UI.Components.BattleOverlayManager OverlayManager { get; private set; }

        private void OnDestroy() => AIHelper?.Cleanup();

        /// <summary>
        /// Initialize the BattleContext with required dependencies. Throws if brain is null.
        /// </summary>
        public void Initialize(Brain.Brain brain, MapGrid mapGrid)
        {
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }

            Brain = brain;
            MapGrid = mapGrid;
            AIHelper = new BattleContextAIHelper(this);

            // ensure the overlay manager component exists and is initialized
            var overlay =
                GetComponent<UI.Components.BattleOverlayManager>()
                ?? gameObject.AddComponent<UI.Components.BattleOverlayManager>();
            OverlayManager = overlay;
        }

        #endregion

        #region Sub-Contexts and State

        // Sub-contexts for clearer separation
        public UnitContext Unit { get; private set; }
        public SkillContext Skill { get; private set; }
        public BattleParticipants Participants { get; private set; }
        public CombatFlags Flags { get; private set; }

        // Cache of computed tiles per unit to avoid duplicate pathfinding work
        private readonly Dictionary<string, CachedTileData> _unitTilesCache = new();

        // Cache precomputed pathfinding parameters (with and without weapon range) to avoid
        // repeated construction and unbounded stat lookups during AI pathfinding queries.
        private readonly Dictionary<string, PathfindingParameters> _cachedPathfindingParameters =
            new();
        private readonly Dictionary<
            string,
            PathfindingParameters
        > _cachedPathfindingParametersWithRange = new();

        private class CachedTileData
        {
            public Dictionary<MapGridPoint, float> MoveTiles;
            public Dictionary<MapGridPoint, float> AttackTiles;
            public int MapStateVersion; // Track map version to invalidate on map changes
            public Vector2Int UnitPosition; // Track position to invalidate on movement

            public CachedTileData(
                Dictionary<MapGridPoint, float> move,
                Dictionary<MapGridPoint, float> attack,
                int mapVersion,
                Vector2Int position
            )
            {
                MoveTiles = move;
                AttackTiles = attack;
                MapStateVersion = mapVersion;
                UnitPosition = position;
            }
        }

        public EnvironmentalConditions EnvironmentalConditions { get; set; }
        public Dictionary<string, object> CustomData { get; private set; }
        private readonly Dictionary<string, CharacterInstance> _lastAttackerByTarget = new();

        #endregion


        public bool IsPlayerControlledUnit(CharacterInstance unit) =>
            unit != null && Participants.Allies.Contains(unit);
    }
}
