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
        /// <summary>
        /// Returns the renderers that should receive blendshape updates.
        /// This intentionally includes only outfit renderers (class or non-battle outfit)
        /// and excludes head/hands and hair so those parts remain unaffected by blendshapes.
        /// </summary>
        private IEnumerable<SkinnedMeshRenderer> GetBlendshapeRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInst
        )
        {
            // Reuse the outfit renderer collection so blendshapes only apply to outfits.
            return GetOutfitRenderers(unit, classInst);
        }

        /// <summary>
        /// Returns the renderers that should receive outfit materials (accent/skin colors).
        /// This intentionally excludes head/hands and hair models so they remain un-tinted.
        /// </summary>
        private IEnumerable<SkinnedMeshRenderer> GetOutfitRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInst
        )
        {
            var list = new List<SkinnedMeshRenderer>();

            if (unit?.Renderer != null)
            {
                // Prefer collecting all skinned renderers under the model root so outfits
                // composed of multiple SMRs are fully included. Exclude head/hands and hair.
                var primary = unit.Renderer;
                var root = primary.gameObject.transform.parent;
                if (root != null)
                {
                    foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        var rn = r.gameObject.name ?? string.Empty;
                        if (!rn.StartsWith("HeadHands") && !rn.Equals("Hair"))
                        {
                            list.Add(r);
                        }
                    }
                }
                else
                {
                    // Fallback to primary's children if root is missing
                    foreach (
                        var r in primary.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(
                            true
                        )
                    )
                    {
                        var rn = r.gameObject.name ?? string.Empty;
                        if (!rn.StartsWith("HeadHands") && !rn.Equals("Hair"))
                        {
                            list.Add(r);
                        }
                    }
                }
            }

            if (classInst?.MeshRenderer != null && !list.Contains(classInst.MeshRenderer))
            {
                list.Add(classInst.MeshRenderer);
            }

            return list.Distinct();
        }

        private SkinnedMeshRenderer CreateHeadMesh(CharacterInstance unit, GameObject parent)
        {
            if (unit.CharacterTemplate.HeadAndHandsPrefab == null)
            {
                return null;
            }

            var instance = Object.Instantiate(
                unit.CharacterTemplate.HeadAndHandsPrefab,
                parent.transform
            );
            instance.name = "HeadHands";
            TurnrootLogger.Log(
                $"CreateHeadMesh: Instantiated head/hands prefab for {unit.CharacterTemplate?.DisplayName}"
            );
            return instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
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
                return OperationResult.Successful();
            }

            CleanupOldModel(pos, models);
            return CreateNewModel(unit, worldPos, pos, models);
        }
    }
}
