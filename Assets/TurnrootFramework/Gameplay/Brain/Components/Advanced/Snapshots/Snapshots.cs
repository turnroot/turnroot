using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Snapshots
{
    /// <summary>
    /// Lightweight snapshot of battle state using dictionary-based storage.
    /// Captures only essential state that can change during battle.
    /// </summary>
    [Serializable]
    public class Snapshot
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public int TurnNumber { get; set; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        // Unit states keyed by unit ID
        private readonly Dictionary<string, UnitState> _units = new();

        /// <summary>
        /// Captures the current state of a unit.
        /// </summary>
        public void CaptureUnit(CharacterInstance unit)
        {
            if (unit == null)
            {
                return;
            }

            _units[unit.Id] = new UnitState
            {
                Position = unit.MapGridPosition,
                IsDefeated = unit.IsDefeatedInCurrentBattle,
                Stats = CaptureStats(unit),
            };
        }

        /// <summary>
        /// Restores a unit to its captured state.
        /// </summary>
        public bool RestoreUnit(CharacterInstance unit, MapGrid mapGrid)
        {
            if (unit == null || !_units.TryGetValue(unit.Id, out var state))
            {
                return false;
            }

            // Restore position
            if (mapGrid != null && unit.MapGridPosition != state.Position)
            {
                unit.MoveToPosition(state.Position, mapGrid);
            }

            // Restore defeated status
            unit.IsDefeatedInCurrentBattle = state.IsDefeated;

            // Restore stats
            RestoreStats(unit, state.Stats);

            return true;
        }

        private Dictionary<string, float> CaptureStats(CharacterInstance unit)
        {
            var stats = new Dictionary<string, float>();

            // Capture bounded stats (HP, MP, etc.)
            foreach (
                Characters.Stats.BoundedStatType type in Enum.GetValues(
                    typeof(Characters.Stats.BoundedStatType)
                )
            )
            {
                var stat = unit.GetBoundedStat(type);
                if (stat != null)
                {
                    stats[$"B_{type}"] = stat.Current;
                }
            }

            // Capture unbounded stats (Str, Def, etc.)
            foreach (
                Characters.Stats.UnboundedStatType type in Enum.GetValues(
                    typeof(Characters.Stats.UnboundedStatType)
                )
            )
            {
                var stat = unit.GetUnboundedStat(type);
                if (stat != null)
                {
                    stats[$"U_{type}"] = stat.Current;
                }
            }

            return stats;
        }

        private void RestoreStats(CharacterInstance unit, Dictionary<string, float> stats)
        {
            foreach (var kvp in stats)
            {
                if (
                    kvp.Key.StartsWith("B_")
                    && Enum.TryParse<Characters.Stats.BoundedStatType>(kvp.Key[2..], out var bType)
                )
                {
                    unit.GetBoundedStat(bType)?.SetCurrent(kvp.Value);
                }
                else if (
                    kvp.Key.StartsWith("U_")
                    && Enum.TryParse<Characters.Stats.UnboundedStatType>(
                        kvp.Key[2..],
                        out var uType
                    )
                )
                {
                    unit.GetUnboundedStat(uType)?.SetCurrent(kvp.Value);
                }
            }
        }

        public IEnumerable<string> GetCapturedUnitIds() => _units.Keys;

        [Serializable]
        private class UnitState
        {
            public Vector2Int Position;
            public bool IsDefeated;
            public Dictionary<string, float> Stats;

            // Mark whether this unit was spawned during battle after the snapshot was taken.
            // This field is reserved for future logic (e.g., skip restoring spawned reinforcements).
            public bool WasSpawned;
        }
    }

    /// <summary>
    /// Manages a ring buffer of snapshots for undo/preview functionality.
    /// </summary>
    public class SnapshotStack
    {
        private readonly Snapshot[] _buffer;
        private int _head;
        private int _count;

        public int Count => _count;
        public int Capacity { get; }

        public event Action<Snapshot> OnSnapshotTaken;
        public event Action<Snapshot> OnSnapshotRestored;

        public SnapshotStack(int capacity = 10)
        {
            Capacity = Math.Max(1, capacity);
            _buffer = new Snapshot[Capacity];
        }

        /// <summary>
        /// Takes a snapshot of all units in the battle.
        /// </summary>
        public Snapshot Take(
            BattleContext context,
            IEnumerable<CharacterInstance> units,
            int turnNumber
        )
        {
            var snapshot = new Snapshot { TurnNumber = turnNumber };

            foreach (var unit in units)
            {
                snapshot.CaptureUnit(unit);
            }

            Push(snapshot);
            OnSnapshotTaken?.Invoke(snapshot);

#if UNITY_EDITOR
            Debug.Log($"[Snapshot] Captured turn {turnNumber}, ID: {snapshot.Id}");
#endif
            return snapshot;
        }

        /// <summary>
        /// Restores the most recent snapshot.
        /// </summary>
        public bool RestoreLast(BattleContext context, IEnumerable<CharacterInstance> units)
        {
            var snapshot = Peek();
            return snapshot == null ? false : Restore(snapshot, context, units);
        }

        /// <summary>
        /// Restores a specific snapshot.
        /// </summary>
        public bool Restore(
            Snapshot snapshot,
            BattleContext context,
            IEnumerable<CharacterInstance> units
        )
        {
            if (snapshot == null)
            {
                return false;
            }

            // Build a lookup of current units by ID for safe restoration
            var unitLookup = new Dictionary<string, CharacterInstance>();
            if (units != null)
            {
                foreach (var u in units)
                {
                    if (u != null && !string.IsNullOrEmpty(u.Id))
                    {
                        unitLookup[u.Id] = u;
                    }
                }
            }

            // Restore only units that were captured in the snapshot and still exist in the current battle context.
            foreach (var unitId in snapshot.GetCapturedUnitIds())
            {
                if (unitLookup.TryGetValue(unitId, out var currentUnit))
                {
                    snapshot.RestoreUnit(currentUnit, context.mapGrid);
                }
                else
                {
                    // Unit was captured previously but is missing now (was removed); skip to avoid null refs.
                    Debug.LogWarning(
                        $"[Snapshot] Skipping restore for unit {unitId} - not present in current battle"
                    );
                }
            }

            OnSnapshotRestored?.Invoke(snapshot);
#if UNITY_EDITOR
            Debug.Log($"[Snapshot] Restored turn {snapshot.TurnNumber}, ID: {snapshot.Id}");
#endif
            return true;
        }

        private void Push(Snapshot snapshot)
        {
            _buffer[_head] = snapshot;
            _head = (_head + 1) % Capacity;
            _count = Math.Min(_count + 1, Capacity);
        }

        public Snapshot Peek()
        {
            if (_count == 0)
            {
                return null;
            }

            int index = (_head - 1 + Capacity) % Capacity;
            return _buffer[index];
        }

        public Snapshot Pop()
        {
            if (_count == 0)
            {
                return null;
            }

            _head = (_head - 1 + Capacity) % Capacity;
            _count--;
            return _buffer[_head];
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// Gets snapshot at index (0 = most recent).
        /// </summary>
        public Snapshot GetAt(int index)
        {
            if (index < 0 || index >= _count)
            {
                return null;
            }

            int bufferIndex = (_head - 1 - index + Capacity * 2) % Capacity;
            return _buffer[bufferIndex];
        }
    }
}
