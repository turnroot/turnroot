using System;
using System.Collections.Generic;
using NaughtyAttributes;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay.Combat
{
    [CreateAssetMenu(
        fileName = "MapExplorationTable",
        menuName = "Turnroot/Gameplay/Map Exploration Table"
    )]
    public class MapExplorationTable : SingletonScriptableObject<MapExplorationTable>
    {
        [Serializable]
        public struct BattleExplorationEntry
        {
            [HorizontalLine(color: EColor.Gray)]
            [Tooltip("The battle scene. Drag the scene asset here — name matching is automatic.")]
            public SceneReference BattleScene;

            [Tooltip("Starting exploration state for the top-left quadrant of the map.")]
            public QuadrantExploredState InitialTopLeft;

            [Tooltip("Starting exploration state for the top-right quadrant of the map.")]
            public QuadrantExploredState InitialTopRight;

            [Tooltip("Starting exploration state for the bottom-left quadrant of the map.")]
            public QuadrantExploredState InitialBottomLeft;

            [Tooltip("Starting exploration state for the bottom-right quadrant of the map.")]
            public QuadrantExploredState InitialBottomRight;
        }

        [InfoBox(
            "Add one entry per battle that uses the quadrant exploration map display. "
                + "Drag the scene asset into BattleScene — names are matched automatically."
        )]
        [ReorderableList]
        public List<BattleExplorationEntry> Entries = new();

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="ExploredStatus"/> for <paramref name="battleSceneName"/>.
        /// Reads from LTM if a saved value exists; otherwise seeds LTM from this table's
        /// initial values and returns those.  If <paramref name="ltm"/> is null, returns
        /// the initial values without persisting anything.
        /// </summary>
        public ExploredStatus Initialize(string battleSceneName, LongTermMemory ltm)
        {
            if (string.IsNullOrEmpty(battleSceneName))
            {
                return default;
            }

            // Try loading from LTM first.
            if (ltm != null)
            {
                var saved = ltm.Recall(LtmKeys.MapExplorationKey(battleSceneName));
                if (!string.IsNullOrEmpty(saved) && TryDecodeStatus(saved, out var loaded))
                {
                    return loaded;
                }
            }

            // LTM miss — fall back to the table's initial values.
            var initial = GetInitialStatus(battleSceneName);

            if (ltm != null)
            {
                SaveStatusToLtm(battleSceneName, initial, ltm);
            }

            return initial;
        }

        /// <summary>
        /// Returns the initial <see cref="ExploredStatus"/> defined in this table for
        /// <paramref name="battleSceneName"/>, without reading or writing LTM.
        /// Returns an all-<see cref="QuadrantExploredState.NotExplored"/> status if no
        /// entry exists for the given name.
        /// </summary>
        public ExploredStatus GetInitialStatus(string battleSceneName)
        {
            if (TryGetEntry(battleSceneName, out var entry))
            {
                return new ExploredStatus
                {
                    TopLeft = entry.InitialTopLeft,
                    TopRight = entry.InitialTopRight,
                    BottomLeft = entry.InitialBottomLeft,
                    BottomRight = entry.InitialBottomRight,
                };
            }

            return default;
        }

        /// <summary>
        /// Updates a single quadrant's exploration state, saves it back to LTM, and returns
        /// the full updated <see cref="ExploredStatus"/>.
        /// Call this from battle code whenever a quadrant is explored at runtime.
        /// </summary>
        public ExploredStatus SetQuadrantState(
            string battleSceneName,
            MapQuadrant quadrant,
            QuadrantExploredState newState,
            LongTermMemory ltm
        )
        {
            var status = Initialize(battleSceneName, ltm);

            switch (quadrant)
            {
                case MapQuadrant.TopLeft:
                    status.TopLeft = newState;
                    break;
                case MapQuadrant.TopRight:
                    status.TopRight = newState;
                    break;
                case MapQuadrant.BottomLeft:
                    status.BottomLeft = newState;
                    break;
                case MapQuadrant.BottomRight:
                    status.BottomRight = newState;
                    break;
            }

            SaveStatusToLtm(battleSceneName, status, ltm);
            return status;
        }

        /// <summary>
        /// Writes an updated <see cref="ExploredStatus"/> for <paramref name="battleSceneName"/>
        /// back to LTM so exploration progress is preserved across sessions.
        /// </summary>
        public void SaveStatusToLtm(
            string battleSceneName,
            ExploredStatus status,
            LongTermMemory ltm
        )
        {
            if (string.IsNullOrEmpty(battleSceneName) || ltm == null)
            {
                return;
            }

            ltm.Remember(LtmKeys.MapExplorationKey(battleSceneName), EncodeStatus(status));
        }

        /// <summary>
        /// Tries to find the entry whose <see cref="BattleExplorationEntry.BattleSceneName"/>
        /// matches <paramref name="battleSceneName"/> (case-sensitive).
        /// </summary>
        public bool TryGetEntry(string battleSceneName, out BattleExplorationEntry entry)
        {
            if (Entries != null)
            {
                foreach (var e in Entries)
                {
                    if (e.BattleScene != null && e.BattleScene.SceneName == battleSceneName)
                    {
                        entry = e;
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        // ── Codec ─────────────────────────────────────────────────────────────
        // Statuses are stored as "TL,TR,BL,BR" where each token is the integer
        // value of the corresponding QuadrantExploredState enum member.

        private static string EncodeStatus(ExploredStatus s) =>
            $"{(int)s.TopLeft},{(int)s.TopRight},{(int)s.BottomLeft},{(int)s.BottomRight}";

        private static bool TryDecodeStatus(string raw, out ExploredStatus status)
        {
            status = default;

            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            var parts = raw.Split(',');
            if (parts.Length != 4)
            {
                return false;
            }

            if (
                !int.TryParse(parts[0], out var tl)
                || !int.TryParse(parts[1], out var tr)
                || !int.TryParse(parts[2], out var bl)
                || !int.TryParse(parts[3], out var br)
            )
            {
                return false;
            }

            status.TopLeft = (QuadrantExploredState)tl;
            status.TopRight = (QuadrantExploredState)tr;
            status.BottomLeft = (QuadrantExploredState)bl;
            status.BottomRight = (QuadrantExploredState)br;
            return true;
        }
    }
}
