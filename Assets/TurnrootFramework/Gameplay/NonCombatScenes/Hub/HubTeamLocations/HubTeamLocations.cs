using System;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using UnityEngine;
using static Turnroot.Characters.Roster;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct HubCharacterLocation
    {
        public CharacterData Character;
        public HubSublocationName Location;

        public int Chapter;

        [InfoBox(
            "If true, the character will be randomly assigned to a valid location this chapter instead of using the specified location."
        )]
        public bool IsRandomForThisChapter;
    }

    [RequireComponent(typeof(HubManager))]
    public partial class HubTeamLocations : MonoBehaviour
    {
        private Brain.Brain _brain;
        private HubManager _hubManager;
        private CharacterFactory _charFactory;
        private readonly System.Collections.Generic.HashSet<string> _spawnedCharacterIds = new();
        public HubCharacterLocation[] HubCharacterLocations;

        [Serializable]
        public struct LocationLayout
        {
            public HubSublocationName location;
            public GameObject layoutObject;
        }

        public LocationLayout[] LocationLayouts;
        public GameObject UnitLocationPortraitPrefab;

        public Sprite FallBackPortrait;

        private HubExploreLocation[] _exploreLocations;

        public void Initialize(
            Brain.Brain brain,
            HubSubLocation[] subLocations,
            HubExploreLocation[] exploreLocations = null
        )
        {
            _hubManager = GetComponent<HubManager>();
            _brain = brain;
            _charFactory = new CharacterFactory(_brain.ltm);
            _exploreLocations = exploreLocations ?? Array.Empty<HubExploreLocation>();

            var persistentRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            SetTeamLocations(persistentRoster, subLocations, _exploreLocations);
            SetNonRosterUnitsInHub(persistentRoster, subLocations, _exploreLocations);

            SpawnAllCharacters(subLocations, _brain);
            SpawnAllCharacters(_exploreLocations, _brain);
        }

        public void SpawnAllCharacters(HubSubLocation[] subLocations, Brain.Brain brain)
        {
            if (subLocations == null || subLocations.Length == 0)
            {
                return;
            }

            foreach (var location in subLocations)
            {
                SpawnCharactersForLocation(location, brain);
            }
        }

        public void SetTeamLocations(
            PlayerTeamRoster roster,
            HubSubLocation[] subLocations,
            HubExploreLocation[] exploreLocations = null
        )
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            var placementMap = LoadSavedPlacement(roster);
            bool changed = false;

            if (placementMap == null)
            {
                placementMap = new System.Collections.Generic.Dictionary<int, HubSublocationName>();
                changed = true;
            }

            for (int i = 0; i < roster.characters.Length; i++)
            {
                var unit = roster.characters[i];
                if (unit.Status == UnitStatus.Defeated || subLocations.Length == 0)
                {
                    continue;
                }

                // don't ever spawn avatar
                if (unit.CharacterData != null && !unit.CharacterData.IsNotAvatar)
                {
                    continue;
                }

                var userSet = FindHubCharacterLocationForChapter(unit.CharacterData);

                if (!placementMap.TryGetValue(i, out var desiredLocation))
                {
                    if (userSet.Character != null)
                    {
                        desiredLocation = userSet.IsRandomForThisChapter
                            ? PickRandomValidLocation(
                                subLocations,
                                exploreLocations ?? Array.Empty<HubExploreLocation>(),
                                maxPerLocation
                            )
                            : userSet.Location;
                    }
                    else
                    {
                        // if not set in inspector, use random
                        desiredLocation = PickRandomValidLocation(
                            subLocations,
                            exploreLocations ?? Array.Empty<HubExploreLocation>(),
                            maxPerLocation
                        );
                    }
                    placementMap[i] = desiredLocation;
                    changed = true;
                }
                else if (
                    userSet.Character != null
                    && !userSet.IsRandomForThisChapter
                    && desiredLocation != userSet.Location
                )
                {
                    desiredLocation = userSet.Location;
                    placementMap[i] = desiredLocation;
                    changed = true;
                }

                AssignUnitToLocation(
                    roster,
                    i,
                    unit,
                    desiredLocation,
                    subLocations,
                    exploreLocations ?? Array.Empty<HubExploreLocation>(),
                    maxPerLocation
                );
            }

            if (changed)
            {
                SavePlacement(roster, placementMap);
            }
        }

        public void SetNonRosterUnitsInHub(
            PlayerTeamRoster roster,
            HubSubLocation[] subLocations,
            HubExploreLocation[] exploreLocations = null
        )
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            var placementMap = LoadSavedNonRosterPlacement();
            bool changed = false;

            if (placementMap == null)
            {
                placementMap = new System.Collections.Generic.Dictionary<
                    string,
                    HubSublocationName
                >();
                changed = true;
            }

            // Iterate distinct characters, picking the best entry per chapter.
            var seen = new System.Collections.Generic.HashSet<CharacterData>();
            foreach (var entry in HubCharacterLocations)
            {
                if (entry.Character == null || !seen.Add(entry.Character))
                {
                    continue;
                }

                var info = FindHubCharacterLocationForChapter(entry.Character);
                if (info.Character == null)
                {
                    continue;
                }

                // Skip characters in the roster — SetTeamLocations handles those
                if (roster.characters.Any(u => u.CharacterData == info.Character))
                {
                    continue;
                }

                string characterKey = info.Character.name;

                if (!placementMap.TryGetValue(characterKey, out var desiredLocation))
                {
                    desiredLocation = info.IsRandomForThisChapter
                        ? PickRandomValidLocation(
                            subLocations,
                            exploreLocations ?? Array.Empty<HubExploreLocation>(),
                            maxPerLocation
                        )
                        : info.Location;
                    placementMap[characterKey] = desiredLocation;
                    changed = true;
                }
                else if (!info.IsRandomForThisChapter && desiredLocation != info.Location)
                {
                    desiredLocation = info.Location;
                    placementMap[characterKey] = desiredLocation;
                    changed = true;
                }

                HubSubLocation location = subLocations.FirstOrDefault(s =>
                    s.LocationName == desiredLocation
                );
                if (location == null && exploreLocations != null)
                {
                    location = exploreLocations.FirstOrDefault(e =>
                        e.LocationName == desiredLocation
                    );
                }

                if (location == null)
                {
                    continue;
                }

                location.CharactersPresent ??= new CharacterInstance[0];
                if (location.CharactersPresent.Length >= maxPerLocation)
                {
                    continue;
                }

                var instance = _charFactory.CreateOrRecall(info.Character);
                if (instance == null)
                {
                    continue;
                }

                var list = new System.Collections.Generic.List<CharacterInstance>(
                    location.CharactersPresent
                )
                {
                    instance,
                };
                location.CharactersPresent = list.ToArray();
            }

            if (changed)
            {
                SaveNonRosterPlacement(placementMap);
            }
        }
    }
}
