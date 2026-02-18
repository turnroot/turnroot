using System;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Placement Events

        public event Action OnPositioningModeEntered;
        public event Action OnPositioningModeExited;
        public event Action OnPlacementsInitialized;

        // Published when some component requests the preparation placements to be synced into the runtime roster.
        // Args: (persist, forceApplyPlacementsOnLoad)
        public event Action<bool, bool> OnPlacementsSyncRequested;
        public event Action<CharacterInstance, bool> OnUnitSelectionChanged;

        public void PublishPositioningModeEntered() => OnPositioningModeEntered.Invoke();

        public void PublishPositioningModeExited() => OnPositioningModeExited.Invoke();

        public void PublishPlacementsInitialized() => OnPlacementsInitialized.Invoke();

        public void PublishPlacementsSyncRequested(bool persist, bool forceApplyPlacementsOnLoad) =>
            OnPlacementsSyncRequested?.Invoke(persist, forceApplyPlacementsOnLoad);

        public void PublishUnitSelectionChanged(CharacterInstance unit, bool selected) =>
            OnUnitSelectionChanged.Invoke(unit, selected);

        #endregion
    }
}
