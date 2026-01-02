using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Commands
{
    /// <summary>
    /// Base interface for all battle commands. Commands are undoable, replayable actions.
    /// </summary>
    public interface ICommand
    {
        string Id { get; }
        int TurnNumber { get; }
        bool Execute(BattleContext context);
        bool Undo(BattleContext context);
    }

    /// <summary>
    /// Manages command execution with undo/redo support.
    /// </summary>
    public class CommandHistory
    {
        private readonly List<ICommand> _history = new();
        private readonly Stack<ICommand> _redoStack = new();
        private int _position = -1;

        public int Count => _history.Count;
        public bool CanUndo => _position >= 0;
        public bool CanRedo => _redoStack.Count > 0;

        public bool Execute(ICommand command, BattleContext context)
        {
            if (!command.Execute(context))
            {
                return false;
            }

            // Clear redo stack when new command is executed
            _redoStack.Clear();

            // Truncate history if we're not at the end
            if (_position < _history.Count - 1)
            {
                _history.RemoveRange(_position + 1, _history.Count - _position - 1);
            }

            _history.Add(command);
            _position++;
            return true;
        }

        public bool Undo(BattleContext context)
        {
            if (!CanUndo)
            {
                return false;
            }

            var command = _history[_position];
            if (!command.Undo(context))
            {
                return false;
            }

            _redoStack.Push(command);
            _position--;
            return true;
        }

        public bool Redo(BattleContext context)
        {
            if (!CanRedo)
            {
                return false;
            }

            var command = _redoStack.Pop();
            if (!command.Execute(context))
            {
                return false;
            }

            _position++;
            return true;
        }

        public void Clear()
        {
            _history.Clear();
            _redoStack.Clear();
            _position = -1;
        }

        public IReadOnlyList<ICommand> GetHistory() => _history.AsReadOnly();
    }

    /// <summary>
    /// Base class for commands that stores undo state as a dictionary.
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public int TurnNumber { get; }
        protected Dictionary<string, object> UndoState { get; } = new();

        protected CommandBase(int turnNumber)
        {
            TurnNumber = turnNumber;
        }

        public abstract bool Execute(BattleContext context);
        public abstract bool Undo(BattleContext context);

        protected CharacterInstance FindUnit(BattleContext context, string unitId)
        {
            if (context?.Unit?.UnitInstance?.Id == unitId)
            {
                return context.Unit.UnitInstance;
            }

            foreach (var target in context?.Participants?.Targets ?? new List<CharacterInstance>())
            {
                if (target.Id == unitId)
                {
                    return target;
                }
            }

            foreach (var ally in context?.Participants?.Allies ?? new List<CharacterInstance>())
            {
                if (ally.Id == unitId)
                {
                    return ally;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Command to move a unit to a new position.
    /// </summary>
    public class MoveCommand : CommandBase
    {
        public string UnitId { get; }
        public Vector2Int Target { get; }

        public MoveCommand(string unitId, Vector2Int target, int turn)
            : base(turn)
        {
            UnitId = unitId;
            Target = target;
        }

        public override bool Execute(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null)
            {
                return false;
            }

            var oldPoint = unit.UnitPositionToMapGridPoint(unit.MapGridPosition, context.mapGrid);

            UndoState["from"] = unit.MapGridPosition;

            // Move the unit (updates internal position)
            var result = unit.MoveToPosition(Target, context.mapGrid);

            if (result.Success)
            {
                // Update grid occupancy
                var newPoint = unit.UnitPositionToMapGridPoint(Target, context.mapGrid);
                context.mapGrid.RemoveOccupied(oldPoint);
                context.mapGrid.SetOccupied(newPoint, unit);
                unit.MapGridPosition = Target;

                // Publish event
                context.Brain?.Publish(
                    new Events.UnitMovedEvent(unit, (Vector2Int)UndoState["from"], Target)
                );
            }

            return result.Success;
        }

        public override bool Undo(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null || !UndoState.TryGetValue("from", out var from))
            {
                return false;
            }

            var bb = context.Brain?.battleBrain;
            var moved = bb != null && bb.MoveUnit(unit, (Vector2Int)from, context.mapGrid);
            return moved;
        }
    }

    public class SpawnCommand : CommandBase
    {
        public string UnitId { get; }
        public Vector2Int SpawnPosition { get; }

        public SpawnCommand(string unitId, Vector2Int spawnPosition, int turn)
            : base(turn)
        {
            UnitId = unitId;
            SpawnPosition = spawnPosition;
        }

        public override bool Execute(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null)
            {
                return false;
            }

            UndoState["wasSpawned"] = true;

            var result = context.mapGrid.SetOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.mapGrid),
                unit
            );
#if UNITY_EDITOR
            Debug.Log(
                $"[SpawnCommand] Spawning Unit {UnitId} at {SpawnPosition}: Success={result.Success}"
            );
#endif
            if (result.Success)
            {
                // Mark unit as spawned during this battle so snapshot restore can identify reinforcements
                try
                {
                    unit.WasSpawnedDuringBattle = true;
                }
                catch (System.Exception) { }

                context.Brain?.Publish(new Events.UnitSpawnedEvent(unit, SpawnPosition));

                // Take a new snapshot to capture the spawn event immediately (helps testing and rollback)
                try
                {
                    context.Brain?.TakeSnapshot();
                }
                catch (System.Exception) { }
            }
            return result.Success;
        }

        public override bool Undo(BattleContext context)
        {
            var unit = FindUnit(context, UnitId);
            if (unit == null || !UndoState.TryGetValue("from", out var from))
            {
                return false;
            }

            var result = context.mapGrid.RemoveOccupied(
                unit.UnitPositionToMapGridPoint(SpawnPosition, context.mapGrid)
            );
            if (result.Success)
            {
                try
                {
                    unit.WasSpawnedDuringBattle = false;
                }
                catch (System.Exception) { }

                context.Brain?.Publish(new Events.UnitDespawnedEvent(unit, SpawnPosition));

                // Update snapshot to reflect removal
                try
                {
                    context.Brain?.TakeSnapshot();
                }
                catch (System.Exception) { }
            }
            return result.Success;
        }
    }

    /// <summary>
    /// Command to deal damage to a unit.
    /// </summary>
    public class DamageCommand : CommandBase
    {
        public string AttackerId { get; }
        public string TargetId { get; }
        public int Damage { get; }

        public DamageCommand(string attackerId, string targetId, int damage, int turn)
            : base(turn)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
        }

        public override bool Execute(BattleContext context)
        {
            var target = FindUnit(context, TargetId);
            if (target == null)
            {
                return false;
            }

            var health = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            if (health == null)
            {
                return false;
            }

            UndoState["prevHP"] = health.Current;
            UndoState["wasDefeated"] = target.IsDefeatedInCurrentBattle;

            health.SetCurrent(health.Current - Damage);

            var attacker = FindUnit(context, AttackerId);

            // Track last attacked target on the attacker for this battle
            if (attacker != null)
            {
                // Save previous value for undo
                UndoState["prevLastTarget"] = attacker.LastAttackedTarget;
                attacker.LastAttackedTarget = target;
            }

            // Track last attacker per target in the BattleContext
            if (context != null)
            {
                UndoState["prevLastAttackerOfTarget"] = context.GetLastAttacker(target);
                // Also save and set target's own LastAttacker field for convenience
                UndoState["prevTargetLastAttacker"] = target.LastAttacker;
                // Use BattleBrain wrapper to ensure events are published and context mapping updated
                context
                    .Brain?.GetComponent<BattleBrain>()
                    ?.SetLastAttacker(context, target, attacker);
            }

            context.Brain?.Publish(
                new Events.UnitDamagedEvent(target, attacker, Damage, (int)health.Current)
            );

            if (health.Current <= 0)
            {
                target.IsDefeatedInCurrentBattle = true;
                context.Brain?.Publish(new Events.UnitDefeatedEvent(target, attacker));
            }

            return true;
        }

        public override bool Undo(BattleContext context)
        {
            var target = FindUnit(context, TargetId);
            if (target == null)
            {
                return false;
            }

            var health = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            if (health == null || !UndoState.TryGetValue("prevHP", out var prev))
            {
                return false;
            }

            health.SetCurrent((float)prev);
            target.IsDefeatedInCurrentBattle = (bool)UndoState["wasDefeated"];

            // Restore previous LastAttackedTarget on the attacker if present
            var attacker = FindUnit(context, AttackerId);
            if (attacker != null && UndoState.TryGetValue("prevLastTarget", out var prevLast))
            {
                attacker.LastAttackedTarget = prevLast as CharacterInstance;
            }

            // Restore previous last-attacker mapping for the target and the target's own LastAttacker
            if (target != null)
            {
                if (UndoState.TryGetValue("prevLastAttackerOfTarget", out var prevLastAttacker))
                {
                    var bb = context.Brain?.GetComponent<BattleBrain>();
                    bb?.SetLastAttacker(context, target, prevLastAttacker as CharacterInstance);
                }
                else if (UndoState.TryGetValue("prevTargetLastAttacker", out var prevTargetLast))
                {
                    var bb = context.Brain?.GetComponent<BattleBrain>();
                    bb?.SetLastAttacker(context, target, prevTargetLast as CharacterInstance);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Command to use an item.
    /// </summary>
    public class UseItemCommand : CommandBase
    {
        public string UserId { get; }
        public string ItemId { get; }
        public string TargetId { get; }

        public UseItemCommand(string userId, string itemId, string targetId, int turn)
            : base(turn)
        {
            UserId = userId;
            ItemId = itemId;
            TargetId = targetId;
        }

        public override bool Execute(BattleContext context)
        {
            // Item use is typically not undoable in most games
            // This is a placeholder - implement based on your item system
#if UNITY_EDITOR
            Debug.Log($"[UseItemCommand] {UserId} used item {ItemId} on {TargetId ?? "self"}");
#endif
            return true;
        }

        public override bool Undo(BattleContext context)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[UseItemCommand] Item use cannot be undone");
#endif
            return false;
        }
    }

    public class HealCommand : CommandBase
    {
        public string HealerId { get; }
        public string TargetId { get; }

        public HealCommand(string healerId, string targetId, int turn)
            : base(turn)
        {
            HealerId = healerId;
            TargetId = targetId;
        }

        public override bool Undo(BattleContext context)
        {
            var target = FindUnit(context, TargetId);
            if (target == null)
            {
                return false;
            }

            var health = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            if (health == null || !UndoState.TryGetValue("prevHP", out var prev))
            {
                return false;
            }

            health.SetCurrent((float)prev);
            return true;
        }

        public override bool Execute(BattleContext context)
        {
            var target = FindUnit(context, TargetId);
            if (target == null)
            {
                return false;
            }

            var health = target.GetBoundedStat(Characters.Stats.BoundedStatType.Health);
            if (health == null)
            {
                return false;
            }

            UndoState["prevHP"] = health.Current;

            // TODO: Calculate heal amount based on stats, items, etc.
            int healAmount = 20;
            health.SetCurrent(Mathf.Min(health.Max, health.Current + healAmount));

            var healer = FindUnit(context, HealerId);

            context.Brain?.Publish(
                new Events.UnitHealedEvent(target, healer, healAmount, (int)health.Current)
            );

            return true;
        }
    }

    /// <summary>
    /// Command to activate a skill.
    /// </summary>
    public class SkillCommand : CommandBase
    {
        public string CasterId { get; }
        public string SkillId { get; }
        public string[] TargetIds { get; }

        public SkillCommand(string casterId, string skillId, string[] targetIds, int turn)
            : base(turn)
        {
            CasterId = casterId;
            SkillId = skillId;
            TargetIds = targetIds ?? Array.Empty<string>();
        }

        public override bool Execute(BattleContext context) => true;

        public override bool Undo(BattleContext context)
        {
            // Skill effects are undone through their individual commands (damage, buffs, etc.)
#if UNITY_EDITOR
            Debug.LogWarning("[SkillCommand] Skill activation record cannot be undone");
#endif
            return false;
        }
    }

    /// <summary>
    /// Command to end a turn.
    /// </summary>
    public class EndTurnCommand : CommandBase
    {
        public EndTurnCommand(int turn)
            : base(turn) { }

        public override bool Execute(BattleContext context)
        {
            // Notify that a unit's turn ended (unit is still the active one on the context)
            context.Brain?.PublishUnitTurnEnded(context.Unit.UnitInstance);

            // Also publish the round-level event
            context.Brain?.Publish(new Events.TurnEndedEvent(TurnNumber));
            return true;
        }

        public override bool Undo(BattleContext context)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[EndTurnCommand] Turn end cannot be undone");
#endif
            return false;
        }
    }
}
