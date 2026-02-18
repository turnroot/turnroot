using System;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Character Model Events

        public event Action<CharacterInstance> OnUnitModelSpawnRequested;
        public event Action<CharacterInstance> OnUnitModelChangeRequested;

        public void PublishUnitModelSpawnRequested(CharacterInstance unit) =>
            OnUnitModelSpawnRequested.Invoke(unit);

        public void PublishUnitModelChangeRequested(CharacterInstance unit) =>
            OnUnitModelChangeRequested.Invoke(unit);

        #endregion
    }
}
