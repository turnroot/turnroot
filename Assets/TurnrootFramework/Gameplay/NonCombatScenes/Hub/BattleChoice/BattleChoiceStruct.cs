using System;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Objects;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay
{
    [Serializable]
    public struct BattleChoiceStruct
    {
        public SceneReference BattleScene;
        public string BattleName;
        public string BattleDescription;

        [Range(1, 3)]
        public int BattleDifficulty;
        public ObjectItem[] rewards;
        public int GoldReward;
        public int ExtraExperienceReward;
        public bool RequiredStoryBattle;
        public bool Repeateable;
        public bool ParalogueBattle;

        [ShowIf(nameof(ParalogueBattle))]
        public CharacterData ParalogueCharacter;

        [HideInInspector]
        public ExploredPartial MapExplorationStatus;
        public Sprite BattleMapPreview;

        [HideInInspector]
        public bool IsAvailable;
    }
}
