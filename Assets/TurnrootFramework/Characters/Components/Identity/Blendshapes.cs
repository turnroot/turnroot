using System;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Defines blendshape values for character model customization.
    /// </summary>
    [Serializable]
    public struct CharacterModelBlendshapeSet
    {
        [Range(0f, 100f)]
        public float chestSize;

        [Range(0f, 100f)]
        public float waistSize;

        [Range(0f, 100f)]
        public float hipSize;

        [Range(0f, 100f)]
        public float thighThickness;

        [Range(0f, 100f)]
        public float armThickness;

        [Range(0f, 100f)]
        public float neckThickness;

        private const float fixes = 100f;

        public readonly string[] BlendshapeNames =>
            new string[]
            {
                "ChestSize",
                "WaistSize",
                "HipSize",
                "ThighThickness",
                "ArmThickness",
                "NeckThickness",
                "Fixes",
            };

        public readonly float GetBlendshapeByName(string name)
        {
            return name switch
            {
                "ChestSize" => chestSize,
                "WaistSize" => waistSize,
                "HipSize" => hipSize,
                "ThighThickness" => thighThickness,
                "ArmThickness" => armThickness,
                "NeckThickness" => neckThickness,
                "Fixes" => fixes,
                _ => 0f,
            };
        }
    }
}
