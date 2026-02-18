using System;
using Turnroot.Characters;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Status Effect Events

        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectApplied;
        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectRemoved;
        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectStacked;
        public event Action<
            CharacterInstance,
            Characters.StatusEffects.StatusEffectInstance
        > OnStatusEffectExpired;
        public event Action<CharacterInstance> OnAllStatusEffectsCleared;

        public void PublishStatusEffectApplied(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectApplied?.Invoke(character, effect);

        public void PublishStatusEffectRemoved(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectRemoved?.Invoke(character, effect);

        public void PublishStatusEffectStacked(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectStacked?.Invoke(character, effect);

        public void PublishStatusEffectExpired(
            CharacterInstance character,
            Characters.StatusEffects.StatusEffectInstance effect
        ) => OnStatusEffectExpired?.Invoke(character, effect);

        public void PublishAllStatusEffectsCleared(CharacterInstance character) =>
            OnAllStatusEffectsCleared?.Invoke(character);

        #endregion
    }
}
