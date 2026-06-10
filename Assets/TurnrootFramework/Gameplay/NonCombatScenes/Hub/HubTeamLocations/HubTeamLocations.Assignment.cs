using Turnroot.Characters;
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
            HubCharacterSpawnArea[] spawnAreas,
            int maxPerLocation
        )
        {
            // Build a pool of candidate locations, excluding Battlefields.
            var pool = new System.Collections.Generic.List<HubCharacterSpawnArea>();
            if (spawnAreas != null)
            {
                foreach (var loc in spawnAreas)
                {
                    if (loc != null && loc.LocationName != HubSublocationName.Battlefields)
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
            HubCharacterSpawnArea[] spawnAreas,
            int maxPerLocation
        )
        {
            HubCharacterSpawnArea assignedLocation = System.Array.Find(
                spawnAreas,
                l => l.LocationName == desiredLocation
            );
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
        }

        private HubSublocationName ResolveAssignedLocationOrRandom(
            HubCharacterSpawnArea assignedArea,
            HubCharacterSpawnArea[] spawnAreas,
            int maxPerLocation
        )
        {
            if (assignedArea != null)
            {
                return assignedArea.LocationName;
            }

            return PickRandomValidLocation(spawnAreas, maxPerLocation);
        }
    }
}
