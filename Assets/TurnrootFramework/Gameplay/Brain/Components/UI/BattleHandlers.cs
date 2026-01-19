using Turnroot.Gameplay.Brain;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        public void HandleBattleUi()
        {
#if UNITY_EDITOR
            Debug.Log("UiBrain: Handling battle UI setup");
#endif
            // Battle UI initialization logic will be added here
            // For now, just log that we're in battle state
        }
    }
}
