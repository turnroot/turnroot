using Turnroot.Conversations;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Segments;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// The universal brain for managing and propagating events and data throughout the brain system.
    /// All brain events come from here.
    /// It's like a farfalle, lots of stuff in, one central point, lots of stuff out.
    /// </summary>
    /// <remarks>
    /// If you want to extend brain functionality, create new scripts that interact with the brain components.
    /// Hook into the brain, don't alter it.
    /// If you're looking for hooks, there are lots of events you can subscribe to,
    /// search for `public event Action`.
    ///
    /// Advanced Systems (see Brain.Advanced.cs):
    /// - Priority Event System: Subscribe with priorities for ordered event handling
    /// - Command Pattern: Undoable, serializable battle actions
    /// - State Snapshot System: Save/restore battle state for preview and replay
    ///
    /// Note: if you are going to make changes here, make sure you fork the Turnroot Framework repository
    /// on GitHub and make your changes there. If you go in willy-nily without git history,
    /// I can't help you if you mess up your game!
    /// </remarks>
    [RequireComponent(typeof(StateBrain))]
    [RequireComponent(typeof(ConversationalBrain))]
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(GamewideContextBrain))]
    [RequireComponent(typeof(CharactersBrain))]
    [RequireComponent(typeof(BattleBrain))]
    [RequireComponent(typeof(InventoryBrain))]
    [RequireComponent(typeof(StorehouseBrain))]
    [RequireComponent(typeof(BattleInputControllerBrain))]
    [RequireComponent(typeof(PositioningInputController))]
    [RequireComponent(typeof(UiBrain))]
    [RequireComponent(typeof(VolumeBrain))]
    [RequireComponent(typeof(AudioBrain))]
    [RequireComponent(typeof(CameraBrain))]
    [RequireComponent(typeof(CursorBrain))]
    [RequireComponent(typeof(UnitAppearanceBrain))]
    [RequireComponent(typeof(SaveFileBrain))]
    [RequireComponent(typeof(LoadingController))]
    public partial class Brain : MonoBehaviour
    {
        // Core components
        [HideInInspector]
        public StateBrain stateBrain;

        [HideInInspector]
        public ConversationalBrain conversationalBrain;

        [HideInInspector]
        public GamewideContextBrain gamewideContextBrain;

        [HideInInspector]
        public CharactersBrain charactersBrain;

        [HideInInspector]
        public BattleBrain battleBrain;

        [HideInInspector]
        public InventoryBrain inventoryBrain;

        [HideInInspector]
        public StorehouseBrain storehouseBrain;

        [HideInInspector]
        public BattleInputControllerBrain battleInputControllerBrain;

        [HideInInspector]
        public UiBrain uiBrain;

        [HideInInspector]
        public VolumeBrain volumeBrain;

        [HideInInspector]
        public AudioBrain audioBrain;

        [HideInInspector]
        public CameraBrain cameraBrain;

        [HideInInspector]
        public CursorBrain cursorBrain;

        [HideInInspector]
        public PositioningInputController positioningInputControllerBrain;

        [HideInInspector]
        public UnitAppearanceBrain unitAppearanceBrain;

        [HideInInspector]
        public LongTermMemory ltm;

        private ConversationController _sceneConversationController;

        public static bool HubModuleEnabled =>
#if TURNROOT_HUB_MODULE
            true;
#else
            false;
#endif
        public static bool BloodlinesModuleEnabled =>
#if TURNROOT_BLOODLINES_MODULE
            true;
#else
            false;
#endif
        public static bool RetroModuleEnabled =>
#if TURNROOT_RETRO_MODULE
            true;
#else
            false;
#endif
        public static bool UnwindModuleEnabled =>
#if TURNROOT_UNWIND_MODULE
            true;
#else
            false;
#endif
        public static bool TroopsModuleEnabled =>
#if TURNROOT_TROOPS_MODULE
            true;
#else
            false;
#endif
        public static bool MonstersModuleEnabled =>
#if TURNROOT_MONSTERS_MODULE
            true;
#else
            false;
#endif
    }
}
