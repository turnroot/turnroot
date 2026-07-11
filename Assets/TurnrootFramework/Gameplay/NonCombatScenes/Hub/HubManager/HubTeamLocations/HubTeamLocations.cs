using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Characters.Roster;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [Serializable]
    public struct HubCharacterLocation
    {
        public CharacterData Character;
        public HubCharacterSpawnArea SpawnArea;

        public int Chapter;

        [InfoBox(
            "If true, the character will be randomly assigned to a valid location this chapter instead of using the specified location."
        )]
        public bool IsRandomForThisChapter;
    }

    public partial class HubManager : MonoBehaviour
    {
        private CharacterFactory _charFactory;
        private readonly HashSet<string> _spawnedCharacterIds = new();

        [Foldout("Characters")]
        public HubCharacterLocation[] HubCharacterLocations;

        [Foldout("Characters")]
        [Tooltip(
            "Spawn areas used for assigning and placing team and non-roster characters in hub."
        )]
        public HubCharacterSpawnArea[] CharacterSpawnAreas;

        public void InitializeTeamLocations()
        {
            _charFactory = new CharacterFactory(_brain.ltm);

            CharacterSpawnAreas ??= Array.Empty<HubCharacterSpawnArea>();

            _spawnedCharacterIds.Clear();
            ClearAssignedCharacters(CharacterSpawnAreas);

            var persistentRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            SetTeamLocations(persistentRoster, CharacterSpawnAreas);
            SetNonRosterUnitsInHub(persistentRoster, CharacterSpawnAreas);

            SpawnAllCharacters(CharacterSpawnAreas, _brain);
        }

        private void ClearAssignedCharacters(HubCharacterSpawnArea[] spawnAreas)
        {
            if (spawnAreas != null)
            {
                foreach (var location in spawnAreas)
                {
                    if (location != null)
                    {
                        location.CharactersPresent = new CharacterInstance[0];
                    }
                }
            }
        }

        public void SpawnAllCharacters(HubCharacterSpawnArea[] spawnAreas, Brain.Brain brain)
        {
            if (spawnAreas == null || spawnAreas.Length == 0)
            {
                return;
            }

            foreach (var location in spawnAreas)
            {
                SpawnCharactersForLocation(location, brain);
            }
        }

        public void SetTeamLocations(PlayerTeamRoster roster, HubCharacterSpawnArea[] spawnAreas)
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            var placementMap = LoadSavedPlacement(roster);
            bool changed = false;

            if (placementMap == null)
            {
                placementMap = new Dictionary<int, HubSublocationName>();
                changed = true;
            }

            for (int i = 0; i < roster.characters.Length; i++)
            {
                var unit = roster.characters[i];
                if (unit.Status == UnitStatus.Defeated || spawnAreas.Length == 0)
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
                            ? PickRandomValidLocation(spawnAreas, maxPerLocation)
                            : ResolveAssignedLocationOrRandom(
                                userSet.SpawnArea,
                                spawnAreas,
                                maxPerLocation
                            );
                    }
                    else
                    {
                        // if not set in inspector, use random
                        desiredLocation = PickRandomValidLocation(spawnAreas, maxPerLocation);
                    }
                    placementMap[i] = desiredLocation;
                    changed = true;
                }
                else if (
                    userSet.Character != null
                    && !userSet.IsRandomForThisChapter
                    && desiredLocation
                        != ResolveAssignedLocationOrRandom(
                            userSet.SpawnArea,
                            spawnAreas,
                            maxPerLocation
                        )
                )
                {
                    desiredLocation = ResolveAssignedLocationOrRandom(
                        userSet.SpawnArea,
                        spawnAreas,
                        maxPerLocation
                    );
                    placementMap[i] = desiredLocation;
                    changed = true;
                }

                AssignUnitToLocation(roster, i, unit, desiredLocation, spawnAreas, maxPerLocation);
            }

            if (changed)
            {
                SavePlacement(placementMap);
            }
        }

        public void SetNonRosterUnitsInHub(
            PlayerTeamRoster roster,
            HubCharacterSpawnArea[] spawnAreas
        )
        {
            int maxPerLocation = GameplayGeneralSettings.Instance.MaxUnitsPerHubLocation;

            var placementMap = LoadSavedNonRosterPlacement();
            bool changed = false;

            if (placementMap == null)
            {
                $"[HubDiag] SetNonRosterUnitsInHub: No saved placement map found — will generate fresh placements".LogInfo(
                    "HubTeamLocations"
                );
                placementMap = new Dictionary<string, HubSublocationName>();
                changed = true;
            }
            else
            {
                $"[HubDiag] SetNonRosterUnitsInHub: Loaded saved placement map with {placementMap.Count} entries".LogInfo(
                    "HubTeamLocations"
                );
            }

            // Iterate distinct characters, picking the best entry per chapter.
            NonRosterUnitLocations(roster, placementMap, spawnAreas, maxPerLocation, ref changed);

            if (changed)
            {
                SaveNonRosterPlacement(placementMap);
            }
        }

        private void NonRosterUnitLocations(
            PlayerTeamRoster roster,
            Dictionary<string, HubSublocationName> placementMap,
            HubCharacterSpawnArea[] spawnAreas,
            int maxPerLocation,
            ref bool changed
        )
        {
            var seen = new HashSet<CharacterData>();
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
                if (roster.characters.Any(u => u.CharacterData.Matches(info.Character)))
                {
                    continue;
                }

                string characterKey = info.Character.name;

                if (!placementMap.TryGetValue(characterKey, out var desiredLocation))
                {
                    desiredLocation = info.IsRandomForThisChapter
                        ? PickRandomValidLocation(spawnAreas, maxPerLocation)
                        : ResolveAssignedLocationOrRandom(
                            info.SpawnArea,
                            spawnAreas,
                            maxPerLocation
                        );
                    placementMap[characterKey] = desiredLocation;
                    changed = true;
                    $"[HubDiag] SetNonRosterUnitsInHub({characterKey}): Not in saved map — assigned to {desiredLocation} (isRandom={info.IsRandomForThisChapter})".LogInfo(
                        "HubTeamLocations"
                    );
                }
                else if (
                    !info.IsRandomForThisChapter
                    && desiredLocation
                        != ResolveAssignedLocationOrRandom(
                            info.SpawnArea,
                            spawnAreas,
                            maxPerLocation
                        )
                )
                {
                    var resolvedInspectorLocation = ResolveAssignedLocationOrRandom(
                        info.SpawnArea,
                        spawnAreas,
                        maxPerLocation
                    );
                    $"[HubDiag] SetNonRosterUnitsInHub({characterKey}): OVERRIDING saved location {desiredLocation} with inspector spawn area location {resolvedInspectorLocation} (isRandom=false)".LogWarning(
                        "HubTeamLocations"
                    );
                    desiredLocation = resolvedInspectorLocation;
                    placementMap[characterKey] = desiredLocation;
                    changed = true;
                }
                else
                {
                    $"[HubDiag] SetNonRosterUnitsInHub({characterKey}): Using saved location {desiredLocation}".LogInfo(
                        "HubTeamLocations"
                    );
                }

                HubCharacterSpawnArea location = spawnAreas.FirstOrDefault(s =>
                    s.LocationName == desiredLocation
                );

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

                var list = new List<CharacterInstance>(location.CharactersPresent) { instance };
                location.CharactersPresent = list.ToArray();
            }
        }
    }
}
