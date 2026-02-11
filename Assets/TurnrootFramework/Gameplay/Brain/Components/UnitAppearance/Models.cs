using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles unit model creation, outfit rendering, and mesh hierarchy management.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        public GameObject GetModelForUnit(string unitId) =>
            _unitModels.TryGetValue(unitId, out var model) ? model : null;

        public GameObject CreateModelForUnit(CharacterInstance unit, GameObject root = null)
        {
            if (root == null)
            {
                root = new GameObject($"{unit.CharacterTemplate.DisplayName}_Root");
            }

            var outfitRenderer = CreateOutfitMesh(unit, root);

            // Some outfits include their own head/hands or hair
            var hasHead = root.transform.Find("HeadAndHands") != null;
            var hasHair = root.transform.Find("Hair") != null;

            var headRenderer = hasHead ? outfitRenderer : CreateHeadMesh(unit, root);
            var hairRenderer = hasHair ? null : CreateHairMesh(unit, root);

            SetPrimaryRenderer(unit, outfitRenderer, headRenderer, root);

            UnifyBoneHierarchies(root);

            var animator = root.AddComponent<Animator>();
            if (_settings.DefaultUnitAnimatorController != null)
            {
                animator.runtimeAnimatorController = _settings.DefaultUnitAnimatorController;
            }

            // Apply custom avatar if character uses a different skeleton
            if (unit.CharacterTemplate.CustomAvatar != null)
            {
                animator.avatar = unit.CharacterTemplate.CustomAvatar;
            }

            // Setup extra bone layers if character has additional bones
            SetupAnimatorLayers(animator, unit);

            return root;
        }

        private SkinnedMeshRenderer CreateOutfitMesh(CharacterInstance unit, GameObject parent)
        {
            // Prefer class outfit when using battle model
            if (unit.UseBattleModel)
            {
                if (TryGetClassOutfit(unit, parent, out var classSmr))
                {
                    return classSmr;
                }
            }

            // Fall back to per-character non-battle outfit
            var nbSmr = TryCreateNonBattleOutfit(unit, parent);
            if (nbSmr != null)
            {
                return nbSmr;
            }

            TurnrootLogger.Log(
                $"No suitable outfit found for {unit.CharacterTemplate.DisplayName}. "
                    + "Ensure class model or NonBattleOutfitPrefab is assigned.",
                TurnrootLogger.LogLevel.Warning
            );
            return null;
        }

        private bool TryGetClassOutfit(
            CharacterInstance unit,
            GameObject parent,
            out SkinnedMeshRenderer smr
        )
        {
            smr = null;
            var classInst = unit.GetCurrentClass();
            if (classInst?.ClassData == null || !classInst.ClassData.HasOutfit)
            {
                return false;
            }

            var prefab = classInst.ClassData.Identity.ClassModelPrefab;
            if (prefab == null)
            {
                return false;
            }

            var obj = Instantiate(prefab, parent.transform);
            obj.name = "ClassOutfit";
            smr = obj.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (smr != null)
            {
                return true;
            }

            TurnrootLogger.Log(
                $"Class outfit prefab '{prefab.name}' is missing a SkinnedMeshRenderer. "
                    + $"Falling back to NonBattleOutfitPrefab for {unit.CharacterTemplate.DisplayName}",
                TurnrootLogger.LogLevel.Warning
            );

            Destroy(obj);
            return false;
        }

        private SkinnedMeshRenderer TryCreateNonBattleOutfit(
            CharacterInstance unit,
            GameObject parent
        )
        {
            var nbPrefab = unit.CharacterTemplate.NonBattleOutfitPrefab;
            if (nbPrefab == null)
            {
                return null;
            }

            var nbInstance = Instantiate(nbPrefab, parent.transform);
            nbInstance.name = "NonBattleOutfit";
            var nbSmr = nbInstance.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (nbSmr == null)
            {
                TurnrootLogger.Log(
                    $"Non-battle outfit prefab '{nbPrefab.name}' does not contain a SkinnedMeshRenderer. "
                        + $"Cannot create outfit for {unit.CharacterTemplate.DisplayName}",
                    TurnrootLogger.LogLevel.Error
                );
                Destroy(nbInstance);
                return null;
            }

            // Preserve the prefab's original materials
            var prefabSmr = nbPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (prefabSmr != null)
            {
                nbSmr.sharedMaterials = prefabSmr.sharedMaterials;
            }

            AttachHeadAndHair(unit, parent);
            return nbSmr;
        }

        private void AttachHeadAndHair(CharacterInstance unit, GameObject parent)
        {
            if (unit.CharacterTemplate.HeadAndHandsPrefab != null)
            {
                var hh = Instantiate(unit.CharacterTemplate.HeadAndHandsPrefab, parent.transform);
                hh.name = "HeadAndHands";
            }

            // Check if class has a hat outfit
            var classInst = unit.GetCurrentClass();
            var classHatPrefab = classInst?.ClassData.Identity.ClassHatPrefab;

            if (classHatPrefab != null)
            {
                // Use class hat with height offset
                var hat = Instantiate(classHatPrefab, parent.transform);
                hat.name = "ClassHat";
                hat.transform.localPosition = new Vector3(
                    0,
                    unit.CharacterTemplate.ClassHatHeightOffset,
                    0
                );
            }
            else if (unit.CharacterTemplate.HairPrefab != null)
            {
                // Fall back to default hair (no offset)
                var hair = Instantiate(unit.CharacterTemplate.HairPrefab, parent.transform);
                hair.name = "Hair";
            }
        }

        private SkinnedMeshRenderer CreateHeadMesh(CharacterInstance unit, GameObject parent)
        {
            if (unit.CharacterTemplate.HeadAndHandsPrefab == null)
            {
                return null;
            }

            var instance = Instantiate(unit.CharacterTemplate.HeadAndHandsPrefab, parent.transform);
            instance.name = "HeadHands";
            return instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }

        private SkinnedMeshRenderer CreateHairMesh(CharacterInstance unit, GameObject parent)
        {
            var classInst = unit.GetCurrentClass();

            // Always use unit hair for non-battle models
            // For battle models, respect the class flags
            var useHair =
                !unit.UseBattleModel
                || classInst?.ClassData == null
                || !classInst.ClassData.HasOutfit
                || classInst.ClassData.UseUnitHairOnModel;

            if (!useHair)
            {
                return null;
            }

            if (unit.CharacterTemplate.HairPrefab == null)
            {
                return null;
            }

            var instance = Instantiate(unit.CharacterTemplate.HairPrefab, parent.transform);
            instance.name = "Hair";
            return instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
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
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.transform.SetParent(parent.transform, worldPositionStays: false);

            // Explicitly ensure it's at local zero
            placeholder.transform.localPosition = Vector3.zero;
            placeholder.transform.localRotation = Quaternion.identity;

            placeholder.GetComponent<Renderer>().material.color =
                unit.CharacterTemplate.AccentColor1;

            return placeholder.AddComponent<SkinnedMeshRenderer>();
        }

        private void UnifyBoneHierarchies(GameObject root)
        {
            var allRoots = new System.Collections.Generic.List<Transform>();
            FindAllRootTransforms(root.transform, allRoots);

            if (allRoots.Count == 0)
            {
                return;
            }

            if (allRoots.Count == 1)
            {
                HandleSingleArmatureRoot(root, allRoots[0]);
                return;
            }

            // Multiple roots - unify them
            var canonicalRoot = FindCanonicalBoneRoot(allRoots);
            var boneMap = BuildBoneMapping(canonicalRoot);
            RebindRendererBones(root, canonicalRoot, boneMap);
            RemoveDuplicateRoots(root, allRoots, canonicalRoot);
        }

        private void HandleSingleArmatureRoot(GameObject root, Transform singleRoot)
        {
            if (singleRoot.parent != root.transform)
            {
                singleRoot.SetParent(root.transform, true);
            }
        }

        private Transform FindCanonicalBoneRoot(System.Collections.Generic.List<Transform> allRoots)
        {
            var canonicalRoot = allRoots
                .OrderByDescending(r => r.GetComponentsInChildren<Transform>().Length)
                .First();

            return canonicalRoot;
        }

        private System.Collections.Generic.Dictionary<string, Transform> BuildBoneMapping(
            Transform canonicalRoot
        )
        {
            var boneMap = new System.Collections.Generic.Dictionary<string, Transform>();
            BuildBoneMap(canonicalRoot, boneMap);
            return boneMap;
        }

        private void RebindRendererBones(
            GameObject root,
            Transform canonicalRoot,
            System.Collections.Generic.Dictionary<string, Transform> boneMap
        )
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.bones != null && renderer.bones.Length > 0)
                {
                    var firstBone = renderer.bones[0];
                    if (firstBone != null && !IsDescendantOf(firstBone, canonicalRoot))
                    {
                        TryRebindRendererToCanonicalBones(renderer, canonicalRoot, boneMap);
                    }
                }
                else
                {
                    renderer.rootBone = canonicalRoot;
                }
            }
        }

        private void TryRebindRendererToCanonicalBones(
            SkinnedMeshRenderer renderer,
            Transform canonicalRoot,
            System.Collections.Generic.Dictionary<string, Transform> boneMap
        )
        {
            var newBones = new Transform[renderer.bones.Length];
            bool success = true;

            for (int i = 0; i < renderer.bones.Length; i++)
            {
                if (
                    renderer.bones[i] != null
                    && boneMap.TryGetValue(renderer.bones[i].name, out var newBone)
                )
                {
                    newBones[i] = newBone;
                }
                else
                {
                    success = false;
                    break;
                }
            }

            if (success)
            {
                renderer.bones = newBones;
                renderer.rootBone = canonicalRoot;
            }
        }

        private void RemoveDuplicateRoots(
            GameObject root,
            System.Collections.Generic.List<Transform> allRoots,
            Transform canonicalRoot
        )
        {
            // Move canonical root to model root
            if (canonicalRoot.parent != root.transform)
            {
                canonicalRoot.SetParent(root.transform, true);
            }

            // Delete duplicate roots
            foreach (var duplicateRoot in allRoots)
            {
                if (duplicateRoot != canonicalRoot && duplicateRoot != null)
                {
                    Destroy(duplicateRoot.gameObject);
                }
            }
        }

        private void FindAllRootTransforms(
            Transform parent,
            System.Collections.Generic.List<Transform> results
        )
        {
            foreach (Transform child in parent)
            {
                // Skip weapon and shield gameobjects - they have their own bone hierarchies
                var childNameLower = child.name.ToLower();
                if (childNameLower.Contains("_weapon") || childNameLower.Contains("_shield"))
                {
                    continue;
                }

                if (
                    childNameLower == "root"
                    || childNameLower == "armature"
                    || childNameLower.StartsWith("root.")
                )
                {
                    results.Add(child);
                }
                else if (!child.GetComponent<SkinnedMeshRenderer>())
                {
                    // Keep searching if this isn't a renderer (don't search into mesh objects)
                    FindAllRootTransforms(child, results);
                }
            }
        }

        private bool IsDescendantOf(Transform child, Transform potentialAncestor)
        {
            var current = child;
            while (current != null)
            {
                if (current == potentialAncestor)
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        private void BuildBoneMap(
            Transform bone,
            System.Collections.Generic.Dictionary<string, Transform> map
        )
        {
            if (bone == null)
            {
                return;
            }

            map[bone.name] = bone;

            foreach (Transform child in bone)
            {
                BuildBoneMap(child, map);
            }
        }
    }
}
