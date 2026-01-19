using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

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
}
