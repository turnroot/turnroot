using System;
using Turnroot.Characters;
using Turnroot.Components.UI;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubTeamLocations
    {
        private HubSublocationName PickRandomValidLocation(
            HubSubLocation[] subLocations,
            int maxPerLocation
        )
        {
            int attempts = 0;
            int pickIndex = HubDayRandom.Range(0, subLocations.Length);

            while (attempts < subLocations.Length)
            {
                var assignedLocation = subLocations[pickIndex];

                if (assignedLocation.LocationName == HubSublocationName.Battlefields)
                {
                    pickIndex = (pickIndex + 1) % subLocations.Length;
                    attempts++;
                    continue;
                }

                assignedLocation.CharactersPresent ??= new CharacterInstance[0];
                if (assignedLocation.CharactersPresent.Length >= maxPerLocation)
                {
                    pickIndex = (pickIndex + 1) % subLocations.Length;
                    attempts++;
                    continue;
                }

                return assignedLocation.LocationName;
            }

            foreach (var location in subLocations)
            {
                if (location.LocationName != HubSublocationName.Battlefields)
                {
                    return location.LocationName;
                }
            }

            return HubSublocationName.Market;
        }

        private void AssignUnitToLocation(
            PlayerTeamRoster roster,
            int rosterIndex,
            Characters.Roster.UnitPlacement unit,
            HubSublocationName desiredLocation,
            HubSubLocation[] subLocations,
            int maxPerLocation
        )
        {
            var assignedLocation = Array.Find(subLocations, l => l.LocationName == desiredLocation);
            if (assignedLocation == null)
            {
                return;
            }

            assignedLocation.CharactersPresent ??= new CharacterInstance[0];
            if (assignedLocation.CharactersPresent.Length >= maxPerLocation)
            {
                return;
            }

            CharacterInstance ci = _charFactory?.CreateOrRecall(unit.CharacterData);
            if (ci == null)
            {
                return;
            }

            var list = new System.Collections.Generic.List<CharacterInstance>(
                assignedLocation.CharactersPresent
            )
            {
                ci,
            };
            assignedLocation.CharactersPresent = list.ToArray();

            int layoutIndex = FindLayoutIndexForLocation(assignedLocation.LocationName);
            if (
                layoutIndex >= 0
                && layoutIndex < LocationLayouts.Length
                && LocationLayouts[layoutIndex].layoutObject != null
                && UnitLocationPortraitPrefab != null
            )
            {
                var portrait = Instantiate(
                    UnitLocationPortraitPrefab,
                    LocationLayouts[layoutIndex].layoutObject.transform
                );
                var portraitScript = portrait.GetComponent<UnitLocationPortraitRefs>();
                if (portraitScript != null)
                {
                    portraitScript.Set(
                        ci.CharacterTemplate.DisplayName,
                        ci.CharacterTemplate.DefaultPortrait?.RuntimeSprite ?? FallBackPortrait
                    );
                }
            }
            else
            {
                $"HubTeamLocations: No horizontal layout prefab or unit portrait prefab assigned for {assignedLocation.LocationName}".LogWarning();
            }
        }

        private int FindLayoutIndexForLocation(HubSublocationName locationName)
        {
            if (LocationLayouts == null)
            {
                return -1;
            }

            for (int idx = 0; idx < LocationLayouts.Length; idx++)
            {
                if (LocationLayouts[idx].location == locationName)
                {
                    return idx;
                }
            }
            return -1;
        }
    }
}
