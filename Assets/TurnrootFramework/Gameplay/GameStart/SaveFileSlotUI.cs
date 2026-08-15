using Turnroot.Gameplay.Brain;
using Turnroot.UI;

namespace Turnroot.Gameplay.GameStart
{
    /// <summary>
    /// Abstract base for any UI component that represents a single save-file slot on the
    /// title / game-start screen. Inherits visual selection behaviour from
    /// <see cref="UiChoice"/> (scale pop, UIEffect highlight), so all save-file slot
    /// GameObjects automatically satisfy the <c>T : UiChoice</c> constraint on
    /// <see cref="UiInputProvider.Navigate{T}"/>.
    /// </summary>
    public abstract class SaveFileSlotUI : UiChoice
    {
        /// <summary>
        /// Populate the slot's visible content from the given save-file data.
        /// Called by <see cref="GameStartManagerBase"/> whenever save files are refreshed.
        /// </summary>
        public abstract void UpdateDisplay(SaveFile saveFile);
    }
}
