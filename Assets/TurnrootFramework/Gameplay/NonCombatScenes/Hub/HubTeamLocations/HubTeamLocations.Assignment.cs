using Turnroot.Characters;
using Turnroot.Components.UI;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubTeamLocations
    {
        /// <summary>
        /// Returns the best-matching <see cref="HubCharacterLocation"/> for <paramref name="character"/>
        /// given the current save-file chapter. An exact chapter match takes priority; chapter 0 is the
        /// "all chapters" fallback. Returns a default struct (Character == null) if nothing is configured.
        /// </summary>
        private HubCharacterLocation FindHubCharacterLocationForChapter(CharacterData character)
        {
            if (character == null || HubCharacterLocations == null)
            {
                return default;
            }

            int currentChapter = _brain.saveFileBrain.ActiveSaveFile.ChapterNumber;

            HubCharacterLocation fallback = default;

            foreach (var entry in HubCharacterLocations)
            {
                if (!CharacterDataUtilities.CharacterDataMatches(entry.Character, character))
                {
                    continue;
                }

                if (entry.Chapter == currentChapter)
                {
                    return entry;
                }

                if (entry.Chapter == 0)
                {
                    fallback = entry;
                }
            }

            $"HubTeamLocations: No exact match found for character {character?.DisplayName} in chapter {currentChapter}, returning fallback.".LogWarning();

            return fallback;
        }

        private HubSublocationName PickRandomValidLocation(
            HubSubLocation[] subLocations,
            HubExploreLocation[] exploreLocations,
            int maxPerLocation
        )
        {
            // Build a combined pool of candidate locations, excluding Battlefields and locked explore locations.
            var pool = new System.Collections.Generic.List<HubSubLocation>();
            if (subLocations != null)
            {
                foreach (var loc in subLocations)
                {
                    if (loc != null && loc.LocationName != HubSublocationName.Battlefields)
                    {
                        pool.Add(loc);
                    }
                }
            }
            if (exploreLocations != null)
            {
                foreach (var loc in exploreLocations)
                {
                    if (loc != null && !loc.IsLocked)
                    {
                        pool.Add(loc);
                    }
                }
            }

            if (pool.Count == 0)
            {
                return HubSublocationName.Market;
            }

            int pickIndex = HubDayRandom.Range(0, pool.Count);
            int attempts = 0;

            while (attempts < pool.Count)
            {
                var candidate = pool[pickIndex];
                candidate.CharactersPresent ??= new CharacterInstance[0];
                if (candidate.CharactersPresent.Length < maxPerLocation)
                {
                    return candidate.LocationName;
                }
                pickIndex = (pickIndex + 1) % pool.Count;
                attempts++;
            }

            // All locations are full — return the first non-Battlefield.
            return pool[0].LocationName;
        }

        private void AssignUnitToLocation(
            PlayerTeamRoster roster,
            int rosterIndex,
            Characters.Roster.UnitPlacement unit,
            HubSublocationName desiredLocation,
            HubSubLocation[] subLocations,
            HubExploreLocation[] exploreLocations,
            int maxPerLocation
        )
        {
            HubSubLocation assignedLocation = System.Array.Find(
                subLocations,
                l => l.LocationName == desiredLocation
            );
            if (assignedLocation == null && exploreLocations != null)
            {
                assignedLocation = System.Array.Find(
                    exploreLocations,
                    l => l.LocationName == desiredLocation
                );
            }
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

            // All explore locations (Cafe, DanceHall, Spa, etc.) share the ExploreMisc layout group.
            var layoutLocationName =
                assignedLocation is HubExploreLocation
                    ? HubSublocationName.ExploreMisc
                    : assignedLocation.LocationName;
            int layoutIndex = FindLayoutIndexForLocation(layoutLocationName);
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
