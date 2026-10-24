using BrainType = Turnroot.Gameplay.Brain.Brain;
using Turnroot.Utilities;
using Turnroot.Gameplay.Brain;

namespace Turnroot.Gameplay.Combat.Precompute
{
    public partial class BattlePrecomputeLoader
    {
        #region Initialization
        public OperationResult Initialize(
            BrainType brain,
            FundamentalComponents.Battles.BattleContext context = null
        )
        {
            var validation = OperationResultGuards.RequireNotNull(brain, nameof(brain));
            if (!validation.Success)
            {
                return validation;
            }

            _brain = brain;
            _loadingController = brain.GetComponent<LoadingController>();
            _battleContext = context ?? _battleContext;
            _initialized = true;

            return OperationResult.Successful();
        }

        private void Start()
        {
            if (_initialized)
            {
                return;
            }

            var res = Initialize(BrainType.Instance);
            if (!res.Success)
            {
                $"BattlePrecomputeLoader: Auto-initialize failed: {res.ErrorMessage}".LogWarning();
            }
        }

        private void OnDestroy() => _brain = null;
        #endregion
    }
}
