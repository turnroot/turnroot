using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Characters.Components.Behavior
{
    [Serializable]
    public struct CharacterBehavior
    {
        [InfoBox("Turn on to lock this character to their starting tile")]
        public bool MovementDisabled;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Soldier units stay close to allies. Lone Wolf units avoid allies. ")
        ]
        public float SoldierLoneWolf;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox(
                "Mindless units target the closest enemy. Cunning units prioritize strategic targets."
            )
        ]
        public float MindlessCunning;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Selfish units prioritize their own safety. Selfless units protect allies. ")
        ]
        public float SelfishSelfless;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Brash units take bold actions. Wary units avoid combat.")
        ]
        public float BrashWary;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Bloodthirsty units prioritize combat. Greedy units prioritize loot.")
        ]
        public float BloodthirstGreed;

        public readonly Dictionary<string, float> GetBehaviorDictionary()
        {
            return new Dictionary<string, float>
            {
                { "SoldierLoneWolf", SoldierLoneWolf },
                { "MindlessCunning", MindlessCunning },
                { "SelfishSelfless", SelfishSelfless },
                { "BrashWary", BrashWary },
                { "BloodthirstGreed", BloodthirstGreed },
            };
        }
    }
}
