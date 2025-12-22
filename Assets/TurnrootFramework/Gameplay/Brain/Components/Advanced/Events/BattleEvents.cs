using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using UnityEngine;
using static Turnroot.Characters.CharacterInstance;

namespace Turnroot.Gameplay.Brain.Events
{
    /// <summary>
    /// Base class for all battle events published through the PriorityEventBus.
    /// </summary>
    public abstract class BattleEvent
    {
        public int TurnNumber { get; set; }
        public float Timestamp { get; set; }

        protected BattleEvent()
        {
            Timestamp = Time.time;
        }
    }

    #region Unit Events

    /// <summary>
    /// Published when a unit moves to a new position.
    /// </summary>
    public class UnitMovedEvent : BattleEvent
    {
        public CharacterInstance Unit { get; }
        public Vector2Int FromPosition { get; }
        public Vector2Int ToPosition { get; }

        public UnitMovedEvent(CharacterInstance unit, Vector2Int from, Vector2Int to)
        {
            Unit = unit;
            FromPosition = from;
            ToPosition = to;
        }
    }

    public class UnitSpawnedEvent : BattleEvent
    {
        public CharacterInstance Unit { get; }
        public Vector2Int SpawnPosition { get; }

        public UnitSpawnedEvent(CharacterInstance unit, Vector2Int position)
        {
            Unit = unit;
            SpawnPosition = position;
        }
    }

    public class UnitDespawnedEvent : BattleEvent
    {
        public CharacterInstance Unit { get; }
        public Vector2Int DespawnPosition { get; }

        public UnitDespawnedEvent(CharacterInstance unit, Vector2Int position)
        {
            Unit = unit;
            DespawnPosition = position;
        }
    }

    public class UnitEmotionChangesEvent : BattleEvent
    {
        public CharacterInstance Unit { get; }
        public BattleEmotion OldEmotion { get; }
        public BattleEmotion NewEmotion { get; }

        public UnitEmotionChangesEvent(
            CharacterInstance unit,
            BattleEmotion oldEmotion,
            BattleEmotion newEmotion
        )
        {
            Unit = unit;
            OldEmotion = oldEmotion;
            NewEmotion = newEmotion;
        }
    }

    /// <summary>
    /// Published when a unit is defeated (HP reaches 0).
    /// </summary>
    public class UnitDefeatedEvent : BattleEvent
    {
        public CharacterInstance Unit { get; }
        public CharacterInstance Killer { get; }
        public string CauseOfDeath { get; }

        public UnitDefeatedEvent(
            CharacterInstance unit,
            CharacterInstance killer = null,
            string cause = null
        )
        {
            Unit = unit;
            Killer = killer;
            CauseOfDeath = cause ?? "Unknown";
        }
    }

    #endregion

    #region Damage Events

    /// <summary>
    /// Published when any unit takes damage.
    /// </summary>
    public class UnitDamagedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public CharacterInstance Source { get; }
        public int DamageAmount { get; }
        public int RemainingHP { get; }
        public bool WasLethal { get; }
        public string DamageSource { get; }

