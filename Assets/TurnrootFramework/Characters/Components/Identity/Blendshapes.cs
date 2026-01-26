using System;
using UnityEngine;

namespace Turnroot.Characters
{
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

        public readonly string[] BlendshapeNames =>
            new string[]
            {
                "ChestSize",
                "WaistSize",
                "HipSize",
                "ThighThickness",
                "ArmThickness",
                "NeckThickness",
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
                _ => 0f,
            };
        }
    }
}
