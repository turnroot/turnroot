using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.Precompute
{
    public partial class BattlePrecomputeLoader
    {
        #region Initialization
        public OperationResult Initialize(
            Brain.Brain brain,
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

            var brain = FindFirstObjectByType<Brain.Brain>();
            if (brain != null)
            {
                var res = Initialize(brain);
                if (!res.Success)
                {
                    $"BattlePrecomputeLoader: Auto-initialize failed: {res.ErrorMessage}".LogWarning();
                }
            }
        }

        private void OnDestroy() => _brain = null;
        #endregion
    }
}
