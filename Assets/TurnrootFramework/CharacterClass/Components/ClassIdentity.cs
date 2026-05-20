using System;
using NaughtyAttributes;
using Turnroot.GameSettings;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    [Serializable]
    public struct PronounPrefab
    {
        [Tooltip("Pronoun key (e.g. 'she', 'he', 'they')")]
        public string pronounKey;

        [Tooltip("Prefab to use for the specified pronoun key")]
        public GameObject prefab;
    }

    /// <summary>
    /// Visual and identity information for a character class.
    /// </summary>
    [Serializable]
    public class ClassIdentity
    {
        [Tooltip(
            "Optional per-pronoun class model overrides. Specify a pronoun key (e.g. 'she','he','they') and a prefab to use for units with that pronoun set."
        )]
        [HideInInspector]
        public PronounPrefab[] PronounClassModelPrefabs = new PronounPrefab[0];

        [Tooltip("Base texture")]
        public Texture2D Base;

        [Tooltip("MSE texture")]
        public Texture2D MSE;

        [Tooltip(
            "Prefab containing a SkinnedMeshRenderer for this class outfit (required). May include head/hands or hat; hair should NOT be included — unit HairPrefab will be used."
        )]
        public GameObject ClassModelPrefab;

        [Tooltip("Prefab for short-body characters. If unset, falls back to ClassModelPrefab.")]
        public GameObject ClassModelPrefabShort;

        [Tooltip("Tint mask texture")]
        public Texture2D TintMask;

        [Tooltip("Display name for this class")]
        public string ClassName;

        [Tooltip("Short description or flavour text for the class")]
        [TextArea(2, 6)]
        public string Description;

        [Tooltip("Optional icon for UI / inspector")]
        public Sprite Icon;

        [Tooltip("Progression tier of this class")]
        public ProgressionLevel ClassTier = ProgressionLevel.Base;

        [Tooltip("Whether this is a magic-based class")]
        public bool IsMagic;

        [Tooltip("If true, this class can perform healing actions")]
        public bool CanHeal = false;

        [Tooltip("If true, only a unique character can hold this class at a time")]
        public bool IsUnique = false;

        [Tooltip("Movement type for this class")]
        public MovementType MovementType = MovementType.Infantry;

        [
            Tooltip("Prefab for mount (used when MovementType is Riding or Flying)"),
            ShowIf(nameof(IsMountedClass))
        ]
        public GameObject MountPrefab;

        [
            Tooltip("Animator for mount (used when MovementType is Riding or Flying)"),
            ShowIf(nameof(IsMountedClass))
        ]
        public RuntimeAnimatorController MountAnimator;

        [Tooltip("Offset for positioning the unit on the mount"), ShowIf(nameof(HasMountVisuals))]
        public Vector3 MountOffset = new(0, 1f, 0);

        public bool IsMountedClass() => MovementType is MovementType.Riding or MovementType.Flying;

        public bool HasMountVisuals() => IsMountedClass() && MountPrefab != null;

        // Return true if this class has a usable class model (either default or a pronoun-specific override)
        public bool HasRequiredVisuals() =>
            (
                ClassModelPrefab != null
                || ClassModelPrefabShort != null
                || (PronounClassModelPrefabs != null && PronounClassModelPrefabs.Length > 0)
            ) && !string.IsNullOrEmpty(ClassName);

        /// <summary>
        /// Get the class model prefab that best matches the given pronoun key and body build.
        /// Falls back to the default ClassModelPrefab when no specific override exists.
        /// </summary>
        public GameObject GetClassModelPrefab(string pronounKey, BodyBuild build)
        {
            // Pronoun overrides take highest priority
            if (!string.IsNullOrEmpty(pronounKey) && PronounClassModelPrefabs != null)
            {
                foreach (var p in PronounClassModelPrefabs)
                {
                    if (
                        !string.IsNullOrEmpty(p.pronounKey)
                        && string.Equals(
                            p.pronounKey,
                            pronounKey,
                            StringComparison.OrdinalIgnoreCase
                        )
                        && p.prefab != null
                    )
                    {
                        return p.prefab;
                    }
                }
            }

            // Short body build: prefer ClassModelPrefabShort, fall back to ClassModelPrefab
            if (build == BodyBuild.Short)
            {
                return ClassModelPrefabShort ?? ClassModelPrefab;
            }

            // Tall body build: prefer ClassModelPrefab, fall back to ClassModelPrefabShort
            return ClassModelPrefab ?? ClassModelPrefabShort;
        }

        /// <summary>
        /// Get the class model prefab that best matches the given pronoun key.
        /// Falls back to the default ClassModelPrefab when no pronoun-specific entry exists.
        /// </summary>
        public GameObject GetClassModelPrefabForPronoun(string pronounKey) =>
            GetClassModelPrefab(pronounKey, BodyBuild.Tall);

        public string GetTierDisplayName()
        {
            return ClassTier switch
            {
                ProgressionLevel.Starter => "Starter Class",
                ProgressionLevel.Base => "Base Class",
                ProgressionLevel.Advanced => "Advanced Class",
                ProgressionLevel.Master => "Master Class",
                ProgressionLevel.Expert => "Expert Class",
                _ => "Unknown",
            };
        }
    }
}
