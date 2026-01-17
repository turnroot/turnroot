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
            var renderers = GetRelevantRenderers(unit, unit.GetCurrentClass()).ToArray();

            if (renderers.Length == 0)
            {
                return OperationResult.Failure("SetBlendshapes: unit has no SkinnedMeshRenderer");
            }

            foreach (var shapeName in names)
            {
                var weight = weights.GetBlendshapeByName(shapeName);
                if (!ApplyBlendshapeToRenderers(renderers, shapeName, weight))
                {
                    return OperationResult.Failure(
                        $"Could not set blendshape weight for {shapeName}: shape not found on any renderer"
                    );
                }
            }

            return OperationResult.SuccessResult();
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
