using Turnroot.Characters;
using UnityEngine;
using static Turnroot.Characters.CharacterInstance;

namespace Turnroot.Gameplay.Brain.Events
{
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

    // Visual/model events - published when Models are spawned/despawned/moved/swapped in the scene.
    public class ModelSpawnedEvent : BattleEvent
    {
        public string UnitId { get; }
        public CharacterInstance Unit { get; }
        public Vector2Int Position { get; }
        public GameObject Model { get; }

        public ModelSpawnedEvent(
            CharacterInstance unit,
            string unitId,
            Vector2Int position,
            GameObject model
        )
        {
            Unit = unit;
            UnitId = unitId;
            Position = position;
            Model = model;
        }
    }

    public class ModelDespawnedEvent : BattleEvent
    {
        public string UnitId { get; }
        public CharacterInstance Unit { get; }
        public Vector2Int Position { get; }
        public GameObject Model { get; }

        public ModelDespawnedEvent(
            CharacterInstance unit,
            string unitId,
            Vector2Int position,
            GameObject model
        )
        {
            Unit = unit;
            UnitId = unitId;
            Position = position;
            Model = model;
        }
    }

    public class ModelMovedEvent : BattleEvent
    {
        public string UnitId { get; }
        public CharacterInstance Unit { get; }
        public Vector2Int From { get; }
        public Vector2Int To { get; }
        public GameObject Model { get; }

        public ModelMovedEvent(
            CharacterInstance unit,
            string unitId,
            Vector2Int from,
            Vector2Int to,
            GameObject model
        )
        {
            Unit = unit;
            UnitId = unitId;
            From = from;
            To = to;
            Model = model;
        }
    }

    public class ModelSwappedEvent : BattleEvent
    {
        public string UnitIdA { get; }
        public string UnitIdB { get; }
        public Vector2Int PosA { get; }
        public Vector2Int PosB { get; }
        public GameObject ModelA { get; }
        public GameObject ModelB { get; }

        public ModelSwappedEvent(
            string unitIdA,
            string unitIdB,
            Vector2Int posA,
            Vector2Int posB,
            GameObject modelA,
            GameObject modelB
        )
        {
            UnitIdA = unitIdA;
            UnitIdB = unitIdB;
            PosA = posA;
            PosB = posB;
            ModelA = modelA;
            ModelB = modelB;
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
}
