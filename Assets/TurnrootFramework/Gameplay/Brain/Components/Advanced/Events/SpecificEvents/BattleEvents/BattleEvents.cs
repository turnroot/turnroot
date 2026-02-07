using UnityEngine;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Base class for all battle events published through the PriorityEventBus.
    /// Specific event types are located in the SpecificEvents subfolder.
    /// </summary>
    public abstract class BattleEvent
    {
        public int TurnNumber { get; set; }
        public float Timestamp { get; set; }

        protected BattleEvent()
        {
            Timestamp = Time.time;
        }
    }
}
