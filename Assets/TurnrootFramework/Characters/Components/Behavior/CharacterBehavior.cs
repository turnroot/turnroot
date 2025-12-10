using System;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Characters.Components.Behavior
{
    [Serializable]
    public struct GoalTile
    {
        public MapGridPoint[] Tiles;

        [HideInInspector]
        public int CurrentTileIndex;

        [InfoBox("Turn on to activate this goal for the AI")]
        public bool GoalActive;

        [InfoBox("Turn on to make this goal the highest priority for the AI")]
        public bool MaxPriority;
    }

    [Serializable]
    public struct GoalEnemy
    {
        public CharacterData[] Targets;
        public int CurrentTargetIndex;

        [InfoBox("Turn on to activate this goal for the AI")]
        public bool GoalActive;

        [InfoBox("Turn on to make this goal the highest priority for the AI")]
        public bool MaxPriority;
    }

    [Serializable]
    public struct CharacterBehavior
    {
        [InfoBox("Turn on to lock this character to their starting tile")]
        public bool _movementDisabled;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Lone Wolf units avoid allies. Soldier units want to stay close to allies.")
        ]
        private float _SoldierLoneWolf;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox(
                "Mindless units target the closest enemy. Cunning units prioritize strategic targets."
            )
        ]
        private float _mindlessCunning;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Selfless units protect allies. Selfish units prioritize their own safety.")
        ]
        private float _SelfishSelfless;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Wary units avoid combat and risks. Brash units take bold actions.")
        ]
        private float _brashWary;

        [
            SerializeField,
            Range(0f, 1f),
            InfoBox("Greedy units prioritize loot. Bloodthirsty units prioritize combat.")
        ]
        private float _BloodthirstGreed;

        [SerializeField, InfoBox("Current goal tile for the AI to move towards.")]
        public GoalTile CurrentGoalTile;

        [SerializeField, InfoBox("Specific goal enemy for the AI to target.")]
        public GoalEnemy CurrentGoalEnemy;
    }
}
