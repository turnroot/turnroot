using System.Linq;
using Turnroot.Characters;
using Turnroot.Components.UI;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;
using static Turnroot.Characters.Roster;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    [System.Serializable]
    public struct AdditionalUnitInfo
    {
        public CharacterInstance character;
        public HubSublocationName location;
    }

    [RequireComponent(typeof(HubManager))]
    /// <remarks>
    /// If you aren't using this feature in your game, you can ignore this-
    /// it will disable itself without activating if GameplayGeneralSettings.Instance.HubHasTeamLocations is false
    /// </remarks>
    public class HubTeamLocations : MonoBehaviour
    {
        private Brain.Brain _brain;
        private HubManager _hubManager;
        private CharacterFactory _charFactory;

        public AdditionalUnitInfo[] NonRosterUnitsInHub;

        [System.Serializable]
        public struct LocationLayout
        {
            public HubSublocationName location;
            public GameObject layoutObject;
        }

        public LocationLayout[] LocationLayouts;
        public GameObject UnitLocationPortraitPrefab;

        public Sprite FallBackPortrait;

        public void Initialize()
        {
            _hubManager = GetComponent<HubManager>();
            _brain = _hubManager._brain;
            _charFactory = new CharacterFactory(_brain.ltm);

            var persistentRoster =
                _brain.gamewideContextBrain.CreateOrRecallGamewidePersistentPlayerRoster();
            SetTeamLocations(persistentRoster, _hubManager.subLocations);
            SetNonRosterUnitsInHub(_hubManager.subLocations);
        }

        public void SetTeamLocations(PlayerTeamRoster roster, HubSubLocation[] subLocations)
        {
            foreach (var unit in roster.characters)
            {
                if (unit.Status != UnitStatus.Defeated && subLocations.Length > 0)
                {
                    // pick a random valid sublocation, but record its true index so we
                    // can put the portrait in the corresponding layout slot.
                    int pickIndex = Random.Range(0, subLocations.Length);
                    var assignedLocation = subLocations[pickIndex];
                    while (assignedLocation.LocationName == HubSublocationName.Battlefields)
                    {
                        // Can't go to battlefields, skip until we get a non-battlefield
                        pickIndex = Random.Range(0, subLocations.Length);
                        assignedLocation = subLocations[pickIndex];
                    }
                    // locate the layout entry that matches this location explicitly
                    int layoutIndex = -1;
                    if (LocationLayouts != null)
                    {
                        for (int idx = 0; idx < LocationLayouts.Length; idx++)
                        {
                            if (LocationLayouts[idx].location == assignedLocation.LocationName)
                            {
                                layoutIndex = idx;
                                break;
                            }
                        }
                    }
                    if (layoutIndex < 0)
                    {
                        // if user didn't configure layouts correctly fall back to pick index
                        layoutIndex = pickIndex;
                    }

                    CharacterInstance ci = _charFactory?.CreateOrRecall(unit.CharacterData);
                    if (ci != null)
                    {
                        assignedLocation.CharactersPresent ??= new CharacterInstance[0];
                        var list = new System.Collections.Generic.List<CharacterInstance>(
                            assignedLocation.CharactersPresent
                        )
                        {
                            ci,
                        };
                        assignedLocation.CharactersPresent = list.ToArray();
                        $"HubTeamLocations: Assigned {ci.CharacterTemplate.DisplayName} to {assignedLocation.LocationName}".LogInfo();
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
                                    ci.CharacterTemplate.DefaultPortrait?.RuntimeSprite
                                        ?? FallBackPortrait
                                );
                            }
                        }
                        else
                        {
                            $"HubTeamLocations: No horizontal layout prefab or unit portrait prefab assigned for {assignedLocation.LocationName}".LogWarning();
                        }
                    }
                }
            }
        }

        public void SetNonRosterUnitsInHub(HubSubLocation[] subLocations)
        {
            foreach (var info in NonRosterUnitsInHub)
            {
                var location = subLocations.FirstOrDefault(s => s.LocationName == info.location);
                if (location != null)
                {
                    location.CharactersPresent ??= new CharacterInstance[0];
                    var list = new System.Collections.Generic.List<CharacterInstance>(
                        location.CharactersPresent
                    )
                    {
                        info.character,
                    };
                    location.CharactersPresent = list.ToArray();
                }
            }
        }
    }
}
