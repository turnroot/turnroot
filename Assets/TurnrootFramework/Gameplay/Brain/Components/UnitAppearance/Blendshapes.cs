using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain : BrainComponent
    {
        public OperationResult SetBlendshapes(CharacterInstance unit)
        {
            var weights = unit.CharacterTemplate.Blendshapes;
            var names = weights.BlendshapeNames ?? new string[0];
            var renderers = GetBlendshapeRenderers(unit, unit.GetCurrentClass()).ToArray();

            if (renderers.Length == 0)
            {
                TurnrootLogger.Log(
                    $"SetBlendshapes: No outfit renderers found for {unit.CharacterTemplate?.DisplayName}. Blendshapes are applied only to outfit meshes (NonBattleOutfitPrefab or class outfit).",
                    TurnrootLogger.LogLevel.Error
                );
                return OperationResult.Failure("SetBlendshapes: no outfit renderers found");
            }

            foreach (var shapeName in names)
            {
                var weight = weights.GetBlendshapeByName(shapeName);
                if (!ApplyBlendshapeToRenderers(renderers, shapeName, weight))
                {
                    TurnrootLogger.Log(
                        $"SetBlendshapes: Could not set blendshape '{shapeName}' for {unit.CharacterTemplate?.DisplayName}. Shape not found on any outfit renderer.",
                        TurnrootLogger.LogLevel.Error
                    );
                    return OperationResult.Failure(
                        $"Could not set blendshape weight for {shapeName}: shape not found on any renderer"
                    );
                }
            }

            return OperationResult.Successful();
        }

        private bool ApplyBlendshapeToRenderers(
            SkinnedMeshRenderer[] renderers,
            string name,
            float weight
        )
        {
            bool applied = false;
            foreach (var r in renderers)
            {
                if (r?.sharedMesh == null)
                {
                    continue;
                }

                int index = r.sharedMesh.GetBlendShapeIndex(name);
                if (index >= 0)
                {
                    r.SetBlendShapeWeight(index, weight);
                    applied = true;
                }
            }
            return applied;
        }
    }
}