        public UnitDamagedEvent(
            CharacterInstance target,
            CharacterInstance source,
            int damage,
            int remainingHP,
            string damageSource = null
        )
        {
            Target = target;
            Source = source;
            DamageAmount = damage;
            RemainingHP = remainingHP;
            WasLethal = remainingHP <= 0;
            DamageSource = damageSource ?? "Attack";
        }
    }

    /// <summary>
    /// Published when a unit is healed.
    /// </summary>
    public class UnitHealedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public CharacterInstance Healer { get; }
        public int HealAmount { get; }
        public int NewHP { get; }

        public UnitHealedEvent(
            CharacterInstance target,
            CharacterInstance healer,
            int heal,
            int newHP
        )
        {
            Target = target;
            Healer = healer;
            HealAmount = heal;
            NewHP = newHP;
        }
    }

    #endregion

    #region Turn Events

    /// <summary>
    /// Published when a new battle round begins (all factions have acted).
    /// </summary>
    public class TurnStartedEvent : BattleEvent
    {
        public TurnStartedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Published when a battle round ends.
    /// </summary>
    public class TurnEndedEvent : BattleEvent
    {
        public TurnEndedEvent(int turnNumber)
        {
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Published when a faction's phase starts.
    /// </summary>
    public class FactionTurnStartedEvent : BattleEvent
    {
        public enum Faction
        {
            Player,
            Enemy,
            ThirdParty,
        }

        public Faction ActiveFaction { get; }

        public FactionTurnStartedEvent(Faction faction, int turnNumber)
        {
            ActiveFaction = faction;
            TurnNumber = turnNumber;
        }
    }

    /// <summary>
    /// Published when a faction's phase ends.
    /// </summary>
    public class FactionTurnEndedEvent : BattleEvent
    {
        public FactionTurnStartedEvent.Faction EndedFaction { get; }

        public FactionTurnEndedEvent(FactionTurnStartedEvent.Faction faction, int turnNumber)
        {
            EndedFaction = faction;
            TurnNumber = turnNumber;
        }
    }

    #endregion

    #region Combat Events

    /// <summary>
    /// Published when an attack is initiated (before damage calculation).
    /// </summary>
    public class AttackInitiatedEvent : BattleEvent
    {
        public CharacterInstance Attacker { get; }
        public CharacterInstance Defender { get; }
        public bool IsCritical { get; set; }
        public bool WillMiss { get; set; }

        public AttackInitiatedEvent(CharacterInstance attacker, CharacterInstance defender)
        {
            Attacker = attacker;
            Defender = defender;
        }
    }

    /// <summary>
    /// Published when a critical hit occurs.
    /// </summary>
    public class CriticalHitEvent : BattleEvent
    {
        public CharacterInstance Attacker { get; }
        public CharacterInstance Target { get; }
        public float DamageMultiplier { get; }

        public CriticalHitEvent(
            CharacterInstance attacker,
            CharacterInstance target,
            float multiplier = 2f
        )
        {
            Attacker = attacker;
            Target = target;
            DamageMultiplier = multiplier;
        }
    }

    #endregion

    #region Skill Events

    /// <summary>
    /// Published when a skill is activated.
    /// </summary>
    public class SkillActivatedEvent : BattleEvent
    {
        public CharacterInstance Caster { get; }
        public Skill Skill { get; }
        public CharacterInstance[] Targets { get; }

        public SkillActivatedEvent(
            CharacterInstance caster,
            Skill skill,
            CharacterInstance[] targets
        )
        {
            Caster = caster;
            Skill = skill;
            Targets = targets ?? System.Array.Empty<CharacterInstance>();
        }
    }

    /// <summary>
    /// Published when a skill finishes executing.
    /// </summary>
    public class SkillCompletedEvent : BattleEvent
    {
        public CharacterInstance Caster { get; }
        public Skill Skill { get; }
        public bool WasSuccessful { get; }

        public SkillCompletedEvent(CharacterInstance caster, Skill skill, bool success)
        {
            Caster = caster;
            Skill = skill;
            WasSuccessful = success;
        }
    }

    #endregion

    #region Item Events

    /// <summary>
    /// Published when an item is used in battle.
    /// </summary>
    public class ItemUsedEvent : BattleEvent
    {
        public CharacterInstance User { get; }
        public ObjectItemInstance Item { get; }
        public CharacterInstance Target { get; }
        public int RemainingUses { get; }

        public ItemUsedEvent(
            CharacterInstance user,
            ObjectItemInstance item,
            CharacterInstance target,
            int remaining
        )
        {
            User = user;
            Item = item;
            Target = target;
            RemainingUses = remaining;
        }
    }

    /// <summary>
    /// Published when an item breaks (uses reach 0).
    /// </summary>
    public class ItemBrokenEvent : BattleEvent
    {
        public CharacterInstance Owner { get; }
        public ObjectItemInstance Item { get; }

        public ItemBrokenEvent(CharacterInstance owner, ObjectItemInstance item)
        {
            Owner = owner;
            Item = item;
        }
    }

    #endregion

    #region Battle Lifecycle Events

    /// <summary>
    /// Published when a battle starts.
    /// </summary>
    public class BattleStartedEvent : BattleEvent
    {
        public string BattleId { get; }

        public BattleStartedEvent(string battleId = null)
        {
            BattleId = battleId ?? System.Guid.NewGuid().ToString();
            TurnNumber = 1;
        }
    }

    /// <summary>
    /// Published when a battle ends.
    /// </summary>
    public class BattleEndedEvent : BattleEvent
    {
        public enum BattleResult
        {
            Victory,
            Defeat,
            Retreat,
            Draw,
        }

        public BattleResult Result { get; }
        public string BattleId { get; }

        public BattleEndedEvent(BattleResult result, int finalTurn, string battleId = null)
        {
            Result = result;
            TurnNumber = finalTurn;
            BattleId = battleId;
        }
    }

    #endregion

    #region Status Effect Events

    /// <summary>
    /// Published when a status effect is applied to a unit.
    /// </summary>
    public class StatusEffectAppliedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public CharacterInstance Source { get; }
        public string EffectId { get; }
        public int Duration { get; }

        public StatusEffectAppliedEvent(
            CharacterInstance target,
            CharacterInstance source,
            string effectId,
            int duration
        )
        {
            Target = target;
            Source = source;
            EffectId = effectId;
            Duration = duration;
        }
    }

    /// <summary>
    /// Published when a status effect expires or is removed.
    /// </summary>
    public class StatusEffectRemovedEvent : BattleEvent
    {
        public CharacterInstance Target { get; }
        public string EffectId { get; }
        public bool WasDispelled { get; }

        public StatusEffectRemovedEvent(
            CharacterInstance target,
            string effectId,
            bool dispelled = false
        )
        {
            Target = target;
            EffectId = effectId;
            WasDispelled = dispelled;
        }
    }

    #endregion
}
