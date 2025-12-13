using System;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Visual and identity information for a character class.
    /// </summary>
    [Serializable]
    public class ClassIdentity
    {
        [Header("Visuals")]
        [Tooltip("3D mesh outfit for this class")]
        public Mesh ClassOutfit;

        [Tooltip("Shader used for rendering")]
        public Shader ShaderGraph;

        [Tooltip("Base texture")]
        public Texture2D Base;

        [Tooltip("MSE texture")]
        public Texture2D MSE;

        [Tooltip("Tint mask texture")]
        public Texture2D TintMask;

        [Header("Identity")]
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

        [Tooltip("If true, only a unique character can hold this class at a time")]
        public bool IsUnique = false;

        [Header("Mobility")]
        [Tooltip("Movement type for this class")]
        public MovementType MovementType = MovementType.Infantry;

        public bool HasRequiredVisuals() => ClassOutfit != null && !string.IsNullOrEmpty(ClassName);

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
