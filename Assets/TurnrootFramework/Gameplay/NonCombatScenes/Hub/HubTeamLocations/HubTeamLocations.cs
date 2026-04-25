using System;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using UnityEngine;
using static Turnroot.Characters.Roster;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct AdditionalUnitInfo
    {
        public CharacterData character;
        public HubSublocationName location;
    }

    [RequireComponent(typeof(HubManager))]
    public partial class HubTeamLocations : MonoBehaviour
    {
        private Brain.Brain _brain;
        private HubManager _hubManager;
        private CharacterFactory _charFactory;
        private readonly System.Collections.Generic.HashSet<string> _spawnedCharacterIds = new();

        public AdditionalUnitInfo[] NonRosterUnitsInHub;

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
            _exploreLocations = exploreLocations ?? System.Array.Empty<HubExploreLocation>();

            var persistentRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            SetTeamLocations(persistentRoster, subLocations, _exploreLocations);
            SetNonRosterUnitsInHub(subLocations, _exploreLocations);

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

                if (!placementMap.TryGetValue(i, out var desiredLocation))
                {
                    desiredLocation = PickRandomValidLocation(
                        subLocations,
                        exploreLocations ?? System.Array.Empty<HubExploreLocation>(),
                        maxPerLocation
                    );
                    placementMap[i] = desiredLocation;
                    changed = true;
                }

                AssignUnitToLocation(
                    roster,
                    i,
                    unit,
                    desiredLocation,
                    subLocations,
                    exploreLocations ?? System.Array.Empty<HubExploreLocation>(),
                    maxPerLocation
                );
            }

            if (changed)
            {
                SavePlacement(roster, placementMap);
            }
        }

        public void SetNonRosterUnitsInHub(
            HubSubLocation[] subLocations,
            HubExploreLocation[] exploreLocations = null
        )
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            foreach (var info in NonRosterUnitsInHub)
            {
                HubSubLocation location = subLocations.FirstOrDefault(s =>
                    s.LocationName == info.location
                );
                if (location == null && exploreLocations != null)
                {
                    location = exploreLocations.FirstOrDefault(e =>
                        e.LocationName == info.location
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

                var instance = _charFactory.CreateOrRecall(info.character);
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
        }
    }
}
