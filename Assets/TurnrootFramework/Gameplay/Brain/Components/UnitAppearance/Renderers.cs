using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain : BrainComponent
    {
        private IEnumerable<SkinnedMeshRenderer> GetRelevantRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInst
        )
        {
            var list = new List<SkinnedMeshRenderer>();

            if (unit?.Renderer != null)
            {
                list.AddRange(
                    unit.Renderer.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                );
            }

            if (classInst?.MeshRenderer != null && !list.Contains(classInst.MeshRenderer))
            {
                list.Add(classInst.MeshRenderer);
            }

            return list.Distinct();
        }

        private SkinnedMeshRenderer CreateHeadMesh(CharacterInstance unit, GameObject parent)
        {
            if (unit.CharacterTemplate.CharacterHeadHandsAndHair == null)
            {
                return null;
            }

            var obj = new GameObject("HeadHandsHair");
            obj.transform.SetParent(parent.transform);
            return CopyRenderer(obj, unit.CharacterTemplate.CharacterHeadHandsAndHair);
        }

        private SkinnedMeshRenderer CopyRenderer(GameObject target, SkinnedMeshRenderer source)
        {
            var renderer = target.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = source.sharedMesh;
            renderer.rootBone = source.rootBone;
            renderer.bones = source.bones;
            return renderer;
        }

        private void SetPrimaryRenderer(
            CharacterInstance unit,
            SkinnedMeshRenderer outfit,
            SkinnedMeshRenderer head,
            GameObject root
        )
        {
            if (outfit != null)
            {
                unit.SetRenderer(outfit);
            }
            else if (head != null)
            {
                unit.SetRenderer(head);
            }
            else
            {
                unit.SetRenderer(CreatePlaceholderRenderer(unit, root));
            }
        }

        private SkinnedMeshRenderer CreatePlaceholderRenderer(
            CharacterInstance unit,
            GameObject parent
        )
        {
            TurnrootLogger.Log(
                $"No renderers for {unit.CharacterTemplate.DisplayName}, creating placeholder"
            );
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.transform.SetParent(parent.transform);
            placeholder.GetComponent<Renderer>().material.color =
                unit.CharacterTemplate.AccentColor1;
            return placeholder.AddComponent<SkinnedMeshRenderer>();
        }

        public OperationResult SpawnUnitModelOnGrid(
            Vector2Int pos,
            CharacterInstance unit,
            Dictionary<Vector2Int, GameObject> models,
            bool prebattle = false
        )
        {
            if (unit == null)
            {
                return OperationResult.Failure("Unit is null");
            }

            var worldPos = GetWorldPosition(pos, prebattle);
            var existing = TryReuseExistingModel(unit, worldPos, pos, models);

            if (existing != null)
            {
                return OperationResult.SuccessResult();
            }

            CleanupOldModel(pos, models);
            return CreateNewModel(unit, worldPos, pos, models);
        }
    }
}
