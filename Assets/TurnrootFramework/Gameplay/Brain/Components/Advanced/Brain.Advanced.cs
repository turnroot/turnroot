using System;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Brain.Snapshots;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Partial class extension for Brain that adds the advanced architectural systems:
    /// - Priority Event Bus: Subscribe with priorities for ordered event handling
    /// - Command History: Undoable battle actions with undo/redo
    /// - Snapshot Stack: Save/restore battle state for preview and replay
    /// </summary>
    public partial class Brain
    {
        #region Priority Event Bus

        private PriorityEventBus _eventBus;

        /// <summary>The priority-based event bus for ordered event handling.</summary>
        public PriorityEventBus EventBus => _eventBus ??= new PriorityEventBus();

        /// <summary>Subscribe to an event type with priority.</summary>
        public void Subscribe<T>(
            Action<T> handler,
            EventPriority priority = EventPriority.Normal
        ) => EventBus.Subscribe(handler, priority);

        /// <summary>Unsubscribe from an event type.</summary>
        public bool Unsubscribe<T>(Action<T> handler) => EventBus.Unsubscribe(handler);

        /// <summary>Publish an event immediately through the priority event bus.</summary>
        public void Publish<T>(T eventData) => EventBus.PublishImmediate(eventData);

        /// <summary>Queue an event for deferred processing at frame end.</summary>
        public void PublishDeferred<T>(T eventData) => EventBus.PublishDeferred(eventData);

        /// <summary>Process all deferred events.</summary>
        public void ProcessDeferredEvents() => EventBus.ProcessDeferredEvents();

        #endregion

        #region Command History

        private CommandHistory _commandHistory;

        /// <summary>The command history for managing undoable battle actions.</summary>
        public CommandHistory Commands => _commandHistory ??= new CommandHistory();

        /// <summary>Execute a command and add it to history.</summary>
        public bool ExecuteCommand(ICommand command)
        {
            var context = battleBrain?.BattleObject?.Context;
            return context == null
                ? OperationResult.Failure("No active battle context.").Success
                : Commands.Execute(command, context);
        }

        /// <summary>Undo the last executed command.</summary>
        public bool UndoCommand()
        {
            var context = battleBrain?.BattleObject?.Context;
            return context == null ? false : Commands.Undo(context);
        }

        /// <summary>Redo the last undone command.</summary>
        public bool RedoCommand()
        {
            var context = battleBrain?.BattleObject?.Context;
            return context == null ? false : Commands.Redo(context);
        }

        #endregion

        #region Snapshot Stack

        private SnapshotStack _snapshots;

        /// <summary>The snapshot stack for state capture and restoration.</summary>
        public SnapshotStack Snapshots => _snapshots ??= new SnapshotStack();

        /// <summary>Take a snapshot of the current battle state.</summary>
        public Snapshot TakeSnapshot()
        {
            var context = battleBrain?.BattleObject?.Context;
            if (context == null)
            {
                _ = OperationResult.Failure("No active battle context.");
                return null;
            }
            var turn = battleBrain?.CurrentTurnNumber ?? 0;
            return Snapshots.Take(context, GetAllBattleCharacters(), turn);
        }

        /// <summary>Restore the most recent snapshot.</summary>
        public bool RestoreSnapshot()
        {
            var context = battleBrain?.BattleObject?.Context;
            return context != null && Snapshots.RestoreLast(context, GetAllBattleCharacters());
        }

        private IEnumerable<CharacterInstance> GetAllBattleCharacters()
        {
            var characters = new List<CharacterInstance>();
            if (battleBrain?.PlayerTeamRoster?.Instances != null)
            {
                characters.AddRange(battleBrain.PlayerTeamRoster.Instances);
            }

            if (battleBrain?.EnemyTeamRoster?.Instances != null)
            {
                characters.AddRange(battleBrain.EnemyTeamRoster.Instances);
            }

            if (battleBrain?.ThirdPartyTeamRoster?.Instances != null)
            {
                characters.AddRange(battleBrain.ThirdPartyTeamRoster.Instances);
            }

            return characters;
        }

        #endregion

        #region Advanced System Lifecycle

        private void InitializeAdvancedSystems()
        {
            _eventBus = new PriorityEventBus();
            _commandHistory = new CommandHistory();
            _snapshots = new SnapshotStack();

            OnTurnEnded += HandleTurnEnd;
        }

        private void HandleTurnEnd() => ProcessDeferredEvents();

        private void CleanupAdvancedSystems()
        {
            OnTurnEnded -= HandleTurnEnd;
            _eventBus?.ClearAllSubscriptions();
            _commandHistory?.Clear();
            _snapshots?.Clear();
        }

        #endregion
    }
}
