using System.Linq;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles unit model creation, outfit rendering, and mesh hierarchy management.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        public GameObject GetModelForUnit(string unitId)
        {
            var prep = _brain.battleBrain.PreparationObject;
            return prep?.GetModelForUnit(unitId);
        }

        public GameObject CreateModelForUnit(CharacterInstance unit, GameObject root = null)
        {
            if (root == null)
            {
                root = new GameObject($"{unit.CharacterTemplate.DisplayName}_Root");
            }

            var outfitRenderer = CreateOutfitMesh(unit, root);

            // Ensure any class/character head, hair or hat prefabs are attached before we decide
            // whether the model already contains HeadAndHands / Hair children.
            AttachHeadAndHair(unit, root);

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

            // Apply the shared Humanoid Avatar used for retargeting all character models
            if (_settings.CharacterAvatar != null)
            {
                animator.avatar = _settings.CharacterAvatar;
            }

            // Attach optional procedural hair simulation when enabled and supported.
            AttachHairSimulation(unit, root);

            return root;
        }

        private void AttachHairSimulation(CharacterInstance unit, GameObject root)
        {
            if (_settings == null || !_settings.ProceduralHairSimulation)
            {
                return;
            }

            var chains = unit.CharacterTemplate.ProceduralBoneChains;
            if (chains == null || chains.Length == 0)
            {
                return;
            }

            var driver = root.AddComponent<HairSimulationDriver>();
            driver.Initialize(unit.CharacterTemplate, _brain);
        }

        private SkinnedMeshRenderer CreateOutfitMesh(CharacterInstance unit, GameObject parent)
        {
            // Prefer the class-provided outfit when available.
            if (TryGetClassOutfit(unit, parent, out var classSmr))
            {
                return classSmr;
            }

            // Fallback: use the character's NonBattleOutfitPrefab as a sensible default when the class has no outfit.
            var nonBattle = unit.CharacterTemplate.NonBattleOutfitPrefab;
            if (nonBattle != null)
            {
                var inst = Instantiate(nonBattle, parent.transform);
                inst.name = "ClassOutfit_NonBattleFallback";

                // Ensure any renderer GameObject names won't be excluded by GetOutfitRenderers (avoid 'NonBattleOutfit' prefix).
                var childSmrs =
                    inst.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    ?? new SkinnedMeshRenderer[0];
                foreach (var s in childSmrs)
                {
                    if (s == null)
                    {
                        continue;
                    }

                    var n = s.gameObject.name ?? string.Empty;
                    if (n.StartsWith("NonBattleOutfit", System.StringComparison.OrdinalIgnoreCase))
                    {
                        s.gameObject.name = n.Replace("NonBattleOutfit", "ClassOutfit");
                    }
                }

                var smr = inst.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr != null)
                {
                    LogWarning(
                        $"CreateOutfitMesh: no ClassModelPrefab for {unit.CharacterTemplate.DisplayName}; using NonBattleOutfitPrefab as fallback."
                    );
                    return smr;
                }
            }

            LogError(
                $"CreateOutfitMesh: no ClassModelPrefab found for {unit.CharacterTemplate.DisplayName} and no NonBattleOutfitPrefab is assigned."
            );

            // Let SetPrimaryRenderer choose head/placeholder when outfit is missing
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

            var pronounKey = unit.CharacterTemplate.CharacterPronouns.GetPronounKey();
            var identity = classInst.ClassData.Identity;
            var prefab = identity.GetClassModelPrefab(pronounKey);

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

            LogWarning(
                $"Class outfit prefab '{prefab.name}' is missing a SkinnedMeshRenderer and cannot be used for {unit.CharacterTemplate.DisplayName}."
            );

            Destroy(obj);
            return false;
        }

        private void AttachHeadAndHair(CharacterInstance unit, GameObject parent)
        {
            // Only instantiate HeadAndHands if the prefab isn't already present in the root
            if (
                parent.transform.Find("HeadAndHands") == null
                && unit.CharacterTemplate.HeadAndHandsPrefab != null
            )
            {
                var hh = TryInstantiatePrefab(
                    unit.CharacterTemplate.HeadAndHandsPrefab,
                    parent.transform,
                    "HeadAndHands",
                    "AttachHeadAndHair"
                );
            }

            // Always use the character's HairPrefab (unit hair is authoritative) if not already present
            var hasHair = parent
                .GetComponentsInChildren<Transform>(true)
                .All(t => t.name != "Hair");
            if (unit.CharacterTemplate.HairPrefab != null && hasHair)
            {
                var hair = TryInstantiatePrefab(
                    unit.CharacterTemplate.HairPrefab,
                    parent.transform,
                    "Hair",
                    "AttachHeadAndHair"
                );
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
