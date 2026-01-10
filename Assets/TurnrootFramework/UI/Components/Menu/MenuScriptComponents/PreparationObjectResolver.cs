using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using Turnroot.Gameplay.Combat.PreBattle;
using UnityEngine;

/// <summary>
/// Resolves which BattlePreparationObject should be used for UI / preview components and notifies
/// interested components when a preparation object is chosen.
/// </summary>
public class PreparationObjectResolver : MonoBehaviour
{
    [Tooltip("Optional explicit PreparationObject to prefer when resolving.")]
    public BattlePreparationObject PreparationObject;
    public static BattlePreparationObject ResolvedPreparationObject { get; private set; }

    public delegate void PreparationResolvedHandler(BattlePreparationObject prep);
    public static event PreparationResolvedHandler OnPreparationResolved;

    public void Initialize(Brain brain)
    {
        var prep = ResolveForBrain(brain);
        SetResolved(prep);

        if (prep != null)
        {
            if (TryGetComponent(out PopulateBattleMapPreview preview) && preview != null)
            {
                preview.Initialize(prep);
            }

            if (TryGetComponent(out PopulateBattleMapName name) && name != null)
            {
                name.Initialize(prep);
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
            }
        }
    }

    public BattlePreparationObject ResolveForBrain(Brain brain)
    {
        var prep = brain?.battleBrain?.PreparationObject;
        if (prep != null)
        {
            return prep;
        }

        if (PreparationObject != null)
        {
            return PreparationObject;
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
