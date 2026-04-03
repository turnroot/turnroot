using System;
using Turnroot.Characters;
using Turnroot.Characters.Components.Support;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Support Relationship Events

        public event Action<
            CharacterInstance,
            SupportRelationshipInstance
        > OnSupportRelationshipAdded;
        public event Action<CharacterInstance, CharacterData> OnSupportRelationshipRemoved;
        public event Action<CharacterInstance, SupportRelationshipInstance> OnSupportLevelIncreased;

        public void PublishSupportRelationshipAdded(
            CharacterInstance source,
            SupportRelationshipInstance relationship
        ) => OnSupportRelationshipAdded.Invoke(source, relationship);

        public void PublishSupportRelationshipRemoved(
            CharacterInstance source,
            CharacterData target
        ) => OnSupportRelationshipRemoved.Invoke(source, target);

        public void PublishSupportLevelIncreased(
            CharacterInstance source,
            SupportRelationshipInstance relationship
        ) => OnSupportLevelIncreased?.Invoke(source, relationship);

        #endregion
    }
}
