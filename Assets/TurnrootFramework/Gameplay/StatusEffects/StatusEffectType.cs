using UnityEngine;

namespace Turnroot.Characters.StatusEffects
{
    /// <summary>
    /// Defines a status effect type (buff or debuff).
    /// Used for applying temporary modifications to characters during battle.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StatusEffectType",
        menuName = "Turnroot/Game Settings/Characters/Status Effect Type"
    )]
    [System.Serializable]
    public class StatusEffectType : ScriptableObject
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private string _displayName;

        [SerializeField, TextArea(2, 4)]
        private string _description;

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private StatusEffectCategory _category = StatusEffectCategory.Debuff;

        [SerializeField]
        private bool _isStackable = false;

        [SerializeField, Tooltip("Maximum number of stacks if stackable")]
        private int _maxStacks = 1;

        [SerializeField, Tooltip("Default duration in turns (0 = permanent until removed)")]
        private int _defaultDuration = 3;

        [Header("Stat Modifiers")]
        [SerializeField, Tooltip("Flat value added/subtracted from stats")]
        private StatusEffectStatModifier[] _flatModifiers;

        [SerializeField, Tooltip("Percentage multiplier applied to stats")]
        private StatusEffectStatModifier[] _percentModifiers;

        [Header("Special Effects")]
        [SerializeField, Tooltip("Damage/healing per turn (negative = damage)")]
        private int _healthChangePerTurn = 0;

        [SerializeField, Tooltip("Prevents the unit from moving")]
        private bool _preventsMovement = false;

        [SerializeField, Tooltip("Prevents the unit from attacking")]
        private bool _preventsAttack = false;

        [SerializeField, Tooltip("Prevents the unit from using items")]
        private bool _preventsItemUse = false;

        [Header("Behavior Modifiers")]
        [
            SerializeField,
            Tooltip("Modifier to SoldierLoneWolf slider (-1 to 1, added to current value)")
        ]
        private float _soldierLoneWolfModifier = 0f;

        [
            SerializeField,
            Tooltip("Modifier to MindlessCunning slider (-1 to 1, added to current value)")
        ]
        private float _mindlessCunningModifier = 0f;

        [
            SerializeField,
            Tooltip("Modifier to SelfishSelfless slider (-1 to 1, added to current value)")
        ]
        private float _selfishSelflessModifier = 0f;

        [SerializeField, Tooltip("Modifier to BrashWary slider (-1 to 1, added to current value)")]
        private float _brashWaryModifier = 0f;

        [
            SerializeField,
            Tooltip("Modifier to BloodthirstGreed slider (-1 to 1, added to current value)")
        ]
        private float _bloodthirstGreedModifier = 0f;

        public string Id
        {
            get => _id;
            set => _id = value;
        }

        public string DisplayName
        {
            get => _displayName;
            set => _displayName = value;
        }

        public string Description
        {
            get => _description;
            set => _description = value;
        }

        public Sprite Icon
        {
            get => _icon;
            set => _icon = value;
        }

        public StatusEffectCategory Category => _category;
        public bool IsStackable => _isStackable;
        public int MaxStacks => _maxStacks;
        public int DefaultDuration => _defaultDuration;
        public StatusEffectStatModifier[] FlatModifiers => _flatModifiers;
        public StatusEffectStatModifier[] PercentModifiers => _percentModifiers;
        public int HealthChangePerTurn => _healthChangePerTurn;
        public bool PreventsMovement => _preventsMovement;
        public bool PreventsAttack => _preventsAttack;
        public bool PreventsItemUse => _preventsItemUse;
        public float SoldierLoneWolfModifier => _soldierLoneWolfModifier;
        public float MindlessCunningModifier => _mindlessCunningModifier;
        public float SelfishSelflessModifier => _selfishSelflessModifier;
        public float BrashWaryModifier => _brashWaryModifier;
        public float BloodthirstGreedModifier => _bloodthirstGreedModifier;

        public bool HasBehaviorModifiers =>
            !Mathf.Approximately(_soldierLoneWolfModifier, 0f)
            || !Mathf.Approximately(_mindlessCunningModifier, 0f)
            || !Mathf.Approximately(_selfishSelflessModifier, 0f)
            || !Mathf.Approximately(_brashWaryModifier, 0f)
            || !Mathf.Approximately(_bloodthirstGreedModifier, 0f);

        public bool IsBuff => _category == StatusEffectCategory.Buff;
        public bool IsDebuff => _category == StatusEffectCategory.Debuff;

        public override string ToString() => _displayName;
    }

    public enum StatusEffectCategory
    {
        Buff,
        Debuff,
        Neutral,
    }

    [System.Serializable]
    public struct StatusEffectStatModifier
    {
        public Stats.UnboundedStatType StatType;
        public float Value;

        public StatusEffectStatModifier(Stats.UnboundedStatType statType, float value)
        {
            StatType = statType;
            Value = value;
        }
    }
}
