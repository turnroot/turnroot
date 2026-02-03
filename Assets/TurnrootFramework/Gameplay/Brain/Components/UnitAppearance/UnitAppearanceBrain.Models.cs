using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain
    {
        /// <summary>
        /// Creates a complete model GameObject for the specified unit.
        /// Assembles outfit, head, and hair pieces into a single hierarchy.
        /// </summary>
        public GameObject CreateModelForUnit(CharacterInstance unit)
        {
            var root = new GameObject($"{unit.CharacterTemplate.DisplayName}_Root");

            var outfitRenderer = CreateOutfitMesh(unit, root);

            // Some outfits include their own head/hands or hair
            var hasHead = root.transform.Find("HeadAndHands") != null;
            var hasHair = root.transform.Find("Hair") != null;

            var headRenderer = hasHead ? outfitRenderer : CreateHeadMesh(unit, root);
            var hairRenderer = hasHair ? null : CreateHairMesh(unit, root);

            SetPrimaryRenderer(unit, outfitRenderer, headRenderer, root);

            // CRITICAL: Unify all bone hierarchies before adding animator
            // All meshes share the same bone names but each brought its own copy
            // We need to rebind them all to use ONE shared armature
            UnifyBoneHierarchies(root);

            // Add Animator component at the root after bones are unified
            var animator = root.AddComponent<Animator>();
            if (_settings?.DefaultUnitAnimatorController != null)
            {
                animator.runtimeAnimatorController = _settings.DefaultUnitAnimatorController;
            }

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
                $"No suitable outfit found for {unit.CharacterTemplate?.DisplayName}. "
                    + "Ensure class model or NonBattleOutfitPrefab is assigned.",
                TurnrootLogger.LogLevel.Error
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

            var prefab = classInst.ClassData.Identity?.ClassModelPrefab;
            if (prefab == null)
            {
                return false;
            }

            var obj = Instantiate(prefab, parent.transform);
            obj.name = "ClassOutfit";
            smr = obj.GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (smr != null)
            {
                TurnrootLogger.Log(
                    $"Using class outfit prefab for {unit.CharacterTemplate?.DisplayName}"
                );
                return true;
            }

            TurnrootLogger.Log(
                $"Class outfit prefab '{prefab.name}' is missing a SkinnedMeshRenderer. "
                    + $"Falling back to NonBattleOutfitPrefab for {unit.CharacterTemplate?.DisplayName}",
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
                        + $"Cannot create outfit for {unit.CharacterTemplate?.DisplayName}",
                    TurnrootLogger.LogLevel.Error
                );
                Destroy(nbInstance);
                return null;
            }

            TurnrootLogger.Log(
                $"Using non-battle outfit prefab for {unit.CharacterTemplate?.DisplayName}"
            );

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

            if (unit.CharacterTemplate.HairPrefab != null)
            {
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
            TurnrootLogger.Log(
                $"No renderers for {unit.CharacterTemplate.DisplayName}, creating placeholder"
            );

            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.transform.SetParent(parent.transform);
            placeholder.GetComponent<Renderer>().material.color =
                unit.CharacterTemplate.AccentColor1;

            return placeholder.AddComponent<SkinnedMeshRenderer>();
        }

        /// <summary>
        /// Unifies all bone hierarchies under the model root.
        /// Finds all "root" transforms (armatures), picks one as canonical,
        /// and removes duplicates. If prefabs are set up correctly, renderers
        /// should automatically reference the unified hierarchy.
        /// </summary>
        private void UnifyBoneHierarchies(GameObject root)
        {
            // Find all "root" transforms (typically named "root", "Armature", etc.)
            var allRoots = new System.Collections.Generic.List<Transform>();
            FindAllRootTransforms(root.transform, allRoots);

            if (allRoots.Count == 0)
            {
                TurnrootLogger.Log(
                    "UnifyBoneHierarchies: No armature root found in hierarchy",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            if (allRoots.Count == 1)
            {
                // Only one root - just move it to model root and we're done
                var singleRoot = allRoots[0];
                if (singleRoot.parent != root.transform)
                {
                    singleRoot.SetParent(root.transform, true);
                }
                TurnrootLogger.Log(
                    $"UnifyBoneHierarchies: Single armature '{singleRoot.name}' found and positioned"
                );
                return;
            }

            // Multiple roots - pick the one with the most child bones as canonical
            Transform canonicalRoot = allRoots
                .OrderByDescending(r => r.GetComponentsInChildren<Transform>().Length)
                .First();

            TurnrootLogger.Log(
                $"UnifyBoneHierarchies: Found {allRoots.Count} armatures, using '{canonicalRoot.name}' as canonical "
                    + $"({canonicalRoot.GetComponentsInChildren<Transform>().Length} bones)"
            );

            // Build bone name map from canonical hierarchy
            var boneMap = new System.Collections.Generic.Dictionary<string, Transform>();
            BuildBoneMap(canonicalRoot, boneMap);

            // For each renderer, try to rebind bones if they reference a duplicate hierarchy
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.bones != null && renderer.bones.Length > 0)
                {
                    // Check if this renderer's bones reference a non-canonical root
                    var firstBone = renderer.bones[0];
                    if (firstBone != null && !IsDescendantOf(firstBone, canonicalRoot))
                    {
                        // Try to rebind to canonical hierarchy
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
                            TurnrootLogger.Log(
                                $"UnifyBoneHierarchies: Rebound '{renderer.name}' to canonical bones"
                            );
                        }
                    }
                }
                else
                {
                    // No bones assigned - just set root bone as a hint
                    renderer.rootBone = canonicalRoot;
                }
            }

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
                    TurnrootLogger.Log(
                        $"UnifyBoneHierarchies: Removing duplicate armature '{duplicateRoot.name}'"
                    );
                    Object.Destroy(duplicateRoot.gameObject);
                }
            }
        }

        /// <summary>
        /// Recursively finds all transforms that look like armature roots.
        /// </summary>
        private void FindAllRootTransforms(
            Transform parent,
            System.Collections.Generic.List<Transform> results
        )
        {
            foreach (Transform child in parent)
            {
                var childName = child.name.ToLower();

                if (childName == "root" || childName == "armature" || childName.StartsWith("root."))
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

        /// <summary>
        /// Checks if a transform is a descendant of another.
        /// </summary>
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

        /// <summary>
        /// Recursively builds a map of bone names to transforms.
        /// </summary>
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
