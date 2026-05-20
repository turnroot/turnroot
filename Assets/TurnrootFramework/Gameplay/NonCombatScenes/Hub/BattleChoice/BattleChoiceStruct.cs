using System;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Maps;
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

        [Range(0, 100)]
        public int ExtraExperienceReward;
        public bool RequiredStoryBattle;
        public bool Repeateable;
        public bool ParalogueBattle;

        [ShowIf(nameof(ParalogueBattle))]
        public CharacterData ParalogueCharacter;

        [InfoBox("If you are using Map Exploration features, fill these fields")]
        public ExploreStatusSprites MapExplorationSprites;

        [Tooltip("The MapGrid asset for this battle, used to look up quadrant exploration status.")]
        public MapGrid MapForExploration;

        [InfoBox("Otherwise, just put a map image in here")]
        public Sprite MapSprite;
    }
}
