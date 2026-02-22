using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    /// <summary>
    /// Resolves which BattlePreparationObject should be used for UI / preview components and notifies
    /// interested components when a preparation object is chosen.
    /// Any instance of:
    ///     - PopulateBattleMapName
    ///     - PopulateBattleMapPreview
    ///     - PopulateMapPrefabEnvironmentCondtions
    ///     - StartingPositions
    ///     - BattlePreTurn
    /// Needs to have one of these
    /// </summary>
    public class PreparationObjectResolver : MonoBehaviour
    {
        public static BattlePreparationObject ResolvedPreparationObject { get; private set; }
        public delegate void PreparationResolvedHandler(BattlePreparationObject prep);
        public static event PreparationResolvedHandler OnPreparationResolved;

        public void InitializeFromInspectorEvent()
        {
            var brain = FindFirstObjectByType<Brain>();
            Initialize(brain);
        }

        public OperationResult Initialize(Brain brain)
        {
            var prep = ResolveForBrain(brain);
            var count = 0;
            SetResolved(prep);

            if (prep != null)
            {
                // Ensure the preparation object is initialized with the Brain so MapGrid and related data exist.
                if (prep.Brain == null)
                {
                    prep.Initialize(brain);
                }

                if (TryGetComponent(out PopulateBattleMapPreview preview) && preview != null)
                {
                    preview.Initialize(prep);
                    count++;
                }

                if (TryGetComponent(out PopulateBattleMapName name) && name != null)
                {
                    name.Initialize(prep);
                    count++;
                }

                StartingPositions p = null;
                if (prep != null)
                {
                    p = prep.GetComponentInChildren<StartingPositions>(true);
                }
                if (p == null)
                {
                    TryGetComponent(out p);
                }

                if (TryGetComponent(out BattlePreTurn preTurn) && preTurn != null)
                {
                    preTurn.Initialize(brain.battleBrain);
                    count++;
                }

                if (p != null)
                {
                    ResolvedPreparationObject.StartingPositionsComponent = p;

                    // Ensure any other StartingPositions instances are replaced to avoid duplicates
                    var others = FindObjectsByType<StartingPositions>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );
                    foreach (var other in others)
                    {
                        if (other == null || other == p)
                        {
                            continue;
                        }

                        other.ReplaceBy(p);
                    }

                    var result = p.Initialize(prep);
                    if (!result.Success)
                    {
#if UNITY_EDITOR
                        $"StartingPositions initialization failed: {result.ErrorMessage}".LogError();
#endif
                    }
                    else
                    {
                        count++;
                    }
                }
            }

            var envPopulators = FindObjectsByType<PopulateMapPrefabEnviromentConditions>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var env in envPopulators)
            {
                if (env != null)
                {
                    env.Initialize(prep);
                    count++;
                }
            }
            return count <= 0
                ? OperationResult.Failure("PreparationObjectResolver: Nothing to initialize")
                : OperationResult.Successful();
        }

        public BattlePreparationObject ResolveForBrain(Brain brain)
        {
            var prep = brain.battleBrain.PreparationObject;
            if (prep != null)
            {
                return prep;
            }

            var scenePrep = FindFirstObjectByType<BattlePreparationObject>();
            return scenePrep;
        }

        private void SetResolved(BattlePreparationObject prep)
        {
            ResolvedPreparationObject = prep;
            OnPreparationResolved?.Invoke(prep);
        }
    }
}

