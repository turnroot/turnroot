using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Characters.Components.Behavior
{
    /// <summary>
    /// Defines AI behavioral traits that influence character decision-making in combat.
    /// </summary>
    [Serializable]
    public struct CharacterBehavior
    {
        public bool MovementDisabled;
        public bool AttackDisabled;

        [SerializeField, Range(0f, 1f)]
        public float SoldierLoneWolf;

        [SerializeField, Range(0f, 1f)]
        public float MindlessCunning;

        [SerializeField, Range(0f, 1f)]
        public float SelfishSelfless;

        [SerializeField, Range(0f, 1f)]
        public float BrashWary;

        [SerializeField, Range(0f, 1f)]
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

        public readonly CharacterBehavior Clone()
        {
            return new CharacterBehavior
            {
                MovementDisabled = this.MovementDisabled,
                AttackDisabled = this.AttackDisabled,
                SoldierLoneWolf = this.SoldierLoneWolf,
                MindlessCunning = this.MindlessCunning,
                SelfishSelfless = this.SelfishSelfless,
                BrashWary = this.BrashWary,
                BloodthirstGreed = this.BloodthirstGreed,
            };
        }

        public struct CharacterBehaviorPreset
        {
            public static CharacterBehavior MindlessBerserker =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.3f,
                    MindlessCunning = 0.1f,
                    SelfishSelfless = 0.5f,
                    BrashWary = 0.1f,
                    BloodthirstGreed = 0.1f,
                };

            public static CharacterBehavior CunningAssassin =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.85f,
                    MindlessCunning = 0.85f,
                    SelfishSelfless = 0.3f,
                    BrashWary = 0.75f,
                    BloodthirstGreed = 0.1f,
                };

            public static CharacterBehavior LoyalGuardian =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.15f,
                    MindlessCunning = 0.5f,
                    SelfishSelfless = 0.8f,
                    BrashWary = 0.5f,
                    BloodthirstGreed = 0.2f,
                };

            public static CharacterBehavior WaryProtector =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.1f,
                    MindlessCunning = 0.8f,
                    SelfishSelfless = 0.8f,
                    BrashWary = 0.8f,
                    BloodthirstGreed = 0.1f,
                };

            public static CharacterBehavior GreedyCoward =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.7f,
                    MindlessCunning = 0.6f,
                    SelfishSelfless = 0.1f,
                    BrashWary = 0.9f,
                    BloodthirstGreed = 0.9f,
                };

            public static CharacterBehavior VengefulWarrior =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.2f,
                    MindlessCunning = 0.5f,
                    SelfishSelfless = 0.75f,
                    BrashWary = 0.4f,
                    BloodthirstGreed = 0.2f,
                };

            public static CharacterBehavior RecklessDuelist =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.8f,
                    MindlessCunning = 0.6f,
                    SelfishSelfless = 0.1f,
                    BrashWary = 0.15f,
                    BloodthirstGreed = 0.1f,
                };

            public static CharacterBehavior BalancedVeteran =>
                new()
                {
                    MovementDisabled = false,
                    AttackDisabled = false,
                    SoldierLoneWolf = 0.4f,
                    MindlessCunning = 0.5f,
                    SelfishSelfless = 0.5f,
                    BrashWary = 0.5f,
                    BloodthirstGreed = 0.4f,
                };
        }

        public enum CharacterBehaviorPresetEnum
        {
            MindlessBerserker,
            CunningAssassin,
            LoyalGuardian,
            WaryProtector,
            GreedyCoward,
            VengefulWarrior,
            RecklessDuelist,
            BalancedVeteran,
        }

        [SerializeField]
        private CharacterBehaviorPresetEnum preset;
        public CharacterBehaviorPresetEnum Preset
        {
            readonly get => preset;
            set
            {
                preset = value;
                SetPreset();
            }
        }

        private void SetPreset()
        {
            switch (preset)
            {
                case CharacterBehaviorPresetEnum.MindlessBerserker:
                    var presetValues = CharacterBehaviorPreset.MindlessBerserker;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.CunningAssassin:
                    presetValues = CharacterBehaviorPreset.CunningAssassin;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.LoyalGuardian:
                    presetValues = CharacterBehaviorPreset.LoyalGuardian;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.WaryProtector:
                    presetValues = CharacterBehaviorPreset.WaryProtector;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.GreedyCoward:
                    presetValues = CharacterBehaviorPreset.GreedyCoward;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.VengefulWarrior:
                    presetValues = CharacterBehaviorPreset.VengefulWarrior;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.RecklessDuelist:
                    presetValues = CharacterBehaviorPreset.RecklessDuelist;
                    this = presetValues;
                    break;
                case CharacterBehaviorPresetEnum.BalancedVeteran:
                    presetValues = CharacterBehaviorPreset.BalancedVeteran;
                    this = presetValues;
                    break;
                default:
                    break;
            }
        }
    }
}
