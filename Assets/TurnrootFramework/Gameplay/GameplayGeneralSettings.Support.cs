using NaughtyAttributes;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.GameSettings
{
    public partial class GameplayGeneralSettings
        : SingletonScriptableObject<GameplayGeneralSettings>
    {
        [HorizontalLine(color: EColor.Green)]
        [
            BoxGroup("Support Settings"),
            InfoBox(
                "All the numbers below are multiplied by speed. Tweak this for your game length and how fast you want supports to grow"
            )
        ]
        [Range(.5f, 5f)]
        public float SupportGrowthSpeed = 2.4f;

        [
            BoxGroup("Support Settings"),
            InfoBox(
                "These are starting multipliers. All these numbers are multiplied by 1 + (Character A Charm + Character B Charm)/25 for any support pairing"
            )
        ]
        public float HubInteractionSupportPoints = .25f;

        [BoxGroup("Support Settings")]
        public bool UseCharmMultiplierForNegativePoints = false;

        [BoxGroup("Support Settings")]
        public float HubInteractionTalkSupportPoints = .5f;

        [
            BoxGroup("Support Settings"),
            InfoBox(
                "This is multiplied by gift rank, so a rank 3 gift would give 3x this amount if the unit likes it"
            )
        ]
        public float GiftSupportPointsUnitLikes = 1f;

        [BoxGroup("Support Settings")]
        public float GiftSupportPointsUnitDislikes = 0.1f;

        [
            BoxGroup("Support Settings"),
            InfoBox(
                "Meal scores are added for both units, not averaged. I.e. if both like a meal the relationship grows by 2x"
            )
        ]
        public float OneOnOneMealSupportPointsUnitLikes = 1.5f;

        [BoxGroup("Support Settings")]
        public float OneOnOneMealSupportPointsUnitDislikes = 0.5f;

        [BoxGroup("Support Settings")]
        public float GroupMealSupportPointsUnitLikes = 1f;

        [BoxGroup("Support Settings")]
        public float GroupMealSupportPointsUnitDislikes = 0.25f;

        [BoxGroup("Support Settings"), InfoBox("Dancing, spa, etc")]
        public float SpecialHubInteractionSupportPoints = 1.5f;

        [
            BoxGroup("Support Settings"),
            InfoBox("Amount of support points gained per turn when adjacent in battle")
        ]
        public float AdjacentInBattleSupportPointsPerTurn = .25f;

        [BoxGroup("Support Settings")]
        public float AdjacentAllyDefeatsEnemySupportPoints = 1f;

        [BoxGroup("Support Settings")]
        public float HealAnAllySupportPoints = 3f;

        [BoxGroup("Support Settings")]
        public int NearbyAllyRadius = 3;

        [
            BoxGroup("Support Settings"),
            InfoBox(
                "Support points gained when an ally within NearbyAllyRadius defeats an enemy. Compounds with AdjacentAllyDefeatsEnemySupportPoints if the ally is also adjacent."
            )
        ]
        public float NearbyAllyDefeatsEnemySupportPoints = .5f;

        [BoxGroup("Support Settings")]
        public float RecruitSuccessSupportPoints = 5f;

        [BoxGroup("Support Settings")]
        public float RecruitFailureSupportPoints = 1f;

        [BoxGroup("Support Settings")]
        public float GoodConversationChoiceSupportPoints = 1f;

        [BoxGroup("Support Settings")]
        public float BadConversationChoiceSupportPoints = -1f;

        [BoxGroup("Support Settings")]
        public float TradeWithAllySupportPoints = .5f;

        [BoxGroup("Support Settings")]
        public float SupportConversationSupportPoints = 2f;
    }
}
