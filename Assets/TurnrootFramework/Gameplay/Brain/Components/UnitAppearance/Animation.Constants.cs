using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        private const float ANIMATION_BLEND_DURATION = 0.2f;

        // animation state names used when overriding clips
        private const string WalkState = "Walk";
        private const string IdleState = "Idle";

        // cached hash for the idle state (used when playing directly)
        private static readonly int IdleHash = Animator.StringToHash(IdleState);
    }
}