using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.UI.Components;
using Turnroot.Utilities;
using UnityEngine;

/// <summary>
/// Resolves which BattlePreparationObject should be used for UI / preview components and notifies
/// interested components when a preparation object is chosen.
/// Any instance of:
///     - PopulateBattleMapName
///     - PopulateBattleMapPreview
///     - PopulateMapPrefabEnvironmentCondtions
///     - StartingPositions
/// Needs to have one of these
/// </summary>
public class PreparationObjectResolver : MonoBehaviour
{
    public static BattlePreparationObject ResolvedPreparationObject { get; private set; }
    public delegate void PreparationResolvedHandler(BattlePreparationObject prep);
    public static event PreparationResolvedHandler OnPreparationResolved;

    public OperationResult Initialize(Brain brain)
    {
        var prep = ResolveForBrain(brain);
        var count = 0;
        SetResolved(prep);

        if (prep != null)
        {
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
            if (TryGetComponent(out StartingPositions p) && p != null)
            {
                p.Initialize(prep);
                count++;
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
            : OperationResult.SuccessResult();
    }

    public BattlePreparationObject ResolveForBrain(Brain brain)
    {
        var prep = brain?.battleBrain?.PreparationObject;
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
