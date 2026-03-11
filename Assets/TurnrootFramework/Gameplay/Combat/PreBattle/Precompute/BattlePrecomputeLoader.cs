using Turnroot.Gameplay.Brain;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.Precompute
{
    /// <summary>
    /// Precomputes expensive battle startup data during the PreBattleTransitionToBattle
    /// brain state and reports progress to the scene's LoadingController.
    /// </summary>
    public partial class BattlePrecomputeLoader : MonoBehaviour
    {
        #region Fields
        // core fields
        private Brain.Brain _brain;
        private LoadingController _loadingController;
        private FundamentalComponents.Battles.BattleContext _battleContext;
        private bool _initialized = false;
        private bool _precomputeStarted = false;

        [SerializeField]
        private float timeBetweenOperations = 0.1f;
        #endregion

        // functionality moved into partial class files for clarity
    }
}
