using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Characters.Configuration;
using Turnroot.Characters.Subclasses;
using Turnroot.GameSettings;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterData
    {
        [Serializable]
        public struct PortraitEntry
        {
            [Tooltip("Key used to reference this portrait (e.g., \"default\", \"happy\").")]
            public string Key;

            [Tooltip("The portrait data used to render this character's image.")]
            public Portrait Portrait;

            public Sprite RenderedSprite => Portrait?.SavedSprite ?? Portrait?.RuntimeSprite;
        }

        [field: Foldout("Visual"), SerializeField]
        public PortraitEntry[] Portraits { get; private set; } = new PortraitEntry[0];

        public Portrait DefaultPortrait => CharacterHelpers.GetDefaultPortrait(Portraits);

        [field: Foldout("Visual"), HideInInspector]
        public SerializableDictionary<string, TaggedLayerDefault> TaggedLayerDefaults
        {
            get;
            private set;
        } = new();

        [NonSerialized]
        private Dictionary<string, Portrait> _portraitLookupCache;

        #region Portrait Management
        public void InvalidatePortraitLookupCache()
        {
            _portraitLookupCache = null;
            _portraitArrayCache = null;
        }

        private void EnsurePortraitLookup()
        {
            if (_portraitLookupCache != null)
            {
                return;
            }

            _portraitLookupCache = new Dictionary<string, Portrait>();
            if (Portraits == null)
            {
                return;
            }

            foreach (var entry in Portraits)
            {
                if (entry.Portrait == null || string.IsNullOrEmpty(entry.Key))
                {
                    continue;
                }

                if (!_portraitLookupCache.ContainsKey(entry.Key))
                {
                    _portraitLookupCache[entry.Key] = entry.Portrait;
                }
            }
        }

        public bool TryGetPortrait(string key, out Portrait portrait)
        {
            portrait = null;
            EnsurePortraitLookup();
            return _portraitLookupCache != null
                && _portraitLookupCache.TryGetValue(key, out portrait);
        }

        public Portrait GetPortrait(string key) => TryGetPortrait(key, out var p) ? p : null;

        public string[] GetPortraitKeys() =>
            Portraits?.Select(p => p.Key).Where(k => !string.IsNullOrEmpty(k)).ToArray()
            ?? Array.Empty<string>();

        public int PortraitCount => Portraits?.Length ?? 0;

        public void AddOrUpdatePortrait(string key, Portrait portrait)
        {
            if (string.IsNullOrEmpty(key) || portrait == null)
            {
                return;
            }

            Portraits ??= new PortraitEntry[0];

            bool updated = false;
            for (int i = 0; i < Portraits.Length; i++)
            {
                if (Portraits[i].Key == key)
                {
                    Portraits[i].Portrait = portrait;
                    updated = true;
                    break;
                }
            }

            if (!updated)
            {
                var list = Portraits.ToList();
                list.Add(new PortraitEntry { Key = key, Portrait = portrait });
                Portraits = list.ToArray();
            }

            InvalidatePortraitArrayCache();
            InvalidatePortraitLookupCache();
        }

        public bool ContainsPortraitKey(string key)
        {
            EnsurePortraitLookup();
            return _portraitLookupCache != null && _portraitLookupCache.ContainsKey(key);
        }
        #endregion

        private Portrait[] _portraitArrayCache;

        [field: Foldout("Visual"), SerializeField]
        [Tooltip(
            "Prefab containing head/hands mesh(s). Should contain a SkinnedMeshRenderer to be used for head/hands"
        )]
        public GameObject HeadAndHandsPrefab { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        [Tooltip(
            "Prefab containing hair mesh (SkinnedMeshRenderer). Used when classes opt to attach unit hair. Hair uses its own material and is excluded from class outfit tinting."
        )]
        public GameObject HairPrefab { get; private set; }

        [field: Foldout("Visual"), SerializeField]
        [Tooltip(
            "Optional per-character non-battle outfit prefab used when UseBattleModel is false"
        )]
        public GameObject NonBattleOutfitPrefab { get; private set; }

        [field:
            Foldout("Rigging"),
            SerializeField,
            Tooltip(
                "Names of the hair/extra bone chains that should be animated procedurally. Each chain is animated independently (e.g., 'Hair_L', 'Hair_R', 'Cape')."
            ),
            HorizontalLine(color: EColor.Blue),
            ShowIf(nameof(ProceduralHairSimulationEnabled))
        ]
        public string[] ProceduralBoneChains { get; private set; } = new string[0];

        private bool ProceduralHairSimulationEnabled() =>
            GameplayGeneralSettings.Instance?.ProceduralHairSimulation ?? false;

        [field:
            Foldout("Behavior"),
            SerializeField,
            HorizontalLine(color: EColor.Blue),
            HideInInspector
        ]
        public CharacterBehavior BehaviorSettings { get; private set; }

        [HideInInspector, SerializeField]
        [HorizontalLine(color: EColor.Indigo)]
        private HereditaryTraits _passedDownTraits = new();

        [HideInInspector, SerializeField]
        private bool _hasDesignatedChildUnit = false;

        [HideInInspector, SerializeField]
        private CharacterData _childUnitId;

        [field: SerializeField]
        public List<InventorySlot> StartingInventory { get; private set; } = new();

    }
}
